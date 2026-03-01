using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    // config
    public float lifetime = 0.6f;
    public float floatSpeed = 1f;
    public TextMeshPro text;
    public float worldScale = 0.02f;

    float timer;

    private void Awake()
    {
        transform.localScale = Vector3.one * worldScale;
    }

    public void Initialize(float damage, Color color)
    {
        timer = lifetime;
        text.text = Mathf.RoundToInt(damage).ToString();
        text.color = color;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        // move up slightly
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // fade out
        float t = Mathf.Clamp01(timer / lifetime);
        Color c = text.color;
        c.a = t;
        text.color = c;

        if (timer <= 0f)
            Destroy(gameObject);
    }
}
