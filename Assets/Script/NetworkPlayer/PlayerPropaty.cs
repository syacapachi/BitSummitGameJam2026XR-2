using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;

public class PlayerPropaty : MonoBehaviour
{
    [SerializeField] InputReciever inputReciever;
    public readonly static Dictionary<PlayerJob, PlayerLayerSettings> jobToLayerMaskDic = new()
    {
        { PlayerJob.Nothing , 
            new PlayerLayerSettings(
                LayerMask.NameToLayer("Default"),
                1 << LayerMask.NameToLayer("Default"), 
                PlayerJob.Nothing
                ) 
        },
        { PlayerJob.Human, 
            new PlayerLayerSettings(
                LayerMask.NameToLayer("Human"),
                1 << LayerMask.NameToLayer("Human"),
                PlayerJob.Human
                ) 
        },
        { PlayerJob.Ghost, 
            new PlayerLayerSettings(
                LayerMask.NameToLayer("Ghost"),
                1 << LayerMask.NameToLayer("Ghost"),
                PlayerJob.Ghost
                )
        },
        { PlayerJob.Both, 
            new PlayerLayerSettings(
                LayerMask.NameToLayer("Both"),
                (1 << LayerMask.NameToLayer("Human")) | (1 << LayerMask.NameToLayer("Ghost")),
                PlayerJob.Both
                ) 
        }
    };
 
    [SerializeField] GameObject PlayerCollider;
    [SerializeField] Camera PlayerCamera;
    [Flags]
    public enum PlayerJob { 
        Nothing = 0,//両方見えない。両方あたる。
        Human = 1,//人間だけ見える。おばけだけ当たる。
        Ghost = 1<<1,//おばけだけ見える。人間だけ当たる。
        Both = Human | Ghost,//両方見える。両方当たらない。
    }
    public readonly struct PlayerLayerSettings
    {
        public readonly int layer;//Colliderのレイヤー
        public readonly LayerMask LayerMask;//Cameraのカリングマスク
        public readonly PlayerJob Job;//プレイヤーの職業

        public PlayerLayerSettings(int layer, LayerMask playerLayer, PlayerJob job)
        {
            this.layer = layer;
            LayerMask = playerLayer;
            Job = job;
        }
    }
    public event Action<PlayerJob> OnJobChanged;
    [SerializeField] PlayerJob playerjob = PlayerJob.Both;
    
    public PlayerJob Job {
        get => playerjob; 
        set 
        {
            if (playerjob != value)
            {
                playerjob = value;
                PlayerLayerSettings settings = jobToLayerMaskDic[playerjob];
                
                OnLayerChange(settings);
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
    void OnEnable()
    {
        if (IsDebugMode)
        {
            inputReciever.OnSwirchJob += OnJobChangeHandle;
            OnJobChanged?.Invoke(Job);
        }
    }
    void OnDisable()
    {
        if (IsDebugMode)
        {
            inputReciever.OnSwirchJob -= OnJobChangeHandle;
        }
    }
    /// <summary>
    /// 物理演算はサーバーで行われるため、クライアント側でレイヤーを変更しても意味がない。
    /// </summary>
    /// <param name="previousValue"></param>
    /// <param name="newValue"></param>
    private void OnLayerChange(PlayerLayerSettings newSetting)
    {
        PlayerCollider.layer = newSetting.layer;
        // カメラのカリングマスクを更新
        PlayerCamera.cullingMask = newSetting.LayerMask;
    }
    private void OnJobChangeHandle()
    {
        Debug.Log("SwitchJob action performed! Current job: " + Job);
        Job = Job switch
        {
            PlayerJob.Nothing => PlayerJob.Human,
            PlayerJob.Human => PlayerJob.Ghost,
            PlayerJob.Ghost => PlayerJob.Both,
            PlayerJob.Both => PlayerJob.Nothing,
            _ => throw new System.NotImplementedException(),
        };
        Debug.Log("Job changed to: " + Job);
    }

}
