using UnityEngine;

public class MechLoadoutManager : MonoBehaviour
{
    // equipped parts
    public MechCorePart core;
    public MechHeadPart head;
    public MechWeaponPart weapon;

    // default loadout
    public MechCorePart defaultCore;
    public MechHeadPart defaultHead;
    public MechWeaponPart defaultWeapon;

    // components
    public MechHealth mechHealth;
    public PlayerMovement movement;
    public PlayerShooting shooting;
    public PlayerUpgradeManager upgradeManager;

    private float baseShieldMax;

    // energy stats
    private float baseMaxEnergy;
    private float baseEnergyRegen;

    public PlayerStatsUI statsUI;

    public int CurrentWeight => (head ? head.weight : 0) + (weapon ? weapon.weight : 0);

    public int MaxWeightCapacity => core ? core.weightCapacity : 0;

    private void Awake()
    {

        baseShieldMax = mechHealth.maxShield;

        // base energy
        baseMaxEnergy = movement.maxEnergy;
        baseEnergyRegen = movement.energyRegenRate;

        // starting loadout
        core = defaultCore;
        head = defaultHead;
        weapon = defaultWeapon;

        ApplyCurrentLoadout();
    }

    // equip

    public bool TryEquipCore(MechCorePart newCore, out string reason)
    {
        reason = "";

        if (CurrentWeight > newCore.weightCapacity)
        {
            reason = "Too heavy for this core";
            return false;
        }

        core = newCore;
        ApplyCurrentLoadout();
        return true;
    }

    public bool TryEquipHead(MechHeadPart newHead, out string reason)
    {
        reason = "";

        int newWeight = (weapon ? weapon.weight : 0) + newHead.weight;
        if (core != null && newWeight > core.weightCapacity)
        {
            reason = "Exceeds weight capacity";
            return false;
        }

        head = newHead;
        ApplyCurrentLoadout();
        return true;
    }

    public bool TryEquipWeapon(MechWeaponPart newWeapon, out string reason)
    {
        reason = "";

        int newWeight = (head ? head.weight : 0) + newWeapon.weight;
        if (core != null && newWeight > core.weightCapacity)
        {
            reason = "Exceeds weight capacity";
            return false;
        }

        weapon = newWeapon;
        ApplyCurrentLoadout();
        return true;
    }

    // apply stats

    public void ApplyCurrentLoadout()
    {
        RecalculateStats(upgradeManager);
    }

    public void RecalculateStats(PlayerUpgradeManager upg)
    {
        // pull upgrade values
        float hpBonus = upg ? upg.maxHealthPercent : 0f;
        float spdBonus = upg ? upg.moveSpeedPercent : 0f;
        float shieldBonus = upg ? upg.shieldMaxPercent : 0f;
        float fireRatePlus = upg ? upg.fireRatePercent : 0f;
        float armourBonus = upg ? upg.armourPercent : 0f;
        float spreadBonus = upg ? upg.spreadPercent : 0f;
        float dmgBonus = upg ? upg.attackDamagePercent : 0f;
        float critFlat = upg ? upg.critChanceFlat : 0f;
        float critBonus = upg ? upg.critDamagePercent : 0f;
        float rangeBonus = upg ? upg.rangePercent : 0f;

        // energy bonus
        float maxEnergyBonus = upg ? upg.maxEnergyPercent : 0f;
        float energyRegenBonus = upg ? upg.energyRegenPercent : 0f;

        if (mechHealth != null)
        {
            // health
            float oldRatio = mechHealth.currentCoreHealth / Mathf.Max(1f, mechHealth.maxCoreHealth); // keep current % when max health changes

            float baseHp = core ? core.maxHealth : mechHealth.maxCoreHealth; // core part defines hp
            float finalHp = baseHp * (1f + hpBonus);

            mechHealth.maxCoreHealth = finalHp;
            mechHealth.currentCoreHealth = Mathf.Clamp(finalHp * oldRatio, 1f, finalHp); // clamp

            // armour
            float baseArmour = core ? core.defence : mechHealth.armourPercent;
            float finalArmour = baseArmour * (1f + armourBonus);
            mechHealth.armourPercent = Mathf.Clamp(finalArmour, 0f, 0.9f); // clamp to avoid armour reaching 100% reduction

            // shield
            float shieldBase = baseShieldMax;
            float finalShield = shieldBase * (1f + shieldBonus);
            mechHealth.maxShield = finalShield;
            mechHealth.currentShield = Mathf.Min(mechHealth.currentShield, finalShield); // ensure current does not exceed new max
        }

        // movement + energy
        if (movement != null)
        {
            float coreSpeed = core ? core.moveSpeed : movement.baseMaxSpeed;

            movement.maxSpeed = coreSpeed * (1f + spdBonus); // apply speed upgrade bonus

            // reset accel / decel
            movement.acceleration = movement.baseAcceleration;
            movement.deceleration = movement.baseDeceleration;

            float cachedBaseEnergy = baseMaxEnergy > 0f ? baseMaxEnergy : movement.maxEnergy;
            float cachedBaseEnergyRegen = baseEnergyRegen > 0f ? baseEnergyRegen : movement.energyRegenRate;

            // apply energy bonuses
            float finalMaxEnergy = cachedBaseEnergy * (1f + maxEnergyBonus);
            float finalEnergyRegen = cachedBaseEnergyRegen * (1f + energyRegenBonus);

            movement.energyCostMultiplier = 1f;

            movement.maxEnergy = finalMaxEnergy;
            movement.energyRegenRate = finalEnergyRegen;
            movement.ClampEnergyToMax(); // ensure current does not exceed new max
        }

        // head base
        float headRangeMult = 1f;

        if (shooting != null)
        {
            if (head != null)
            {
                // crit chance
                float baseCritChance = head.baseCritChance;
                shooting.critChance = Mathf.Clamp01(baseCritChance + critFlat);

                // crit mult
                float baseCritMult = head.baseCritDamage;
                shooting.critMultiplier = baseCritMult * (1f + critBonus);

                headRangeMult = head.rangeMultiplier <= 0f ? 1f : head.rangeMultiplier;
            }

            // weapon base
            if (weapon != null)
            {
                // damage
                float baseDmg = weapon.baseDamage;
                shooting.baseDamage = baseDmg * (1f + dmgBonus);

                // fire rate
                float baseFR = weapon.fireRate;
                float frMult = Mathf.Max(0.1f, 1f - fireRatePlus);
                shooting.fireRate = baseFR * frMult;

                // projectile speed
                shooting.bulletSpeed = weapon.bulletSpeed;

                // spread
                float baseSpread = weapon.spreadAngle;
                shooting.spreadAngle = baseSpread * (1f + spreadBonus);

                // range
                float baseRange = weapon.baseRange;
                float finalRange = baseRange * headRangeMult * (1f + rangeBonus);
                shooting.bulletRange = finalRange;
            }
        }

        Debug.Log(
            $"Loadout+Upgrades applied. Core={core?.name}, Head={head?.name}, Weapon={weapon?.name}, " +
            $"Weight {CurrentWeight}/{MaxWeightCapacity}");

        statsUI.Refresh();

    }
}
