using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;

public class LocalCharactorControll : MonoBehaviour 
{ 
    public enum MoveMode { Normal, Dash, Stop}
    /*
     * 
     * NetworkTransform Authority Mode  ->  Server 
     * NetworkAnimator Authority Mode ->  Owner
     */
    [SerializeField] protected Transform playerRootTransform;
    [SerializeField] LocalCameraSetting cameraSetting;
    [SerializeField] protected float jumpForce = 5f;
    [SerializeField] protected float normalSpeed = 5f;
    [SerializeField] protected float dashSpeed = 10f;
    [Header("Subscribe Event")]
    [SerializeField] VoidEvent jumpEvent;
    [SerializeField] BoolEvent dashEvent;
    [SerializeField] Vector2Event moveEvent;
    public bool IsStop => moveMode == MoveMode.Stop;
    Vector2 lastMove = Vector2.zero;

    protected MoveMode moveMode = MoveMode.Normal;

    private void OnEnable()
    {
        jumpEvent.Register(OnJunp);
        dashEvent.Register(OnDashChanged);
    }
    private void OnDisable()
    {
        jumpEvent.Unregister(OnJunp);
        dashEvent.Unregister(OnDashChanged);
    }
    private void Update()
    {
        Vector2 moveInput = moveEvent.CurrentValue;
        if(XRSettings.isDeviceActive) return;
        if (lastMove == Vector2.zero && moveInput == lastMove) return;
        float speed = moveMode switch
        {
            MoveMode.Normal => normalSpeed,
            MoveMode.Dash => dashSpeed,
            MoveMode.Stop => 0f,
            _ => 0f,
        };
        //カメラの向きに応じた移動をするかどうかは、PlayerManagerのCameraSettingで管理する、サーバーに送るときに一緒に送る
        MoveCharactor(moveInput * speed);
        lastMove = moveInput;
    }

    
    //プレイヤーの移動をオーナーで処理する(OwnerAuthority)
    private void MoveCharactor(Vector2 moveVector)
    {
        Vector3 move = cameraSetting.CurrentActiveCamera.transform.rotation * new Vector3(moveVector.x, 0, moveVector.y);
        move.y = 0f;

        //rb.linearVelocity = move;
        //相対座標で移動させる。これにより、プレイヤーの向きに応じた移動が可能になる。
        playerRootTransform.Translate(move * Time.deltaTime);    
    }
    private void OnJunp()
    {
        Debug.Log("Jump");
        playerRootTransform.Translate(jumpForce * Time.deltaTime * Vector3.up);
    }
    private void OnDashChanged(bool isDash)
    {
        if (isDash)
        {
            moveMode = MoveMode.Dash;
        }
        else
        {
            moveMode = MoveMode.Normal;
        }
    }
}
