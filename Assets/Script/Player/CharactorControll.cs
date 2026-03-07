using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharactorControll : NetworkBehaviour
{
    /*
     * 
     * NetworkTransform Authority Mode  ->  Server 
     * NetworkAnimator Authority Mode ->  Owner
     */
    [SerializeField] Transform playerRootTransform;
    [SerializeField] Rigidbody charactorRigidbody;
    [SerializeField] GameObject bombPrefab;
    [SerializeField] Animator animator;
    [Tooltip("NetworkAnimatorのAuthority Mode はOwner")]
    [SerializeField] NetworkAnimator networkAnimator;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction setObjectAction;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float moveSpeed = 5f;
    NetworkVariable<Vector2> moveInput = new NetworkVariable<Vector2>(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    public int JumpCount => jumpCount.Value;
    private Vector2 lastSendInput = Vector2.zero;
    // ネットワークで同期する変数の例
    [SerializeField] NetworkVariable<int> jumpCount = 
        new NetworkVariable<int>(
            //初期値
            0,
            //読める人、書ける人の権限設定
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    // Net上でオブジェクトがスポーンしたときに呼ばれる
    public override void OnNetworkSpawn()
    {
        Debug.Log($"CharactorControll spawned on network. owner:{OwnerClientId},NetworkId = {NetworkObjectId}");
        if (IsOwner)
        {
            Debug.Log("CharactorControll spawned on owner client.");
            if (ManagerLocator.Instance.PlayerManager.OwnerPlayer.IsXREnabled)
            {
                return;
            }
            ResistAction();
        }
    }
    private void ResistAction()
    {
        moveAction = ManagerLocator.Instance.PlayerManager.OwnerPlayer.playerInput.actions["Move"];
        jumpAction = ManagerLocator.Instance.PlayerManager.OwnerPlayer.playerInput.actions["Jump"];
        setObjectAction = ManagerLocator.Instance.PlayerManager.OwnerPlayer.playerInput.actions["Interact"];

        moveAction.performed += MoveActionCallback;
        moveAction.canceled += MoveActionCallback; // 入力がキャンセルされたときもコールバックを呼び出す(ゼロの検出)

        jumpAction.performed += JumpActionCallback;

        setObjectAction.performed += OnSetObjectCallback;

        jumpCount.OnValueChanged += OnJumoCountChanged;
    }
    //ネット上でオブジェクトがデスポーンしたときに呼ばれる
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            if (moveAction != null)
                moveAction.performed -= MoveActionCallback;
            if (jumpAction != null)
            {
                jumpAction.canceled -= JumpActionCallback;
                jumpAction.performed -= JumpActionCallback;
            }
            if (setObjectAction != null)
                setObjectAction.performed -= OnSetObjectCallback;
            if (jumpCount != null)
                jumpCount.OnValueChanged -= OnJumoCountChanged;
        }
        
    }
    private void OnJumoCountChanged(int oldValue, int newValue)
    {
        Debug.Log($"JumpCount changed from {oldValue} to {newValue}");
    }
    private void MoveActionCallback(InputAction.CallbackContext context)
    {
        if (IsOwner)
        {
            moveInput.Value = context.ReadValue<Vector2>();
        }
    }
    private void JumpActionCallback(InputAction.CallbackContext context)
    {
        if (IsOwner)
        {
            //Debug.Log("Jump!");
            //セットトリガーは通常のAnimatorでは反映されないので、NetworkAnimatorを使用する
            networkAnimator.SetTrigger("Jump");
            CharactorAddForceServerRpc();
            
        }
    }
    
    private void OnSetObjectCallback(InputAction.CallbackContext context)
    {
        if (IsOwner)
        {
            SpawnBomnServerRpc();
        }
    }
    //オブジェクト生成はサーバーで行う必要がある。生成したオブジェクトをクライアントに同期するためには、生成したオブジェクトのNetworkObjectコンポーネントのSpawn()メソッドを呼び出す必要がある。
    [ServerRpc]
    private void SpawnBomnServerRpc()
    {
        if (bombPrefab == null)
        {
            Debug.LogError("Bomb prefab is not assigned.");
            return;
        }
        GameObject bomb = Instantiate(bombPrefab, playerRootTransform.position + playerRootTransform.forward * 2, Quaternion.identity);
        bomb.GetComponent<NetworkObject>().Spawn();
    }
    void Update()
    {   
        if (IsOwner)
        {
            //入力がないときも、最後に送った入力をサーバーに送ることで、移動の停止をサーバーに伝える
            if (moveInput.Value != lastSendInput)
            {
                
                lastSendInput = moveInput.Value;
            }
            //NetworkAnimator(Owner Authority Mode)を使ってアニメーションを同期する
            //アニメーションのパラメーターを設定(たいていは同期される)
            animator.SetFloat("Speed", moveInput.Value.magnitude);
            animator.SetFloat("Direction", moveInput.Value.x);

            //カメラの向きに応じた移動をするかどうかは、PlayerManagerのCameraSettingで管理する、サーバーに送るときに一緒に送る
            MoveCharactor(moveInput.Value,ManagerLocator.Instance.PlayerManager.OwnerPlayer.cameraSetting.IsMainCameraActive);
        }

    }

    //サーバーで実行される関数(名前にServerRpcを付ける) 呼び出せるのはオーナー のみ
    //プレイヤーの移動をサーバーで処理する
    //NetworkTransform(Server Authority Mode)は、Serverの位置をクライアントに同期するので、サーバーに送る必要がある
    private void MoveCharactor(Vector2 inputVector,bool isWorldSpace)
    {
        //Debug.Log($"Catch {inputVector}");
        //クライアントから送られた入力をもとに、サーバー側でキャラクターを移動させる
        Vector3 move = new Vector3(inputVector.x, 0, inputVector.y);
        
        if (isWorldSpace)
        {
            playerRootTransform.Translate(move * Time.deltaTime * moveSpeed, Space.World);
        }
        else
        {
            //相対座標で移動させる。これにより、プレイヤーの向きに応じた移動が可能になる。
            playerRootTransform.Translate(move * Time.deltaTime * moveSpeed);
        }
            
    }
    //サーバー側で力を加える
    //NetworkRigidbodyは、Serverの物理演算をクライアントに同期するので、サーバーに送る必要がある
    [ServerRpc]
    private void CharactorAddForceServerRpc()
    {
        charactorRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpCount.Value += 1;
    }
    
}
