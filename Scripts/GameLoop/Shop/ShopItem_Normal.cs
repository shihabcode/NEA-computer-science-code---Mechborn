using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Normal Upgrade Item")]
public class ShopItem_Normal : ShopItemBase
{
    public NormalItemEffect[] effects;

    protected override ShopItemType AutoType => ShopItemType.NormalUpgrade;
}