using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Mech Part Item")]
public class ShopItem_Part : ShopItemBase
{
    public MechCorePart corePart;
    public MechHeadPart headPart;
    public MechWeaponPart weaponPart;

    protected override ShopItemType AutoType
    {
        get
        {
            if (corePart != null) return ShopItemType.CorePart;
            if (headPart != null) return ShopItemType.HeadPart;
            if (weaponPart != null) return ShopItemType.WeaponPart;
            return ShopItemType.CorePart;
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        int count =
            (corePart != null ? 1 : 0) +
            (headPart != null ? 1 : 0) +
            (weaponPart != null ? 1 : 0);
    }
}