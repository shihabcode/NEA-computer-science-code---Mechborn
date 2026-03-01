using TMPro;
using UnityEngine;

public class PlayerStatsUI : MonoBehaviour
{
    public TMP_Text statsText;

    // references
    public MechHealth mechHealth;
    public PlayerMovement movement;
    public PlayerShooting shooting;
    public MechLoadoutManager loadout;

    private void OnEnable()
    {
        mechHealth.OnHealthChanged += OnHealthChanged;

        Refresh();
    }

    private void OnDisable()
    {
        mechHealth.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float hp, float shield)
    {
        Refresh();
    }

    public void Refresh()
    {
        // stats
        float maxHp = mechHealth ? mechHealth.maxCoreHealth : 0f;
        float maxShield = mechHealth ? mechHealth.maxShield : 0f;
        float armour = mechHealth ? mechHealth.armourPercent : 0f;
        float maxEnergy = movement ? movement.maxEnergy : 0f;
        float speed = movement ? movement.maxSpeed : 0f;
        float dmg = shooting ? shooting.baseDamage : 0f;
        float critChance = shooting ? shooting.critChance : 0f;
        float critMult = shooting ? shooting.critMultiplier : 0f;
        float fireRate = shooting ? shooting.fireRate : 0f;
        int weight = loadout ? loadout.CurrentWeight : 0;
        int cap = loadout ? loadout.MaxWeightCapacity : 0;
        float shotsPerSecond = fireRate > 0.0001f ? 1f / fireRate : 0f;

        // text
        statsText.text =
            $"<b>STATS</b>\n" +
            $" \n" +
            $"HP: {maxHp:0}\n" +
            $"Shield: {maxShield:0}\n" +
            $"Energy: {maxEnergy:0}\n" +
            $"Speed: {speed:0.00}\n" +
            $"Defence: {(armour * 100f):0}%\n" +
            $"Attack Damage: {dmg:0.0}\n" +
            $"Crit Chance: {(critChance * 100f):0}%\n" +
            $"Crit Mult: {critMult:0.00}x\n" +
            $"Fire Rate: {shotsPerSecond:0.0}\n" +
            $"Weight: {weight}/{cap}";
    }
}
