using UnityEngine;
using UnityEngine.UI;

public class EnergyBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    public void SetEnergy(float current, float max)
    {
        fillImage.fillAmount = Mathf.Clamp01(current / max);
    }
}
