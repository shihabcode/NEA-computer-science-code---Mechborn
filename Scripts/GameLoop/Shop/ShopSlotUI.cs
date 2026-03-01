using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    // references
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI cannotBuyReasonText;
    public Button buyButton;

    [HideInInspector] public ShopItemBase item;
    private ShopManager manager;
    private int currentCost;

    public void Init(ShopManager managerRef)
    {
        manager = managerRef;
    }

    public void SetItem(ShopItemBase newItem, int price, string failureReason = "")
    {
        item = newItem;
        currentCost = price;

        if (item == null) // if no item then clear slot
        {
            nameText.text = "";
            descriptionText.text = "";
            costText.text = "";
            buyButton.interactable = false;
            buyButton.onClick.RemoveAllListeners();
            return;
        }

        // update Ui fields
        nameText.text = item.itemName;
        descriptionText.text = item.description;
        costText.text = $"Cost: {currentCost}";
        buyButton.interactable = true; // enable buying

        buyButton.onClick.RemoveAllListeners(); // clear old listeners
        buyButton.onClick.AddListener(OnBuyClicked);

        // show failure reason if provided
        if (!string.IsNullOrEmpty(failureReason))
        {
            cannotBuyReasonText.text = failureReason;
            cannotBuyReasonText.color = Color.red;
            cannotBuyReasonText.gameObject.SetActive(true);
        }
        else
        {
            cannotBuyReasonText.gameObject.SetActive(false);
        }
    }

    // called when player presses button
    private void OnBuyClicked()
    {
        if (manager.BuyItem(item))
        {
                buyButton.interactable = false;
                var colors = buyButton.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f);
                buyButton.colors = colors;
        }
    }
}
