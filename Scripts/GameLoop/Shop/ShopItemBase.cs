using UnityEngine;

public enum ItemRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}
public enum ShopItemType
{
    NormalUpgrade,
    CorePart,
    HeadPart,
    WeaponPart,
    Modifier
}
public enum NormalItemEffectType
{
    MaxHealthPercent,
    MoveSpeedPercent,
    ShieldMaxPercent,
    FireRatePercent,
    ArmourPercent,
    SpreadPercent,
    AttackDamagePercent,
    CritChanceFlat,
    CritDamagePercent,
    RangePercent,
    MaxEnergyPercent,
    EnergyRegenPercent
}

public enum ModifierEffectType
{
    TungstenRounds,
    Gasoline,
    OverchargedShield,
    HermesDrive,
    VampireCore
}

[System.Serializable]
public class NormalItemEffect
{
    public NormalItemEffectType effectType;
    public float magnitudePercent = 0.1f;
}

public abstract class ShopItemBase : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public ItemRarity rarity = ItemRarity.Common;
    public int cost = 10;

    [HideInInspector] public ShopItemType itemType;

    protected abstract ShopItemType AutoType { get; }

    protected virtual void OnValidate()
    {
        itemType = AutoType;
    }
}

