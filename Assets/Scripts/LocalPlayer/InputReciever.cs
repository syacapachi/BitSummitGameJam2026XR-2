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
    InputAction uiAction;
    Vector2 moveInput = Vector2.zero;
    Vector2 lookInput = Vector2.zero;
    public Vector2 MoveInput => moveInput;
    public Vector2 LookInput => lookInput;
    [Header("Publish Event")]
    [SerializeField] Vector2Event moveEvent;
    [SerializeField] Vector2Event lookEvent;
    [SerializeField] VoidEvent jumpEvent;
    [SerializeField] VoidEvent setEvent;
    [SerializeField] BoolEventSO dashEvent;
    [SerializeField] VoidEvent fireEvent;
    [SerializeField] VoidEvent cameraSwitchEvent;
    [SerializeField] VoidEvent switchJobEvent;
    [SerializeField] VoidEvent markerEvent;
    [SerializeField] VoidEvent uiEvent;
    //public event Action OnJumped;
    //public event Action OnSetted;
    //public event Action<bool> OnDashChanged;
    //public event Action OnCameraChanged;
    //public event Action OnFireed;
    //public event Action OnSwirchJob;
    //public event Action OnMarker;
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
        uiAction = PlayerInput.actions["ShowUI"];
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

        fireAction.performed += FireActionCallback;

        switchJobAction.performed += OnSwirchJobCallback;
        markerAction.performed += OnMarkerCallback;

        uiAction.performed += UiActionCallback;
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
            fireAction.performed -= FireActionCallback;
        }
        switchCameraAction.performed -= SwitchCamera;
        switchJobAction.performed -= OnSwirchJobCallback;
        markerAction.performed -= OnMarkerCallback;
        uiAction.performed -= UiActionCallback;
    }
    private void MoveActionCallback(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        moveEvent.Invoke(moveInput);
    }
    private void LookActionCallback(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        lookEvent.Invoke(lookInput);
    }
    private void JumpActionCallback(InputAction.CallbackContext context)
    {
        jumpEvent.Invoke();
        //OnJumped?.Invoke();
    }

    private void OnSetObjectCallback(InputAction.CallbackContext context)
    {
        setEvent.Invoke();
        //OnSetted?.Invoke();
    }
    private void DashActionCallback(InputAction.CallbackContext context)
    {
        dashEvent.Invoke(context.ReadValue<float>() > 0.7f);
        //OnDashChanged?.Invoke(context.ReadValue<float>() > 0.7f);
    }
    private void FireActionCallback(InputAction.CallbackContext context)
    {
        fireEvent.Invoke();
        //OnFireed?.Invoke();
    }
    private void SwitchCamera(InputAction.CallbackContext context)
    {
        cameraSwitchEvent.Invoke();
        //OnCameraChanged?.Invoke();
    }
    private void OnSwirchJobCallback(InputAction.CallbackContext context)
    {
        switchJobEvent.Invoke();
        //OnSwirchJob?.Invoke();
    }
    private void OnMarkerCallback(InputAction.CallbackContext context)
    {
        markerEvent.Invoke();
        //OnMarker?.Invoke();
    }
    private void UiActionCallback(InputAction.CallbackContext obj)
    {
        uiEvent.Invoke();
    }
}
