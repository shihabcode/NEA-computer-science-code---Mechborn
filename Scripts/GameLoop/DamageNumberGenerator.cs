using UnityEngine;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance;

    public FloatingDamageText damageTextPrefab;
    public Canvas worldCanvas;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnDamageNumber(Vector3 position, float damage, bool crit = false)
    {
        FloatingDamageText d = Instantiate(damageTextPrefab, worldCanvas.transform);
        d.transform.position = position;

        Color color = crit ? Color.yellow : Color.white; // determine where colour is white or yellow
        d.Initialize(damage, color); // initialize and set colour
    }
}
