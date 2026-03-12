using UnityEngine;
using UnityEngine.InputSystem;
public class Controllertest : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;
    InputAction FireAction;
    InputAction markerAction;

    void Awake()
    {
        playerInput.ActivateInput();
        FireAction = playerInput.actions["Fire"];
        markerAction = playerInput.actions["Marker"];
    }
    void OnEnable()
    {
        playerInput.ActivateInput();
        FireAction.performed += ctx => Fire();
        markerAction.performed += ctx => Marker();
    }
    void OnDisable()
    {
        FireAction.performed -= ctx => Fire();
        markerAction.performed -= ctx => Marker();
        playerInput.DeactivateInput();
    }   
    void Fire()
    {
        Debug.Log("Fire!");
    }
    void Marker()
    {
        Debug.Log("Marker!");
    }
}
