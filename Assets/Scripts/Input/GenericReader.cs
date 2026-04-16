using UnityEngine.InputSystem;

public class GenericReader<T> : IInputReader<T> where T : struct
{
    public T ReadValue(InputAction.CallbackContext context)
    {
        return context.ReadValue<T>();
    }
}
