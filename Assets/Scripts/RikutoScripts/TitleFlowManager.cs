using UnityEngine;
using Unity.Netcode;

public class TitleFlowManager : MonoBehaviour
{
    public static TitleFlowManager Instance;

    [Header("Canvas")]
    [SerializeField] GameObject languageCanvas;
    [SerializeField] GameObject connectCanvas;
    [SerializeField] Canvas worldViewCanvas;

    public TitleFlowState CurrentState { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        bool connected =
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsClient;

        if (connected)
        {
            SetState(TitleFlowState.WorldView);
        }
        else
        {
            SetState(TitleFlowState.LanguageSelect);
        }
    }

    public void SetState(TitleFlowState state)
    {
        CurrentState = state;

        languageCanvas.SetActive(
            state == TitleFlowState.LanguageSelect);

        connectCanvas.SetActive(
            state == TitleFlowState.NetworkConnect ||
            state == TitleFlowState.WorldView);

        worldViewCanvas.enabled =
            state == TitleFlowState.WorldView;
    }

    public void EnterLanguageSelect()
    {
        SetState(TitleFlowState.LanguageSelect);
    }

    public void EnterNetworkConnect()
    {
        SetState(TitleFlowState.NetworkConnect);
    }



    public void EnterWorldView()
    {
        SetState(TitleFlowState.WorldView);
    }


}

public enum TitleFlowState
{
    LanguageSelect,
    NetworkConnect,
    WorldView
}