using Syacapachi.Attribute;
using UnityEngine;

public class OverLayCanvasChanger : MonoBehaviour
{
    [SerializeField] Canvas overLayCanvas;
    [Header("Subscribe Event")]
    [SerializeField] VoidEvent changeEvent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        overLayCanvas.enabled = false;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnEnable()
    {
        changeEvent.Register(ChangeCursorMode);
    }
    private void OnDisable()
    {
        changeEvent.Unregister(ChangeCursorMode);
    }
    [OnInspectorButton]
    private void ChangeCursorMode()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            overLayCanvas.enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            overLayCanvas.enabled = true;
        }
    }
}
