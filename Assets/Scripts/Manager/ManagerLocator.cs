using Syacapachi.util;
using Unity.Netcode;
using UnityEngine;
using Syacapachi.Manager;

public class ManagerLocator : MonoBehaviour
{
    public static ManagerLocator Instance;
    [field:SerializeField] public PlayerManager AllPlayerManager { get; private set; }
    [field:SerializeField] public NetworkGameManager AllGameManager { get; private set; }
    [field:SerializeField] public NetworkObjectPool AllNetworkObjectPool { get; private set; }
    [field:SerializeField] public LocalObjectPoolManager LocalObjectPool { get; private set; }
    [field:SerializeField] public TutorialManager TutorialManager { get; private set; }
    [field:SerializeField] public CheckPointManager CheckPointManager { get; private set; }

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.Log("ManagerLocator dont allow to maltiplicate");
            Destroy(gameObject);
        }
    }

}
