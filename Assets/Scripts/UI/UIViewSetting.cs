using UnityEngine;

public class UIViewSetting : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Canvas canvas;
    [SerializeField] Camera playerCamera;
    [Header("PositonSetting")]
    [SerializeField] Vector3 offset;
    [Header("Subsctibe Event")]
    [SerializeField] VoidEventSO uiEvent;
    private bool isShowing = false;
    private void OnEnable()
    {
        uiEvent.Register(UIEventCallback);
    }
    private void OnDisable()
    {
        uiEvent.Unregister(UIEventCallback);
    }
    private void UIEventCallback()
    {
        isShowing = !isShowing;
        canvas.gameObject.SetActive(isShowing);
    }
}