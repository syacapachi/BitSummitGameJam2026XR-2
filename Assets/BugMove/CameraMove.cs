using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMove : MonoBehaviour
{
    [SerializeField] Transform playerRootTransform;
    [SerializeField] float  mouseSensitivity = 1f;
    [SerializeField] float yMinLimit = -89f;
    [SerializeField] float yMaxLimit = 89f;
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Camera localCamera;
    Vector2 cameraAngle;
    Vector2 lastSendRotation;
    InputAction lookAction;
    
    void OnEnable()
    {
        playerInput.ActivateInput();
        //Cursor.lockState = CursorLockMode.Locked;
        Vector3 angle = localCamera.transform.eulerAngles;
        cameraAngle.x = angle.y;
        cameraAngle.y = angle.x;
        lookAction = playerInput.actions["Look"];
    }
    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        playerInput.DeactivateInput();
    }

    // Update is called once per frame
    void LateUpdate()
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
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
    private void RotationChangeServerRpc(float x, float y)
    {
        playerRootTransform.rotation = Quaternion.Euler(y, x, 0);
    }
}
