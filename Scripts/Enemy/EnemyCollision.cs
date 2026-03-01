using UnityEngine;

public class EnemyCollision : MonoBehaviour
{
    public float knockbackForce;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Vector3 dir = (transform.position - collision.transform.position).normalized;
            rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
        }
    }
}
