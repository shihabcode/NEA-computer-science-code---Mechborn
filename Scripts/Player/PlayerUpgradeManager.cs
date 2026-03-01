using JetBrains.Annotations;
using UnityEngine;

public class PlayerUpgradeManager : MonoBehaviour
{
    // references
    public MechHealth mechHealth;
    public PlayerMovement movement;
    public PlayerShooting shooting;
    public MechLoadoutManager loadout;
    public LayerMask enemyLayer;

    // cumulative upgrade stats (stored as additive bonuses for e.g. moveSpeedPercent = 0.15 means +15% move speed)
    [HideInInspector] public float maxHealthPercent;
    [HideInInspector] public float moveSpeedPercent;
    [HideInInspector] public float shieldMaxPercent;
    [HideInInspector] public float fireRatePercent;
    [HideInInspector] public float armourPercent;
    [HideInInspector] public float spreadPercent;
    [HideInInspector] public float attackDamagePercent;
    [HideInInspector] public float critChanceFlat;
    [HideInInspector] public float critDamagePercent;
    [HideInInspector] public float rangePercent;
    [HideInInspector] public float bulletSpeedPercent;
    [HideInInspector] public float maxEnergyPercent;
    [HideInInspector] public float energyRegenPercent;


    // modifier flags
    public bool hasTungstenRounds;
    public bool hasGasoline;
    public bool hasOverchargedShield;
    public bool hasHermes;
    public bool hasVampireCore;

    // extra values for all the modifiers
    public int overchargedShieldStacks = 0;
    public float overchargedShieldBonusPerStack = 100f;
    public float overchargedRegenBonusPerStack = 5f;
    public float overchargedSeepPercent = 0.25f;

    public int hermesMaxStacks = 10;
    public float hermesSpeedPerStack = 0.001f;
    public float hermesDuration = 3f;
    private int hermesStacks = 0;
    private float hermesTimer = 0f;

    public float vampireBaseHealPercent = 0.02f;
    public int vampireStacks = 0;
    public float CurrentVampireHealPercent => vampireStacks <= 0 ? 0f : vampireBaseHealPercent * Mathf.Pow(2f, vampireStacks - 1);

    public void ApplyItem(ShopItemBase item)
    {
        if (item == null)
        {
            Debug.LogWarning("ApplyItem called with null");
            return;
        }

        // normal upgrades can contain multiple effects
        if (item is ShopItem_Normal normal)
        {
            if (normal.effects == null || normal.effects.Length == 0)
            {
                Debug.LogWarning($"Normal upgrade '{item.name}' has no effects.");
                return;
            }

            foreach (var effect in normal.effects)
            {
                ApplySingleEffect(effect.effectType, effect.magnitudePercent);
            }

            Debug.Log($"Applied normal upgrade {item.itemName} with {normal.effects.Length} effects.");
            if (loadout != null)
                loadout.RecalculateStats(this); // recalculate final player stats
            return;
        }

        // applies the modifier if item is one
        if (item is ShopItem_Modifier modifier)
        {
            ApplyModifier(modifier);
            return;
        }

        Debug.Log($"Item {item.name} is not an upgrade / modifier, skipping PlayerUpgradeManager.");
    }

    private void ApplySingleEffect(NormalItemEffectType type, float p)
    {
        switch (type)
        {
            case NormalItemEffectType.MaxHealthPercent:
                maxHealthPercent += p;
                break;

            case NormalItemEffectType.MoveSpeedPercent:
                moveSpeedPercent += p;
                break;

            case NormalItemEffectType.ShieldMaxPercent:
                shieldMaxPercent += p;
                break;

            case NormalItemEffectType.FireRatePercent:
                fireRatePercent += p;
                break;

            case NormalItemEffectType.ArmourPercent:
                armourPercent += p;
                break;

            case NormalItemEffectType.SpreadPercent:
                spreadPercent += p;
                break;

            case NormalItemEffectType.AttackDamagePercent:
                attackDamagePercent += p;
                break;

            case NormalItemEffectType.CritChanceFlat:
                critChanceFlat += p;
                break;

            case NormalItemEffectType.CritDamagePercent:
                critDamagePercent += p;
                break;

            case NormalItemEffectType.RangePercent:
                rangePercent += p;
                break;

            case NormalItemEffectType.MaxEnergyPercent:
                maxEnergyPercent += p;
                break;

            case NormalItemEffectType.EnergyRegenPercent:
                energyRegenPercent += p;
                break;
        }
    }

    private void ApplyModifier(ShopItem_Modifier item)
    {
        // determines which modifier is what and how to apply it.
        switch (item.modifierEffectType)
        {
            case ModifierEffectType.TungstenRounds:
                if (!hasTungstenRounds)
                {
                    hasTungstenRounds = true;

                    attackDamagePercent += 0.15f;
                    bulletSpeedPercent -= 0.10f;

                    shooting.canPierce = true;
                }
                break;

            case ModifierEffectType.Gasoline:
                if (!hasGasoline)
                {
                    hasGasoline = true;
                }
                break;

            case ModifierEffectType.OverchargedShield:
                hasOverchargedShield = true;
                overchargedShieldStacks++;

                if (mechHealth != null)
                {
                    mechHealth.overchargedShieldActive = true; // activates behaviour in mech health
                    mechHealth.shieldSeepPercent = overchargedSeepPercent;

                    // increases max shield and tops up current shield
                    mechHealth.maxShield += overchargedShieldBonusPerStack;
                    mechHealth.currentShield = Mathf.Min(
                        mechHealth.currentShield + overchargedShieldBonusPerStack,
                        mechHealth.maxShield
                    );

                    mechHealth.shieldRechargeRate += overchargedRegenBonusPerStack; // increase recharge speed per stack
                    mechHealth.ForceHealthChangedEvent();
                }
                break;

            case ModifierEffectType.HermesDrive:
                hasHermes = true;
                break;

            case ModifierEffectType.VampireCore:
                if (!hasVampireCore)
                {
                    hasVampireCore = true;
                    vampireStacks = 1;
                }
                else
                {
                    vampireStacks++;
                }
                break;
        }

        Debug.Log($"Applied modifier: {item.modifierEffectType}");

        if (loadout != null)
            loadout.RecalculateStats(this);
    }
    private void Update()
    {
        // stacks decay when timer runs out
        if (hasHermes && hermesTimer > 0)
        {
            hermesTimer -= Time.deltaTime;
            if (hermesTimer <= 0f)
            {
                hermesTimer = 0f;
                hermesStacks = 0;
                UpdateHermesSpeed();
            }
        }
    }
    public void OnEnemyKilledHermes()
    {
        // adds a stack, refreshes timer and applies speed bonus
        if (!hasHermes) return;

        hermesStacks = Mathf.Min(hermesStacks + 1, hermesMaxStacks);
        hermesTimer = hermesDuration; // refresh duration on kill
        UpdateHermesSpeed();
    }

    private void UpdateHermesSpeed()
    {
        // applies hermes speed buff
        float bonusPercent = hermesStacks * hermesSpeedPerStack;
        movement.hermesSpeedMultiplier = 1f + bonusPercent;
    }

    public void OnEnemyKilled(MechHealth enemy)
    {
        if (!hasVampireCore || enemy == null)
            return;

        float healPercent = CurrentVampireHealPercent;
        if (healPercent <= 0f)
            return;

        // heal scales with enemy max HP
        float healAmount = enemy.maxCoreHealth * healPercent;
        mechHealth.Heal(healAmount);

        Debug.Log($"Vampire Core healed {healAmount:F1} HP ({healPercent * 100f:F1}% of enemy max HP).");
    }

}



