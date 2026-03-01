using UnityEngine;

public struct Damage
{
    public float amount;
    public GameObject source;
    public bool isCrit;

    public Damage(float amount, GameObject source, bool isCrit = false)
    {
        this.amount = amount;
        this.source = source;
        this.isCrit = isCrit;
    }
}
