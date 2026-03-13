using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;

public class CameraSetting : NetworkBehaviour
{
    [SerializeField] Transform cameraTransform;
    /// <summary>
    /// 個々のオブジェクトにローカルカメラを割り当てるためのフィールド。ローカルプレイヤーのカメラを指定するために使用される。
    /// </summary>
    public Camera localCamera;
    ///// <summary> 
    ///// 観戦用のカメラを割り当てるためのフィールド。観戦モードで使用されるカメラを指定するために使用される。
    ///// </summary>
    //[SerializeField] Camera mainCamera;
    [Header("GUISetting")]
    [SerializeField] CharactorControll characterControll;
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] PlayerPropaty propaty;
    [SerializeField] float mouseSensitivity = 1f;
    [SerializeField] float yMinLimit = -89f;
    [SerializeField] float yMaxLimit = 89f;
    Vector2 cameraAngle;
    Vector2 lastSendRotation;
    InputAction switchCameraAction;
    InputAction lookAction;
    public bool IsMainCameraActive => false;
    public event Action<Camera> OnCameraChanged;
    private bool isXREnabled;
    // シーン内のアクティブなカメラを追跡するためのフィールド。これにより、どのカメラが現在アクティブであるかを簡単に確認できるようになる。
    private Camera activeCamera;
    public Camera CurrentActiveCamera { 
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
                    if(activeCamera.gameObject.TryGetComponent<AudioListener>(out var lisnteb)) lisnteb.enabled = false;
                }
                activeCamera = value;
                if (activeCamera != null)
                {
                    activeCamera.enabled = true;
                    if (activeCamera.gameObject.TryGetComponent<AudioListener>(out var lisnteb)) lisnteb.enabled = true;
                    OnCameraChanged?.Invoke(activeCamera);
                }
            }
        }
    }
    public override void OnNetworkSpawn()
    {
        //メインカメラをシーン内のカメラから自動的に割り当てる。もしmainCameraがnullの場合、Camera.mainを使用してメインカメラを取得する。
        //mainCamera =
        //mainCamera != null ?
        //mainCamera : Camera.main;
        
        if (IsOwner)
        {
            //Cursor.lockState = CursorLockMode.Locked;

            //mainCamera.enabled = false;
            //if (mainCamera.gameObject.TryGetComponent<AudioListener>(out AudioListener listener)) listener.enabled = false;
            CurrentActiveCamera = localCamera;
            ResistAction();
        }
        else
        {
            //他の奴は無効にし、破壊する。これにより、他のプレイヤーのカメラが有効にならないようにし、リソースを節約する。
            localCamera.enabled = false;
            Destroy(localCamera.gameObject.GetComponent<AudioListener>());
            Destroy(localCamera.gameObject.GetComponent<UniversalAdditionalCameraData>());
            Destroy(localCamera.gameObject.GetComponent<TrackedPoseDriver>());
            //Destroy(localCamera);
        }
    }
    private void ResistAction()
    {
        Vector3 angle = localCamera.transform.eulerAngles;
        //xとyを入れ替える。これにより、カメラの回転がプレイヤーの入力に対して正しく反応するようになる。
        cameraAngle.x = angle.y;
        cameraAngle.y = angle.x;
        switchCameraAction = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.playerInput.actions["SwitchCamera"];
        lookAction = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.playerInput.actions["Look"];

        //switchCameraAction.performed += SwitchCamera;
    }
    public override void OnNetworkDespawn()
    {
        //if (IsOwner && switchCameraAction != null)
        //{ 
        //    switchCameraAction.performed -= SwitchCamera;
        //}
    }
    private void LateUpdate()
    {
        if (IsOwner)
        {
            //オーナーのクライアントで、現在アクティブなカメラがローカルカメラである場合、ローカルカメラの位置と回転をプレイヤーの位置と回転に合わせて更新する。これにより、ローカルカメラがプレイヤーの動きに追従するようになる。
            if (CurrentActiveCamera == localCamera)
            {
                
                Vector2 rotation = lookAction.ReadValue<Vector2>();
                cameraAngle.x += rotation.x * mouseSensitivity;
                cameraAngle.y = ClampAngle(cameraAngle.y - rotation.y * mouseSensitivity, yMinLimit, yMaxLimit);

                if (cameraAngle != lastSendRotation)
                {
                    RotationChange(cameraAngle.x, cameraAngle.y);
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

    private void RotationChange(float x, float y)
    {
        cameraTransform.rotation = Quaternion.Euler(y, x, 0);
    }
    //private void SwitchCamera(InputAction.CallbackContext context)
    //{
    //    //Debug.Log("SwitchCamera");
    //    if (CurrentActiveCamera == localCamera)
    //    {
    //        CurrentActiveCamera = mainCamera;
    //        //カーソルの設定を行う。これにより、ゲーム中にカーソルが画面内に固定され、プレイヤーがマウスを動かすことでカメラを回転させることができるようになる。
    //        //None 自由
    //        //Locked 画面中央に固定
    //        //Confined 画面内に制限
    //        Cursor.lockState = CursorLockMode.None;
    //    }
    //    else
    //    {
    //        CurrentActiveCamera = localCamera;
    //        Cursor.lockState = CursorLockMode.Locked;
    //    }
    //}
    private void OnGUI()
    {
        if(!IsClient) return;
        if(!IsOwner)
        {
            //オーナーでないプレイヤーの情報を表示するためのコード。オーナーでないプレイヤーのカメラが有効な場合は、そのカメラの位置にプレイヤーのIDとジャンプ回数を表示する。
            Vector3 positon = ManagerLocator.Instance.AllPlayerManager.LocalOwnerPlayer.cameraSetting.CurrentActiveCamera.WorldToScreenPoint(cameraTransform.position);
            GUI.Label(new Rect(positon.x, Screen.height - positon.y - 120, 100, 20), $"{OwnerClientId}");
            GUI.Label(new Rect(positon.x, Screen.height - positon.y - 100, 100, 20), $"jump: {characterControll.JumpCount}");
            return;         
        }

        //if (mainCamera != null && CurrentActiveCamera == mainCamera)
        //{
        //    Vector3 positon = mainCamera.WorldToScreenPoint(cameraTransform.position);
        //    if (positon.z < 0) return; // カメラの前にいる場合のみ表示
        //    GUI.Label(new Rect(positon.x, Screen.height - positon.y - 60, 100, 20), IsOwner ? "You" : "");
        //    GUI.Label(new Rect(positon.x, Screen.height - positon.y - 30, 100, 20), $"jump: {characterControll.JumpCount}");
        //    GUI.Label(new Rect(positon.x, Screen.height - positon.y, 100, 20), $"HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
        //}
        //GUI.Label(new Rect(10, 10, 200, 20), $"Job: {propaty.Job}");

    }
}
