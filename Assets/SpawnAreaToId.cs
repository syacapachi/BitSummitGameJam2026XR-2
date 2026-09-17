using Syacapachi.Attribute;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SpawnAreaToId : MonoBehaviour
{
    [SerializeField] Vector3 centerPos;
    [SerializeField] Vector3 size;
    [SerializeField] int[] areaIdArray;
    private static readonly List<int> searchListCashe = new();
    [OnInspectorButton]
    private void Seach()
    {
        SearchId(centerPos, size / 2,out areaIdArray);
    }
    public static void SearchId(Vector3 center, Vector3 halfsize,out int[] idArray)
    {
        Collider[] colliders = Physics.OverlapBox(center, halfsize);
        searchListCashe.Clear();
        foreach (var collider in colliders)
        {
           
            if(collider.TryGetComponent<SpawnPointMarker>(out var s))
            {
                searchListCashe.Add(s.SpawnPointId);
            }
        }
        idArray = searchListCashe.OrderBy(t=> t).ToArray();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(centerPos, size);
    }
}
