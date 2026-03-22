using Unity.XR.CoreUtils;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class PlayerPropaty : NetworkBehaviour
{
    public readonly static Dictionary<PlayerJob, string> jobToLayerDic = new()
    {
        { PlayerJob.Nothing,"Default" },
        { PlayerJob.Human, "Human" },
        { PlayerJob.Ghost, "Ghost" },
        { PlayerJob.Both, "Both" }
    };
    public readonly static Dictionary<string, PlayerJob> layerToJobDic = jobToLayerDic.ToDictionary(pair=>pair.Value,pair=>pair.Key);
 
    [SerializeField] GameObject PlayerRoot;
    [Flags]
    public enum PlayerJob { 
        Nothing = 0,
        Human = 1,
        Ghost = 1<<1,
        Both = Human | Ghost,
    }
    public event Action<PlayerJob> OnJobChanged;
    private readonly NetworkVariable<int> PlayerLayer = new (
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
        );
    [SerializeField] PlayerJob playerjob = PlayerJob.Both;
    
    public PlayerJob Job {
        get => playerjob; 
        set 
        {
            if (playerjob != value)
            {
                playerjob = value;
                string layerName = jobToLayerDic[playerjob];
                PlayerLayer.Value = LayerMask.NameToLayer(layerName);         
                OnJobChanged?.Invoke(playerjob);
            }
        }
    }
    

    /*
    NetworkVariable<PlayerJob> job =
    new NetworkVariable<PlayerJob>(
        PlayerJob.Nothing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public PlayerJob Job
    {
        get => job.Value;
        set => job.Value = value;
    }
    */
    public bool CanSeeEnemy
    {
        get => playerjob != PlayerJob.Human;
    }
    [SerializeField] bool IsDebugMode = true;
    private InputAction changeJobAction;
    public override void OnNetworkSpawn()
    {
        if (IsOwner && IsDebugMode)
        {
            changeJobAction = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.playerInput.actions["SwitchJob"];
            changeJobAction.performed += OnJobChangeHandle;
            OnJobChanged?.Invoke(Job);
        }
        PlayerLayer.OnValueChanged += OnValueChanged;
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner && IsDebugMode)
        {
            changeJobAction.performed -= OnJobChangeHandle;
        }
        PlayerLayer.OnValueChanged -= OnValueChanged;
    }
    public override void OnGainedOwnership()
    {
        changeJobAction.performed += OnJobChangeHandle;
    }
    public override void OnLostOwnership()
    {
        changeJobAction.performed -= OnJobChangeHandle;
    }
    /// <summary>
    /// 物理演算はサーバーで行われるため、クライアント側でレイヤーを変更しても意味がない。
    /// </summary>
    /// <param name="previousValue"></param>
    /// <param name="newValue"></param>
    private void OnValueChanged(int previousValue, int newValue)
    {
        if (previousValue != newValue)
        {
            PlayerRoot.SetLayerRecursively(newValue);
            playerjob = layerToJobDic[LayerMask.LayerToName(newValue)];
        }
    }
    private void OnJobChangeHandle(InputAction.CallbackContext context)
    {
        Debug.Log("SwitchJob action performed! Current job: " + Job);
        Job = Job switch
        {
            PlayerJob.Nothing => PlayerJob.Human,
            PlayerJob.Human => PlayerJob.Ghost,
            PlayerJob.Ghost => PlayerJob.Both,
            PlayerJob.Both => PlayerJob.Human,
            _ => throw new System.NotImplementedException(),
        };
        Debug.Log("Job changed to: " + Job);
    }

}
