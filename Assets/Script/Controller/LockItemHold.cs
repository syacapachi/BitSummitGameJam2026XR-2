using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
[RequireComponent(typeof(XRGrabInteractable))]
public class LockItemHold : MonoBehaviour
{
    XRGrabInteractable grab;
    [SerializeField] bool canRelease = false;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }
    private void OnEnable()
    {
        grab.focusExited.AddListener(OnRelesase);
    }
    private void OnDisable()
    {
        grab.focusExited.RemoveListener(OnRelesase);
    }
    private void OnRelesase(FocusExitEventArgs args)
    {
        if (!canRelease)
        {
            grab.interactionManager.SelectEnter(
                (UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)args.interactorObject,
                grab
            );
        }
    }
    public void ForceRelease()
    {
        canRelease = true;
    }
}
