using UnityEngine;
using UnityEngine.XR;

public class LocalCameraSetting : MonoBehaviour
{
    [SerializeField] LocalCharactorControll characterControll;
    /// <summary>
    /// 個々のオブジェクトにローカルカメラを割り当てるためのフィールド。ローカルプレイヤーのカメラを指定するために使用される。
    /// </summary>
    [SerializeField] Camera localCamera;
    
    [SerializeField] float mouseSensitivity = 1f;
    [SerializeField] float yMinLimit = -89f;
    [SerializeField] float yMaxLimit = 89f;
    [Header("Subscribe Event")]
    [SerializeField] Vector2Event look;
    Vector2 cameraAngle;
    Vector2 lastSendRotation;
    
    // シーン内のアクティブなカメラを追跡するためのフィールド。これにより、どのカメラが現在アクティブであるかを簡単に確認できるようになる。
    public Camera CurrentActiveCamera { 
        get => localCamera; 
    }
    private void OnEnable()
    {
        //Cursor.lockState = CursorLockMode.Locked;

        Vector3 angle = localCamera.transform.eulerAngles;
        //xとyを入れ替える。これにより、カメラの回転がプレイヤーの入力に対して正しく反応するようになる。
        cameraAngle.x = angle.y;
        cameraAngle.y = angle.x;
    }

    private bool IsCameraUpdateable()
    {
        return !characterControll.IsStop;
    }
    public void LateUpdate()
    {
        if (XRSettings.isDeviceActive) return;
        Vector2 rotation = look.CurrentValue;
        if(rotation == Vector2.zero) return;
        //オーナーのクライアントで、現在アクティブなカメラがローカルカメラである場合、ローカルカメラの位置と回転をプレイヤーの位置と回転に合わせて更新する。これにより、ローカルカメラがプレイヤーの動きに追従するようになる。
        if (IsCameraUpdateable())
        {
            cameraAngle.x += rotation.x * mouseSensitivity;
            cameraAngle.y = ClampAngle(cameraAngle.y - rotation.y * mouseSensitivity, yMinLimit, yMaxLimit);

            if (cameraAngle != lastSendRotation)
            {
                RotationChange(cameraAngle.x, cameraAngle.y);
                lastSendRotation = cameraAngle;
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
        localCamera.transform.rotation = Quaternion.Euler(y, x, 0);
    }
    
}
