using UnityEngine;

[CreateAssetMenu(fileName = "NewHeadPart", menuName = "Mech Parts/Head")]
public class MechHeadPart : ScriptableObject
{
    public string headName;
    public string id;

    public int weight;
    public float baseCritChance;
    public float baseCritDamage = 1.5f;
    public float rangeMultiplier = 1f;
}
