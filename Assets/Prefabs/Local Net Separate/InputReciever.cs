
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReciever : MonoBehaviour
{
    [SerializeField] PlayerInput PlayerInput;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction setObjectAction;
    InputAction dashAction;
    InputAction fireAction;
    InputAction switchCameraAction;
    InputAction lookAction;
    InputAction switchJobAction;
    InputAction markerAction;
    Vector2 moveInput = Vector2.zero;
    Vector2 lookInput = Vector2.zero;
    public Vector2 MoveInput => moveInput;
    public Vector2 LookInput => lookInput;

    public event Action OnJumped;
    public event Action OnSetted;
    public event Action<bool> OnDashChanged;
    public event Action OnCameraChanged;
    public event Action OnFireed;
    public event Action OnSwirchJob;
    public event Action OnMarker;
    protected virtual void Awake()
    {
        moveAction = PlayerInput.actions["Move"];
        lookAction = PlayerInput.actions["Look"];
        jumpAction = PlayerInput.actions["Jump"];
        setObjectAction = PlayerInput.actions["Interact"];
        dashAction = PlayerInput.actions["Sprint"];
        fireAction = PlayerInput.actions["Fire"];
        switchCameraAction = PlayerInput.actions["SwitchCamera"];
        
        switchJobAction = PlayerInput.actions["SwitchJob"];
        markerAction = PlayerInput.actions["Marker"];
    }
    protected virtual void OnEnable()
    {
        switchCameraAction.performed += SwitchCamera;
        moveAction.performed += MoveActionCallback;
        moveAction.canceled += MoveActionCallback; // 入力がキャンセルされたときもコールバックを呼び出す(ゼロの検出)

        lookAction.performed += LookActionCallback;
        lookAction.canceled += LookActionCallback;

        jumpAction.performed += JumpActionCallback;

        setObjectAction.performed += OnSetObjectCallback;

        dashAction.performed += DashActionCallback;
        dashAction.canceled += DashActionCallback;

        fireAction.performed += UIActionCallback;

        switchJobAction.performed += OnTakeActionCallback;
        markerAction.performed += OnRelseaseActionCallback;
    }
    protected virtual void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= MoveActionCallback;
            moveAction.canceled -= MoveActionCallback;
        }
        if (lookAction != null)
        {
            lookAction.performed -= LookActionCallback;
            lookAction.canceled -= LookActionCallback;
        }
        if (jumpAction != null)
        {
            jumpAction.performed -= JumpActionCallback;
        }
        if (setObjectAction != null)
        {
            setObjectAction.performed -= OnSetObjectCallback;
        }

        if (dashAction != null)
        {
            dashAction.performed -= DashActionCallback;
            dashAction.canceled -= DashActionCallback;
        }

        if (fireAction != null)
        {
            fireAction.performed -= UIActionCallback;
        }
        switchCameraAction.performed -= SwitchCamera;
        switchJobAction.performed -= OnTakeActionCallback;
        markerAction.performed -= OnRelseaseActionCallback;
    }
    private void MoveActionCallback(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    private void LookActionCallback(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    private void JumpActionCallback(InputAction.CallbackContext context)
    {
        OnJumped?.Invoke();
    }

    private void OnSetObjectCallback(InputAction.CallbackContext context)
    {
        OnSetted?.Invoke();
    }
    private void DashActionCallback(InputAction.CallbackContext context)
    {
        OnDashChanged?.Invoke(context.ReadValue<float>() > 0.7f);
    }
    private void UIActionCallback(InputAction.CallbackContext context)
    {
        OnFireed?.Invoke();
    }
    private void SwitchCamera(InputAction.CallbackContext context)
    {
        OnCameraChanged?.Invoke();
    }
    private void OnTakeActionCallback(InputAction.CallbackContext context)
    {
        OnSwirchJob?.Invoke();
    }
    private void OnRelseaseActionCallback(InputAction.CallbackContext context)
    {
        OnMarker?.Invoke();
    }
}
