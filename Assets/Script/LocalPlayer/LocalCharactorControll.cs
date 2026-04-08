using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalCharactorControll : MonoBehaviour 
{ 
    [SerializeField] InputReciever reciever;
    public enum MoveMode { Normal, Dash, Stop}
    /*
     * 
     * NetworkTransform Authority Mode  ->  Server 
     * NetworkAnimator Authority Mode ->  Owner
     */
    [SerializeField] protected Transform playerRootTransform;
    [SerializeField] LocalCameraSetting cameraSetting;
    [SerializeField] protected GameObject bombPrefab;
    [SerializeField] protected float jumpForce = 5f;
    [SerializeField] protected float normalSpeed = 5f;
    [SerializeField] protected float dashSpeed = 10f;
    
    public bool IsStop => moveMode == MoveMode.Stop;
    Vector2 lastMove = Vector2.zero;

    protected MoveMode moveMode = MoveMode.Normal;

    private void OnEnable()
    {
        reciever.OnDashChanged += OnDashChanged;
        reciever.OnFireed += OnUIChanged;
        reciever.OnJumped += OnJunp;
    }
    private void OnDisable()
    {
        reciever.OnDashChanged -= OnDashChanged;
        reciever.OnFireed -= OnUIChanged;
        reciever.OnJumped -= OnJunp;
    }
    //サーバーで実行される関数(名前にServerRpcを付ける) 呼び出せるのはオーナー のみ
    //オブジェクト生成はサーバーで行う必要がある。生成したオブジェクトをクライアントに同期するためには、生成したオブジェクトのNetworkObjectコンポーネントのSpawn()メソッドを呼び出す必要がある。
    private void SpawnBomn()
    {
        if (bombPrefab == null)
        {
            Debug.LogError("Bomb prefab is not assigned.");
            return;
        }
        GameObject bomb = Instantiate(bombPrefab, playerRootTransform.position + playerRootTransform.forward * 2, Quaternion.identity);
        bomb.GetComponent<NetworkObject>().Spawn();
    }
    private void Update()
    {
        if (lastMove == Vector2.zero && reciever.MoveInput == lastMove) return;
        float speed = moveMode switch
        {
            MoveMode.Normal => normalSpeed,
            MoveMode.Dash => dashSpeed,
            MoveMode.Stop => 0f,
            _ => 0f,
        };
        //カメラの向きに応じた移動をするかどうかは、PlayerManagerのCameraSettingで管理する、サーバーに送るときに一緒に送る
        MoveCharactor(reciever.MoveInput * speed);
        lastMove = reciever.MoveInput;
    }

    
    //プレイヤーの移動をオーナーで処理する(OwnerAuthority)
    private void MoveCharactor(Vector2 moveVector)
    {
        Vector3 move = cameraSetting.currentActiveCamera.transform.rotation * new Vector3(moveVector.x, 0, moveVector.y);
        move.y = 0f;

        //rb.linearVelocity = move;
        //相対座標で移動させる。これにより、プレイヤーの向きに応じた移動が可能になる。
        playerRootTransform.Translate(move * Time.deltaTime);    
    }
    private void OnJunp()
    {
        Debug.Log("Jump");
        playerRootTransform.Translate(Vector3.up * jumpForce * Time.deltaTime);
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
    private void OnUIChanged()
    {
        if (moveMode == MoveMode.Stop)
        {
            //Cursor.lockState = CursorLockMode.Locked;
            moveMode = MoveMode.Normal;
        }
        else
        {
            //Cursor.lockState = CursorLockMode.None;
            moveMode = MoveMode.Stop;
        }
    }
}
