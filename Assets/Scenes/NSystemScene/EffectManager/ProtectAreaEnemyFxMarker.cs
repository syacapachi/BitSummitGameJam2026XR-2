using UnityEngine;

public class ProtectAreaEnemyFxMarker : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        NEnemyDeathFxEmitter emitter =
            other.GetComponent<NEnemyDeathFxEmitter>() ??
            other.GetComponentInParent<NEnemyDeathFxEmitter>();

        if (emitter != null)
        {
            emitter.MarkReachedProtectAreaServer();
        }
    }
}