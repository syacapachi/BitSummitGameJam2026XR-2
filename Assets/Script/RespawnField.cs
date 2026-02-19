using UnityEngine;

public class RespawnField : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        collision.gameObject.transform.position = new Vector3(0, 5, 0);
        collision.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
    }
}
