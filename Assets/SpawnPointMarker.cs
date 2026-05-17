using Syacapachi.Attribute;
using UnityEngine;
[RequireComponent(typeof(Collider))]
public class SpawnPointMarker : MonoBehaviour
{
    [SerializeField,ReadOnly] int spanwnPointId = 0;

    public int SpawnPointId {  get { return spanwnPointId; } set { spanwnPointId = value; }}
#if UNITY_EDITOR
    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        c.isTrigger = true;
    }
#endif
}
