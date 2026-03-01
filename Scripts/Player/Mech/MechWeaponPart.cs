using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponPart", menuName = "Mech Parts/Weapon")]
public class MechWeaponPart : ScriptableObject
{
    public string weaponName;
    public string id;

    public int weight;
    public float baseDamage;
    public float bulletSpeed;
    public float spreadAngle;
    public float baseRange = 3f;
    public float fireRate;
}
