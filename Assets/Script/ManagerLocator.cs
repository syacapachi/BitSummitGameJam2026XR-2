using Syacapachi.util;
using Unity.Netcode;
using UnityEngine;
using Syacapachi.Manager;

public class ManagerLocator : MonoBehaviour
{
    public static ManagerLocator Instance;

    [field:SerializeField] public NetworkManager NetworkManager { get; private set; }
    [field:SerializeField] public PlayerManager AllPlayerManager { get; private set; }
    [field:SerializeField] public NGameManager AllGameManager { get; private set; }
    [field:SerializeField] public NetworkObjectPool AllObjectPool { get; private set; }
    [field:SerializeField] public GameAudioManager GameAudioManager { get; private set; }
    [field:SerializeField] public RankingManager RankingManager { get; private set; }
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.Log("ManagerLocator dont allow to maltiplicate");
            Destroy(gameObject);
        }
    }

}
