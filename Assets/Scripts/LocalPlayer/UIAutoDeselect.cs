using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class UIAutoDeselect : MonoBehaviour
{
    [SerializeField] NearFarInteractor leftInteractor;

    [SerializeField] NearFarInteractor rightInteractor;

    int hoverCount;

    void OnEnable()
    {
        Register(leftInteractor);
        Register(rightInteractor);
    }

    void OnDisable()
    {
        Unregister(leftInteractor);
        Unregister(rightInteractor);
    }

    void Register(NearFarInteractor interactor)
    {
        if (interactor == null)
            return;

        interactor.uiHoverEntered.AddListener(OnUIHoverEntered);
        interactor.uiHoverExited.AddListener(OnUIHoverExited);
    }

    void Unregister(NearFarInteractor interactor)
    {
        if (interactor == null)
            return;

        interactor.uiHoverEntered.RemoveListener(OnUIHoverEntered);
        interactor.uiHoverExited.RemoveListener(OnUIHoverExited);
    }

    void OnUIHoverEntered(UIHoverEventArgs args)
    {
        hoverCount++;
    }

    void OnUIHoverExited(UIHoverEventArgs args)
    {
        hoverCount--;

        if (hoverCount <= 0)
        {
            hoverCount = 0;

            ClearSelection();
        }
    }

    void ClearSelection()
    {
        if (EventSystem.current == null)
            return;

        var selected = EventSystem.current.currentSelectedGameObject;

        if (selected != null &&
            selected.TryGetComponent<TMP_InputField>(out var input))
        {
            input.DeactivateInputField();
        }

        EventSystem.current.SetSelectedGameObject(null);
    }
}