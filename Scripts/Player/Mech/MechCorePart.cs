using UnityEngine;

[CreateAssetMenu(fileName = "NewCorePart", menuName = "Mech Parts/Core")]
public class MechCorePart : ScriptableObject
{
    public string coreName;
    public string id;

    public int weightCapacity;
    public float maxHealth;
    public float moveSpeed;
    public float defence;
}
