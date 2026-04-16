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
    [EnableIfEnum(nameof(settingType), true,SettingType.Vector3)] 
    [SerializeField] Vector3 respawnPositon = new Vector3(0, 5, 0);
    [EnableIfEnum(nameof(settingType), true, SettingType.Transform)]
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
        Respawn(collision.gameObject);
        
    }
    private void OnTriggerEnter(Collider other)
    {
        Respawn(other.gameObject);
    }
    [OnInspectorButton("Respawn Objects")]
    private void Respawn(GameObject obj)
    {
        obj.transform.position = respawnPositon;
        obj.transform.rotation = Quaternion.Euler(Vector3.zero);
        if(obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        
    }
}
