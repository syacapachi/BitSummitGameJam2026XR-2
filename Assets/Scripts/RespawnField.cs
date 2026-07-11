using UnityEngine;
using Syacapachi.Attribute;

public class RespawnField : MonoBehaviour
{
    private enum SettingType
    {
        Transform,
        Vector3
    }
    [SerializeField] SettingType settingType;
    [EnableIfEnum(nameof(settingType), true, (int)SettingType.Vector3)] 
    [SerializeField] Vector3 respawnPositon = new Vector3(0, 5, 0);
    [EnableIfEnum(nameof(settingType), true, (int)SettingType.Transform)]
    [SerializeField] Transform respawnTransform;
    private void Start()
    {
        if (settingType == SettingType.Transform && respawnTransform != null)
        {
            respawnPositon = respawnTransform.position;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Collison {collision.gameObject.name} to {collision.body}", collision.gameObject);
        Respawn(collision.gameObject, respawnPositon);  
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger {other.name}", other);
        Respawn(other.gameObject, respawnPositon);
    }
    [OnInspectorButton("Respawn Objects")]
    private static void Respawn(GameObject obj, Vector3 respawnPositon)
    {
        obj.transform.SetPositionAndRotation(respawnPositon, Quaternion.Euler(Vector3.zero));
        if (obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
