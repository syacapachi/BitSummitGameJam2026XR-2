using Netcode.Extensions;
using UnityEngine;

public class ManagerLocator : MonoBehaviour
{
    public static ManagerLocator Instance;

    [field:SerializeField] public PlayerManager AllPlayerManager { get; private set; }
    [field:SerializeField] public NGameManager AllGameManager { get; private set; }
    [field:SerializeField] public NetworkObjectPool AllObjectPool { get; private set; }
    [field:SerializeField] public GameAudioManager GameAudioManager { get; private set; }
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
