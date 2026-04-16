using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReciever : MonoBehaviour
{
    [Flags]
    public enum InvokeSetting
    {
        Nothing = 0,
        Performed = 1,
        Canceled = 2,
    }
    /// <summary>
    /// where T : structで、Null非許容値型に制限
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="inputConfig"></param>
    /// <param name="enable"></param>
    [Serializable]
    public class InputEventConfig<T> where T : struct
    {
        static readonly Type thisType = typeof(T);
        static readonly Type boolType = typeof(bool);
        [Header("Input Action")]
        [SerializeField] InputActionReference action;
        [Header("Publish Event")]
        [SerializeField] GameEventSOBase<T> gameEvent;
        [SerializeField] InvokeSetting setting;

        private InputAction cachedAction;
        private bool isEnabled = false;
        public void Enable()
        {
            cachedAction ??= action.action;
            if (cachedAction == null)
            {
                Debug.LogError("cachedAction is null!");
                return;
            }
            if (isEnabled) return;
            isEnabled = true;
            if ((setting & InvokeSetting.Performed) != 0)
                cachedAction.performed += OnAction;

            if ((setting & InvokeSetting.Canceled) != 0)
                cachedAction.canceled += OnAction;
        }
        public void Disable()
        {
            if (cachedAction == null) return;
            if (!isEnabled) return;
            isEnabled = false;
            if ((setting & InvokeSetting.Performed) != 0)
                cachedAction.performed -= OnAction;

            if ((setting & InvokeSetting.Canceled) != 0)
                cachedAction.canceled -= OnAction;
        }
        private void OnAction(InputAction.CallbackContext context)
        {
            if (boolType == thisType)
            {
                var val = context.ReadValueAsButton();
                gameEvent.Invoke((T)(object)val);
            }
            else
            {
                try
                {
                    gameEvent.Invoke(context.ReadValue<T>());
                } catch 
                {
                    Debug.LogWarning($"Input type mismatch: {typeof(T)}");
                }
            }
        }
    }
    [Serializable]
    public class InputEventConfig
    {
        [Header("Input Action")]
        [SerializeField] InputActionReference action;
        [Header("Publish Event")]
        [SerializeField] GameEventSOBase gameEvent;
        [SerializeField] InvokeSetting setting;

        private InputAction cachedAction;
        private bool isEnabled = false;
        public void Enable()
        {
            cachedAction ??= action.action;
            if (cachedAction == null)
            {
                Debug.LogError("cachedAction is null!");
                return;
            }
            if (isEnabled) return;
            isEnabled = true;
            if ((setting & InvokeSetting.Performed) != 0)
                cachedAction.performed += OnAction;

            if ((setting & InvokeSetting.Canceled) != 0)
                cachedAction.canceled += OnAction;
        }
        public void Disable()
        {
            if (cachedAction == null) return;
            if (!isEnabled) return;
            isEnabled = false;
            if ((setting & InvokeSetting.Performed) != 0)
                cachedAction.performed -= OnAction;

            if ((setting & InvokeSetting.Canceled) != 0)
                cachedAction.canceled -= OnAction;
        }
        private void OnAction(InputAction.CallbackContext context)
        {
            gameEvent.Invoke();
        }
    }
    [SerializeField] PlayerInput PlayerInput;
    [SerializeField] List<InputEventConfig<Vector2>> vector2EventList;
    [SerializeField] List<InputEventConfig<bool>> boolEventList;
    [SerializeField] List<InputEventConfig> voidEventList;
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
#if UNITY_EDITOR
    private void OnValidate()
    {
        
    }
#endif
    protected virtual void Awake()
    {
        //moveAction = PlayerInput.actions["Move"];
        //lookAction = PlayerInput.actions["Look"];
        //jumpAction = PlayerInput.actions["Jump"];
        //setObjectAction = PlayerInput.actions["Interact"];
        //dashAction = PlayerInput.actions["Sprint"];
        //fireAction = PlayerInput.actions["Fire"];
        //switchCameraAction = PlayerInput.actions["SwitchCamera"];
        
        //switchJobAction = PlayerInput.actions["SwitchJob"];
        //markerAction = PlayerInput.actions["Marker"];
        //uiAction = PlayerInput.actions["ShowUI"];
    }
    protected virtual void OnEnable()
    {
        foreach(var v in vector2EventList)
        {
            v.Enable();
        }
        foreach(var v in boolEventList)
        {
            v.Enable();
        }
        foreach(var v in voidEventList)
        {
            v.Enable();
        }
        //switchCameraAction.performed += SwitchCamera;
        //moveAction.performed += MoveActionCallback;
        //moveAction.canceled += MoveActionCallback; // 入力がキャンセルされたときもコールバックを呼び出す(ゼロの検出)

        //lookAction.performed += LookActionCallback;
        //lookAction.canceled += LookActionCallback;

        //jumpAction.performed += JumpActionCallback;

        //setObjectAction.performed += OnSetObjectCallback;

        //dashAction.performed += DashActionCallback;
        //dashAction.canceled += DashActionCallback;

        //fireAction.performed += FireActionCallback;

        //switchJobAction.performed += OnSwirchJobCallback;
        //markerAction.performed += OnMarkerCallback;

        //uiAction.performed += UiActionCallback;
    }

    

    protected virtual void OnDisable()
    {
        foreach(var v in vector2EventList)
        {
            v.Disable();
        }
        foreach (var v in boolEventList)
        {
            v.Disable();
        }
        foreach (var v in voidEventList)
        {
            v.Disable();
        }
        //switc
        //if (moveAction != null)
        //{
        //    moveAction.performed -= MoveActionCallback;
        //    moveAction.canceled -= MoveActionCallback;
        //}
        //if (lookAction != null)
        //{
        //    lookAction.performed -= LookActionCallback;
        //    lookAction.canceled -= LookActionCallback;
        //}
        //if (jumpAction != null)
        //{
        //    jumpAction.performed -= JumpActionCallback;
        //}
        //if (setObjectAction != null)
        //{
        //    setObjectAction.performed -= OnSetObjectCallback;
        //}

        //if (dashAction != null)
        //{
        //    dashAction.performed -= DashActionCallback;
        //    dashAction.canceled -= DashActionCallback;
        //}

        //if (fireAction != null)
        //{
        //    fireAction.performed -= FireActionCallback;
        //}
        //switchCameraAction.performed -= SwitchCamera;
        //switchJobAction.performed -= OnSwirchJobCallback;
        //markerAction.performed -= OnMarkerCallback;
        //uiAction.performed -= UiActionCallback;
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
