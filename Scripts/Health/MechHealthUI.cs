using UnityEngine;
using UnityEngine.UI;

public class MechHealthUI : MonoBehaviour
{
    public MechHealth mech;             
    public Image coreFillImage;        
    public Image shieldFillImage;

    void Start()
    {
        mech.OnHealthChanged += UpdateUI; // subscribe to health change so UI updates automatically

        UpdateUI(mech.GetCurrentHealth(), mech.currentShield); // make bars show correct values when game is started
    }

    void OnDestroy()
    {
        mech.OnHealthChanged -= UpdateUI; // ubsub from event
    }

    void UpdateUI(float core, float shield)
    {
        //  update core health bar
        if (coreFillImage != null && mech.maxCoreHealth > 0f)
            coreFillImage.fillAmount = Mathf.Clamp01(core / mech.maxCoreHealth);

        // update shield bar
        if (shieldFillImage != null && mech.maxShield > 0f)
            shieldFillImage.fillAmount = Mathf.Clamp01(shield / mech.maxShield);
    }
}