using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReciever : MonoBehaviour
{
    [Flags]
    enum InvokeSetting
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
    class InputEventConfig<T> where T : struct
    {
        static readonly Type thisType = typeof(T);
        static readonly Type boolType = typeof(bool);
        [Header("Input Action")]
        [SerializeField] InputActionReference action;
        [Header("Publish Event")]
        [SerializeField] GameEventBase<T> gameEvent;
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
    class InputEventConfig
    {
        [Header("Input Action")]
        [SerializeField] InputActionReference action;
        [Header("Publish Event")]
        [SerializeField] GameEventBase gameEvent;
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
    [SerializeField] InputEventConfig<Vector2>[] vector2EventList;
    [SerializeField] InputEventConfig<bool>[] boolEventList;
    [SerializeField] InputEventConfig[] voidEventList;
    Vector2 moveInput = Vector2.zero;
    Vector2 lookInput = Vector2.zero;
    public Vector2 MoveInput => moveInput;
    public Vector2 LookInput => lookInput;
#if UNITY_EDITOR
    private void OnValidate()
    {
    }
#endif
    protected virtual void OnEnable()
    {
        foreach (var v in vector2EventList)
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
    }

    protected virtual void OnDisable()
    {
        foreach (var v in vector2EventList)
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
    }
}
