using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletPool : MonoBehaviour
{
    public static EnemyBulletPool Instance { get; private set; }

    public GameObject bulletPrefab;
    public int initialPoolSize = 30;
    public bool allowPoolGrowth = true;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Prewarm(initialPoolSize);
    }

    private void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(bulletPrefab);
            go.SetActive(false);
            go.transform.parent = transform;
            pool.Enqueue(go);
        }
    }

    public GameObject Get()
    {
        GameObject go;
        if (pool.Count > 0)
        {
            go = pool.Dequeue();
        }
        else if (allowPoolGrowth)
        {
            go = Instantiate(bulletPrefab);
        }
        else
        {
            go = pool.Dequeue();
        }

        go.SetActive(true);
        ResetPooledObject(go);
        return go;
    }

    public void Return(GameObject go)
    {
        go.SetActive(false);
        pool.Enqueue(go);
    }

    private void ResetPooledObject(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}
