using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class WaveManager : MonoBehaviour
{
    // wave config
    public float timePerWave;
    public int maxWaves = 20;
    public float minWaveTime = 20f;
    public float maxWaveTime = 120f;

    // spawn config
    public float baseSpawnRate = 0.5f;
    public float spawnRateGrowthPerWave = 1.3f;
    public float spawnRateGrowthOverTime = 1.05f;

    // enemy prefabs
    public GameObject seekerPrefab;
    public GameObject walkingTurretPrefab;
    public GameObject mechaMinePrefab;
    public GameObject deadmanPrefab;
    public GameObject berserkerPrefab;

    public Transform[] spawnPoints;
    public Transform player;
    public Transform enemyParent;
    public float minSpawnDistanceFromPlayer = 8f;

    // ui and text
    public GameObject shopUI;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI timerText;

    // arena bounds
    public Vector2 arenaMin;
    public Vector2 arenaMax;
    public float spawnRadiusExtra = 12f;
    public float wallAvoidThreshold = 5;

    public int currentWave = 0;
    public float waveTimer = 0f;
    public float waveElapsedTime = 0f;
    public bool waveRunning = false;
    public bool inShop = false;

    // difficulty scaling
    public float healthGrowthPerWave = 1.08f;
    public float damageGrowthPerWave = 1.06f;
    public float speedGrowthPerWave = 1.03f;
    public float accelGrowthPerWave = 1.03f;
    public float decelGrowthPerWave = 1.03f;

    // boss wave
    public int bossWaveIndex = 15;
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public GameObject victoryUI;
    private bool bossWaveActive = false;
    private GameObject bossInstance;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    public static WaveManager Instance { get; private set; } // singleton access

    private float spawnAccumulator = 0f; // accumulate spawn amounts

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartNextWave(); // starts first wave immediately
    }

    private void Update()
    {
        if (!waveRunning) return; // if wave isnt running do nothing (when in shop)

        if (bossWaveActive) // boss wave replaces timer
        {
            timerText.text = "BOSS";
            return;
        }

        float dt = Time.deltaTime;

        // countdown wave timer
        waveTimer -= dt;
        if (waveTimer < 0f) waveTimer = 0f;

        waveElapsedTime += dt;

        UpdateTimerUI();
        HandleSpawning(dt);

        if (waveTimer <= 0f) // end wave when timer reaches 0
        {
            EndCurrentWave();
        }
    }

    // Wave flow

    private float GetTimeForWave(int wave)
    {
        // calculate wave length
        int w = Mathf.Clamp(wave, 1, maxWaves);
        float t = (w - 1f) / (maxWaves - 1f);
        return Mathf.Lerp(minWaveTime, maxWaveTime, t);
    }

    private void StartNextWave()
    {
        // starts next wave
        if (currentWave >= maxWaves) return;
        
        currentWave++;
        waveRunning = true;
        inShop = false;

        if (currentWave == bossWaveIndex) // if next is boss wave begin that
        {
            StartBossWave();
            return;
        }

        StartNormalWave();
    }

    private void StartNormalWave()
    {
        bossWaveActive = false;

        // setup timer and accumulator
        waveTimer = GetTimeForWave(currentWave);
        waveElapsedTime = 0f;
        spawnAccumulator = 0f;

        UpdateWaveUI();
    }

    private void StartBossWave()
    {
        bossWaveActive = true;

        KillAllEnemies(); // clear active enemies

        shopUI.SetActive(false);
        inShop = false;

        // spawn boss
        Vector3 spawnPos = GetDynamicSpawnPosition();

        bossInstance = Instantiate(bossPrefab, spawnPos, Quaternion.identity, enemyParent);

        ApplyEnemyScaling(bossInstance); // apply scaling

        var mh = bossInstance.GetComponent<MechHealth>();
        mh.OnDeath += OnBossDefeated;

        UpdateWaveUI();
    }

    private void EndCurrentWave()
    {
        // ends current wave, clears enemies and opens shop
        waveRunning = false;

        KillAllEnemies();

        if (shopUI != null)
        {
            shopUI.SetActive(true);
        }

        inShop = true;
    }

    private void OnBossDefeated()
    {
        // opens victory screen when boss dies
        bossWaveActive = false;
        waveRunning = false;

        KillAllEnemies();

        victoryUI.SetActive(true);

        Time.timeScale = 0f;
    }


    public void OnShopClosed()
    {
        // exit shop
        shopUI.SetActive(false);

        if (!inShop) return;
        StartNextWave();
    }

    // spawning

    private GameObject ChooseEnemyPrefabForCurrentWave()
    {
        // seekers
        if (currentWave < 3)
        {
            return seekerPrefab;
        }
        // 3-4 + turrets
        else if (currentWave < 5)
        {
            return ChooseWeighted(
                seekerPrefab, 0.8f,
                walkingTurretPrefab, 0.2f
            );
        }
        // 5-8 + mines
        else if (currentWave < 8)
        {
            return ChooseWeighted(
                seekerPrefab, 0.7f,
                walkingTurretPrefab, 0.2f,
                mechaMinePrefab, 0.1f
            );
        }
        // 8-12 + deadmans
        else if (currentWave < 12)
        {
            return ChooseWeighted(
                seekerPrefab, 0.5f,
                walkingTurretPrefab, 0.2f,
                mechaMinePrefab, 0.2f,
                deadmanPrefab, 0.1f
            );
        }
        // 12-15 + berserkers
        else
        {
            return ChooseWeighted(
                seekerPrefab, 0.4f,
                walkingTurretPrefab, 0.2f,
                mechaMinePrefab, 0.2f,
                deadmanPrefab, 0.1f,
                berserkerPrefab, 0.1f
            );
        }
    }

    private GameObject ChooseWeighted(params object[] prefabWeightPairs)
    {
        float totalWeight = 0f; // calculate total weight

        // sum all valid weights
        for (int i = 0; i < prefabWeightPairs.Length; i += 2)
        {
            GameObject prefab = prefabWeightPairs[i] as GameObject;
            float weight = (float)prefabWeightPairs[i + 1];

            if (prefab == null || weight <= 0f) continue;
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return null;

        // roll random number [0, totalWeight]
        float r = Random.value * totalWeight;

        // walk through weights until it works
        for (int i = 0; i < prefabWeightPairs.Length; i += 2)
        {
            GameObject prefab = prefabWeightPairs[i] as GameObject;
            float weight = (float)prefabWeightPairs[i + 1];

            if (prefab == null || weight <= 0f) continue;

            if (r < weight)
                return prefab;

            r -= weight;
        }

        // fallback
        return null;
    }

    // spawning

    private void HandleSpawning(float deltaTime)
    {

        if (spawnPoints == null || spawnPoints.Length == 0) return;

        if (seekerPrefab == null && walkingTurretPrefab == null && // dont spawn if prefabs are missing (used for testing before i implemented these enemies)
            mechaMinePrefab == null && deadmanPrefab == null &&
            berserkerPrefab == null)
        {
            return;
        }

        float baseRateThisWave = baseSpawnRate * Mathf.Pow(spawnRateGrowthPerWave, currentWave - 1); // spawn rate increases with wave num
        float currentSpawnRate = baseRateThisWave * Mathf.Pow(spawnRateGrowthOverTime, waveElapsedTime); // spawn rate ramps up over wave duration

        spawnAccumulator += currentSpawnRate * deltaTime;

        int toSpawn = Mathf.FloorToInt(spawnAccumulator); // convert accumulator into integer
        if (toSpawn <= 0) return;

        spawnAccumulator -= toSpawn;

        for (int i = 0; i < toSpawn; i++) // spawn multiple enemies if accumulator is large
        {
            SpawnOneEnemy();
        }
    }


    private void SpawnOneEnemy()
    {
        Vector3 spawnPos = GetDynamicSpawnPosition();
        Quaternion spawnRot = Quaternion.identity;

        // select enemy type based on wave
        GameObject prefab = ChooseEnemyPrefabForCurrentWave();
        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, spawnPos, spawnRot, enemyParent);

        ApplyEnemyScaling(enemy); // apply scaling
        activeEnemies.Add(enemy); // track enemy
    }


    private Vector3 GetDynamicSpawnPosition()
    {
        float minRadius = minSpawnDistanceFromPlayer;
        float maxRadius = minSpawnDistanceFromPlayer + spawnRadiusExtra;

        // how close the player is to bounds
        float distLeft = player.position.x - arenaMin.x;
        float distRight = arenaMax.x - player.position.x;
        float distBottom = player.position.z - arenaMin.y;
        float distTop = arenaMax.y - player.position.z;

        // which walls player is close to
        bool nearLeft = distLeft < wallAvoidThreshold;
        bool nearRight = distRight < wallAvoidThreshold;
        bool nearBottom = distBottom < wallAvoidThreshold;
        bool nearTop = distTop < wallAvoidThreshold;

        Vector3 pos = player.position;

        const int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++) // try few random directions
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(minRadius, maxRadius);

            Vector2 dir2 = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // if player near wall, avoid spawning on that side
            if (nearLeft && dir2.x < 0f) continue;
            if (nearRight && dir2.x > 0f) continue;
            if (nearBottom && dir2.y < 0f) continue;
            if (nearTop && dir2.y > 0f) continue;

            Vector3 offset = new Vector3(dir2.x, 0f, dir2.y) * radius;
            pos = player.position + offset;
            break;
        }

        // final safety clamp
        pos.x = Mathf.Clamp(pos.x, arenaMin.x, arenaMax.x);
        pos.z = Mathf.Clamp(pos.z, arenaMin.y, arenaMax.y);

        return pos;
    }

    private void KillAllEnemies() // destroys every enemy inside activeEnemies
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
    }

    // UI
    private void UpdateWaveUI()
    {
        waveText.text = $"Wave {currentWave}";
    }

    private void UpdateTimerUI()
    {
        int seconds = Mathf.CeilToInt(waveTimer);
        timerText.text = seconds.ToString();
    }

    // difficulty scaling
    public float GetEnemyDamageMultiplier()
    {
        int w = Mathf.Max(0, currentWave - 1);
        return Mathf.Pow(damageGrowthPerWave, w);
    }

    private void ApplyEnemyScaling(GameObject enemy)
    {
        int w = Mathf.Max(0, currentWave - 1);

        // HP scaling
        MechHealth health = enemy.GetComponent<MechHealth>();
        if (health != null)
        {
            float hpMult = Mathf.Pow(healthGrowthPerWave, w);

            health.maxCoreHealth *= hpMult;
            health.currentCoreHealth = health.maxCoreHealth;

            health.maxShield *= hpMult;
            health.currentShield = health.maxShield;
        }

        // movement scaling
        float speedMult = Mathf.Pow(speedGrowthPerWave, w);
        float accelMult = Mathf.Pow(accelGrowthPerWave, w);
        float decelMult = Mathf.Pow(decelGrowthPerWave, w);
        float dmgMult = Mathf.Pow(damageGrowthPerWave, w);

        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.maxSpeed *= speedMult;
            ai.acceleration *= accelMult;
            ai.deceleration *= decelMult;
            ai.contactDamage *= dmgMult;
        }
        else
        {
            BaseRangedAI ranged = enemy.GetComponent<BaseRangedAI>();
            if (ranged != null)
            {
                ranged.maxSpeed *= speedMult;
                ranged.acceleration *= accelMult;
                ranged.deceleration *= decelMult;
                ranged.contactDamage *= dmgMult;
            }
        }
    }
}
