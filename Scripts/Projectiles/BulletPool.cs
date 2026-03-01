using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance {  get; private set; }
    public GameObject bulletPrefab;
    public int initialPoolSize = 50;
    public bool allowPoolGrowth = true;

    private Queue<GameObject> pool = new Queue<GameObject>();

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

    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(bulletPrefab);
            go.SetActive(false);
            pool.Enqueue(go);
            go.transform.parent = transform;
        }
    }

    public GameObject Get() // gets bullet from the pool
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
            go = pool.Dequeue(); // fallback
        }

        go.SetActive(true);
        ResetPooledObject(go);
        return go; // resets the state
    }

    public void Return(GameObject go)
    {
        go.SetActive(false);
        pool.Enqueue(go);
    }

    private void ResetPooledObject(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>();
        rb.angularVelocity = Vector3.zero;

        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear();
            ps.Stop();
        }
    }
}
