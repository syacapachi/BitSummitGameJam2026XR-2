using UnityEngine.InputSystem;

public class BoolReader : IInputReader<bool>
{
    public bool ReadValue(InputAction.CallbackContext context)
    {
        return context.ReadValueAsButton();
    }
}
