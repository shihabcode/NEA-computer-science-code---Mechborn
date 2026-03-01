using UnityEngine;
using TMPro;

public class ScrapManager : MonoBehaviour
{
    public static ScrapManager Instance { get; private set; }

    // scrap config
    public int scrap = 0;
    public int baseScrapPerKill = 1;
    public int maxWave = 20;
    public int bonusScrapAtMaxWave = 4;

    public TextMeshProUGUI scrapText;
    public WaveManager waveManager;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        scrapText.text = scrap.ToString(); // update scrap text
    }

    public int GetScrapPerKill()
    {
        int wave = Mathf.Clamp(waveManager.currentWave, 1, maxWave); // clamp wave between 1 and maxwave
        float t = (wave - 1) / (float)(maxWave - 1); // normalise
        int bonus = Mathf.RoundToInt(t * bonusScrapAtMaxWave); // scale bonus scrap
        return baseScrapPerKill + bonus;
    }

    public void AddScrapFromKill()
    {
        // called when enemy is killed
        int amount = GetScrapPerKill();
        scrap += amount;
        UpdateUI();
    }

    public bool TrySpendScrap(int amount)
    {
        // prevent negative scrap
        if (scrap < amount)
            return false;

        scrap -= amount;           
        UpdateUI();                
        return true;
    }
}
