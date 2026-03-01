using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public float knockbackForce = 100f;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            Vector3 dir = (transform.position - collision.transform.position).normalized;
            rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
        }
    }
}
