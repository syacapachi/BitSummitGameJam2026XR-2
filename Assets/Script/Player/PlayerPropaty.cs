using Unity.XR.CoreUtils;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerPropaty : NetworkBehaviour
{
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
    [SerializeField] PlayerJob playerjob = PlayerJob.Human;
    public PlayerJob Job {
        get => playerjob; 
        set 
        {
            if (playerjob != value)
            {
                playerjob = value;
                string layerName = playerjob switch
                {
                    PlayerJob.Nothing => "Default",
                    PlayerJob.Human => "Human",
                    PlayerJob.Ghost => "Ghost",
                    PlayerJob.Both => "Default",
                    _ => throw new System.NotImplementedException(),
                };
                PlayerLayer.Value = LayerMask.NameToLayer(layerName);         
                OnJobChanged?.Invoke(playerjob);
            }
        }
    }
    public bool CanSeeEnemy
    {
        get => playerjob != PlayerJob.Human;
    }
    
    private InputAction changeJobAction;
    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
            changeJobAction = ManagerLocator.Instance.PlayerManager.OwnerPlayer.playerInput.actions["SwitchJob"];
            changeJobAction.performed += OnJobChangeHandle;
            OnJobChanged?.Invoke(Job);
        }
        PlayerLayer.OnValueChanged += OnValueChanged;
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
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
        PlayerRoot.SetLayerRecursively(newValue);
    }
    private void OnJobChangeHandle(InputAction.CallbackContext context)
    {
        Job = Job switch
        {
            PlayerJob.Nothing => PlayerJob.Human,
            PlayerJob.Human => PlayerJob.Ghost,
            PlayerJob.Ghost => PlayerJob.Both,
            PlayerJob.Both => PlayerJob.Human,
            _ => throw new System.NotImplementedException(),
        };
    }
    private void OnGUI()
    {
        if (IsOwner)
        {
            GUI.Label(new Rect(10, 10, 200, 20), $"Job: {Job}");
        }
    }
}
