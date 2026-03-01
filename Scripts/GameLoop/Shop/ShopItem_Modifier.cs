using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Modifier Item")]
public class ShopItem_Modifier : ShopItemBase
{
    public ModifierEffectType modifierEffectType;

    protected override ShopItemType AutoType => ShopItemType.Modifier;
}