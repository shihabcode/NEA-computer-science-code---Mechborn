using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// controls shop between waves
public class ShopManager : MonoBehaviour
{
    // references
    public WaveManager waveManager;
    public ShopSlotUI[] slots;
    public List<ShopItemBase> allItems;
    public ScrapManager scrapManager;
    public PlayerUpgradeManager playerUpgrades;
    public MechLoadoutManager loadoutManager;

    public TextMeshProUGUI weightText;

    // reroll settings
    public int baseRerollCost = 10;
    public float rerollCostGrowthPerRoll = 1.2f;
    public float rerollCostGrowthPerWave = 1.1f;
    private int rerollCountThisRun = 0;

    // repair settings
    public int baseRepairCost = 15;
    public float repairCostGrowthPerWave = 1.1f;

    // buttons (repair and reroll)
    public Button rerollButton;
    public TextMeshProUGUI rerollCostText;
    public Button repairButton;
    public TextMeshProUGUI repairCostText;

    private System.Random rng = new System.Random();

    private void Awake()
    {
        foreach (var slot in slots) // initialise each shop slot
        {
            if (slot != null)
                slot.Init(this);
        }
    }

    private void OnEnable()
    {
        // called when activated
        GenerateNewShop();
        RefreshMetaButtons();
        RefreshWeightUI();
    }

    // shop generation

    public void GenerateNewShop()
    {

        List<ShopItemBase> chosen = new List<ShopItemBase>(); // track items already chosen

        for (int i = 0; i < slots.Length; i++)
        {
            ShopItemBase item = GetRandomItemForSlot(chosen); // pick random valid item

            if (item == null) // if nothing found clear slot
            {
                slots[i].SetItem(null, 0, "");
                continue;
            }

            // get price and push item into UI
            int price = GetPriceForItem(item);
            chosen.Add(item);

            string failureReason = GetFailureReason(item);

            slots[i].SetItem(item, price, failureReason);

        }

        RefreshMetaButtons();
    }

    private ShopItemBase GetRandomItemForSlot(List<ShopItemBase> exclude)
    {
        ItemRarity rarity = RollRarity(); // roll rarity based on current wave

        // filter (not already in chosen list and not already equipped)
        bool Filter(ShopItemBase it) =>
            !exclude.Contains(it) &&
            !IsEquippedPart(it);

        // candidate pool
        List<ShopItemBase> candidates = allItems.FindAll(it => it.rarity == rarity && Filter(it));

        if (candidates.Count == 0) // fallback if no uitems of that rarity
            candidates = allItems.FindAll(Filter);

        if (candidates.Count == 0)
            return null;

        int index = rng.Next(candidates.Count); // choose random candidate
        return candidates[index];
    }

    private bool IsEquippedPart(ShopItemBase item)
    {
        // returns true if item is a part already equipped
        if (item is ShopItem_Part part)
        {
            if (part.corePart != null && loadoutManager.core == part.corePart)
                return true;
            if (part.headPart != null && loadoutManager.head == part.headPart)
                return true;
            if (part.weaponPart != null && loadoutManager.weapon == part.weaponPart)
                return true;
        }

        return false;
    }

    // rarity rolling
    private ItemRarity RollRarity()
    {
        int wave = waveManager.currentWave;
        int maxWaves = waveManager.maxWaves;

        float t = Mathf.Clamp01((wave - 1) / (float)(maxWaves - 1)); // normalise wave progression

        // start rarities
        float commonStart = 0.85f;
        float rareStart = 0.13f;
        float epicStart = 0.02f;
        float legendaryStart = 0.00f;

        // end rarities
        float commonEnd = 0.10f;
        float rareEnd = 0.55f;
        float epicEnd = 0.3f;
        float legendaryEnd = 0.1f;

        // interpolate weights across run
        float wCommon = Mathf.Lerp(commonStart, commonEnd, t);
        float wRare = Mathf.Lerp(rareStart, rareEnd, t);
        float wEpic = Mathf.Lerp(epicStart, epicEnd, t);
        float wLegendary = Mathf.Lerp(legendaryStart, legendaryEnd, t);

        // roll weighted random rarity
        float total = wCommon + wRare + wEpic + wLegendary;
        float roll = Random.value * total;

        if (roll < wCommon) return ItemRarity.Common;
        if (roll < wCommon + wRare) return ItemRarity.Rare;
        if (roll < wCommon + wRare + wEpic) return ItemRarity.Epic;
        return ItemRarity.Legendary;
    }

    // failure message
    private string GetFailureReason(ShopItemBase item)
    {
        if (item is ShopItem_Part part)
        {
            var core = loadoutManager.core;
            var head = loadoutManager.head;
            var weapon = loadoutManager.weapon;

            if (part.corePart != null)
            {
                int newCap = part.corePart.weightCapacity;
                int otherWeight =
                    (head != null ? head.weight : 0) +
                    (weapon != null ? weapon.weight : 0);

                if (otherWeight > newCap)
                    return "Core capacity too low for current loadout";
            }

            else if (part.headPart != null)
            {
                if (core != null)
                {
                    int newWeight =
                        part.headPart.weight +
                        (weapon != null ? weapon.weight : 0);

                    if (newWeight > core.weightCapacity)
                        return "Exceeds weight capacity";
                }
            }

            else if (part.weaponPart != null)
            {
                if (core != null)
                {
                    int newWeight =
                        part.weaponPart.weight +
                        (head != null ? head.weight : 0);

                    if (newWeight > core.weightCapacity)
                        return "Exceeds weight capacity";
                }
            }
        }

        return "";
    }

    // price scaling

    private int GetPriceForItem(ShopItemBase item)
    {
        if (item == null) return 0;

        int wave = Mathf.Max(1, waveManager.currentWave);
        int maxWaves = waveManager.maxWaves;

        // early / late multipliers (how expensive it becomes over the run)
        float earlyMult = 0.5f;
        float lateMult = 3.0f;

        float exponent = (wave - 1) / Mathf.Max(1f, (maxWaves - 1)); // exponent normalised across waves
        float baseGrowth = lateMult / earlyMult; // choose exponential curve that reaches lateMult by maxWaves
        float priceMult = earlyMult * Mathf.Pow(baseGrowth, exponent);

        // rarity multipliers
        switch (item.rarity)
        {
            case ItemRarity.Rare: priceMult *= 1.15f; break;
            case ItemRarity.Epic: priceMult *= 1.35f; break;
            case ItemRarity.Legendary: priceMult *= 1.6f; break;
        }

        int finalCost = Mathf.Max(1, Mathf.RoundToInt(item.cost * priceMult)); // ensure nothing is free
        return finalCost;
    }

    private int GetCurrentRerollCost()
    {
        int wave = Mathf.Max(1, waveManager.currentWave);

        float costF = baseRerollCost * Mathf.Pow(rerollCostGrowthPerRoll, rerollCountThisRun) * Mathf.Pow(rerollCostGrowthPerWave, wave - 1);

        return Mathf.CeilToInt(costF);
    }

    private int GetCurrentRepairCost()
    {
        MechHealth mh = loadoutManager.mechHealth;

        float missing = mh.maxCoreHealth - mh.currentCoreHealth; // amt of health missing
        if (missing <= 0.5f) return 0;

        float missingFraction = missing / mh.maxCoreHealth;

        int wave = (waveManager != null) ? Mathf.Max(1, waveManager.currentWave) : 1;

        float baseCost = baseRepairCost * missingFraction; // base cost scaled by how damaged player is
        float costF = baseCost * Mathf.Pow(repairCostGrowthPerWave, wave - 1); // scale cost by wave progression

        return Mathf.CeilToInt(costF);
    }

    private void RefreshMetaButtons()
    {
        // updates reroll / repair buttons and whether they can be pressed
        int rerollCost = GetCurrentRerollCost();

        rerollCostText.text = $"REROLL ({rerollCost})";
        rerollButton.interactable = (scrapManager != null);

        int repairCost = GetCurrentRepairCost();

        if (repairCost <= 0) repairCostText.text = "REPAIR (FULL)";
        else repairCostText.text = $"REPAIR ({repairCost})";

        repairButton.interactable = repairCost > 0;
    }

    // reroll / repair

    public void RerollShop()
    {
        int rerollCost = GetCurrentRerollCost();

        if (!scrapManager.TrySpendScrap(rerollCost))
        {
            Debug.Log($"Not enough scrap to reroll. Need {rerollCost}.");
            return;
        }

        rerollCountThisRun++;
        Debug.Log($"Shop rerolled for {rerollCost} scrap (rerolls so far: {rerollCountThisRun}).");

        GenerateNewShop(); // creates a new selection
    }

    public void RepairMech()
    {
        // repair player to full health
        int cost = GetCurrentRepairCost();
        if (cost <= 0)
        {
            Debug.Log("Mech already at full health.");
            RefreshMetaButtons();
            return;
        }

        if (!scrapManager.TrySpendScrap(cost))
        {
            Debug.Log($"Not enough scrap to repair. Need {cost}.");
            return;
        }

        MechHealth mh = loadoutManager.mechHealth;
        mh.currentCoreHealth = mh.maxCoreHealth;
        mh.currentShield = mh.maxShield;
        mh.ForceHealthChangedEvent();

        Debug.Log($"Repaired mech to full for {cost} scrap.");
        RefreshMetaButtons();
    }

    public bool BuyItem(ShopItemBase item)
    {
        // buys item and pplies its effect. returns true if purchase succeeded
        if (item == null) return false;

        // weight check
        if (item is ShopItem_Part)
        {
            string reason = GetFailureReason(item);
            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log($"Cannot buy {item.itemName}: {reason}");
                return false;
            }
        }

        // scrap check
        int price = GetPriceForItem(item);

        if (!scrapManager.TrySpendScrap(price))
        {
            Debug.Log($"Not enough scrap to buy {item.itemName}. Need {price}.");
            return false;
        }

        if (item is ShopItem_Normal normal)
        {
            playerUpgrades?.ApplyItem(normal);
        }
        else if (item is ShopItem_Part partItem)
        {
            string reason;
            if (partItem.corePart != null)
                loadoutManager.TryEquipCore(partItem.corePart, out reason);
            else if (partItem.headPart != null)
                loadoutManager.TryEquipHead(partItem.headPart, out reason);
            else if (partItem.weaponPart != null)
                loadoutManager.TryEquipWeapon(partItem.weaponPart, out reason);
        }
        else if (item is ShopItem_Modifier mod)
        {
            playerUpgrades?.ApplyItem(mod);
        }

        Debug.Log($"Bought: {item.itemName} for {price} scrap");

        RefreshWeightUI();
        RefreshMetaButtons();

        return true;
    }

    public void RefreshWeightUI()
    {
        int currentWeight = loadoutManager.CurrentWeight;
        int maxWeight = loadoutManager.MaxWeightCapacity;

        weightText.text = $"Weight: {currentWeight} / {maxWeight}";
    }

    public void CloseShop()
    {
        gameObject.SetActive(false);
        if (waveManager != null)
            waveManager.OnShopClosed();
    }
}