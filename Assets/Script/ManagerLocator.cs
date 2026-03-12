using UnityEngine;

public class ManagerLocator : MonoBehaviour
{
    public static ManagerLocator Instance;

    [field:SerializeField] public PlayerManager AllPlayerManager;
    [field:SerializeField] public NGameManager AllGameManager;
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
