using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSetting : NetworkBehaviour
{
    [SerializeField] Transform playerRootTransform;
    /// <summary>
    /// 個々のオブジェクトにローカルカメラを割り当てるためのフィールド。ローカルプレイヤーのカメラを指定するために使用される。
    /// </summary>
    [SerializeField] Camera localCamera;
    /// <summary> 
    /// 観戦用のカメラを割り当てるためのフィールド。観戦モードで使用されるカメラを指定するために使用される。
    /// </summary>
    [SerializeField] Camera mainCamera;
    [SerializeField] CharactorControll characterControll;
    [SerializeField] float mouseSensitivity = 1f;
    [SerializeField] float yMinLimit = -89f;
    [SerializeField] float yMaxLimit = 89f;
    Vector2 cameraAngle;
    Vector2 lastSendRotation;
    InputAction switchCameraAction;
    InputAction lookAction;
    public bool IsMainCameraActive => currentActiveCamera == mainCamera;
    // シーン内のアクティブなカメラを追跡するためのフィールド。これにより、どのカメラが現在アクティブであるかを簡単に確認できるようになる。
    private Camera activeCamera;
    public Camera currentActiveCamera { 
        get => activeCamera; 
        private set
        {
            if (!IsOwner)
            {
                Debug.LogWarning("Only the owner can change the active camera.");
                return;
            }
            if (activeCamera != value)
            {
                if (activeCamera != null)
                {
                    activeCamera.enabled = false;
                }
                activeCamera = value;
                if (activeCamera != null)
                {
                    activeCamera.enabled = true;
                }
            }
        }
    }
    public override void OnNetworkSpawn()
    {
        //メインカメラをシーン内のカメラから自動的に割り当てる。もしmainCameraがnullの場合、Camera.mainを使用してメインカメラを取得する。
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;

            mainCamera.enabled = false;
            localCamera.enabled = true;
            activeCamera = localCamera;

            Vector3 angle = localCamera.transform.eulerAngles;
            //xとyを入れ替える。これにより、カメラの回転がプレイヤーの入力に対して正しく反応するようになる。
            cameraAngle.x = angle.y;
            cameraAngle.y = angle.x;

            switchCameraAction = PlayerManager.Instance.playerInput.actions["SwitchCamera"];
            lookAction = PlayerManager.Instance.playerInput.actions["Look"];

            switchCameraAction.performed += SwitchCamera;
        }
        else
        {
            //他の奴は無効にし、破壊する。これにより、他のプレイヤーのカメラが有効にならないようにし、リソースを節約する。
            localCamera.enabled = false;
            //Destroy(localCamera);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        { 
            switchCameraAction.performed -= SwitchCamera;
        }
    }
    private void LateUpdate()
    {
        if (IsOwner)
        {
            //オーナーのクライアントで、現在アクティブなカメラがローカルカメラである場合、ローカルカメラの位置と回転をプレイヤーの位置と回転に合わせて更新する。これにより、ローカルカメラがプレイヤーの動きに追従するようになる。
            if (currentActiveCamera == localCamera)
            {
                
                Vector2 rotation = lookAction.ReadValue<Vector2>();
                cameraAngle.x += rotation.x * mouseSensitivity;
                cameraAngle.y = ClampAngle(cameraAngle.y - rotation.y * mouseSensitivity, yMinLimit, yMaxLimit);

                if (cameraAngle != lastSendRotation)
                {
                    RotationChangeServerRpc(cameraAngle.x, cameraAngle.y);
                    lastSendRotation = cameraAngle;
                }
            }
        }
    }
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    //サーバーに回転の変更を通知するためのServerRpc。これにより、プレイヤーの回転がサーバーに伝えられ、他のクライアントにも反映されるようになる。
    [ServerRpc]
    private void RotationChangeServerRpc(float x, float y)
    {
        playerRootTransform.rotation = Quaternion.Euler(y, x, 0);
    }
    private void SwitchCamera(InputAction.CallbackContext context)
    {
        Debug.Log("SwitchCamera");
        if (localCamera.enabled)
        {
            currentActiveCamera = mainCamera;
            //カーソルの設定を行う。これにより、ゲーム中にカーソルが画面内に固定され、プレイヤーがマウスを動かすことでカメラを回転させることができるようになる。
            //None 自由
            //Locked 画面中央に固定
            //Confined 画面内に制限
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            currentActiveCamera = localCamera;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    private void OnGUI()
    {
        if(!IsOwner)
        {
            //オーナーでないプレイヤーの情報を表示するためのコード。オーナーでないプレイヤーのカメラが有効な場合は、そのカメラの位置にプレイヤーのIDとジャンプ回数を表示する。
            Vector3 positon = PlayerManager.Instance.cameraSetting.currentActiveCamera.WorldToScreenPoint(playerRootTransform.position);
             GUI.Label(new Rect(positon.x, Screen.height - positon.y - 120, 100, 20), $"{OwnerClientId}");
             GUI.Label(new Rect(positon.x, Screen.height - positon.y - 100, 100, 20), $"jump: {characterControll.JumpCount}");
                            
        }

        else if (mainCamera != null && mainCamera.enabled)
        {
            Vector3 positon = mainCamera.WorldToScreenPoint(playerRootTransform.position);
            if (positon.z < 0) return; // カメラの前にいる場合のみ表示
            GUI.Label(new Rect(positon.x, Screen.height - positon.y - 60, 100, 20), IsOwner ? "You" : "");
            GUI.Label(new Rect(positon.x, Screen.height - positon.y - 30, 100, 20), $"jump: {characterControll.JumpCount}");
            GUI.Label(new Rect(positon.x, Screen.height - positon.y, 100, 20), $"HP: {PlayerManager.Instance.playerHealth.Health.Value}/{PlayerManager.Instance.playerHealth.MaxHealth}");
        }
            
    }
}
