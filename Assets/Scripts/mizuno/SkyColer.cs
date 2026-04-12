using Syacapachi.Attribute;
using UnityEngine;

public class SkyColer : MonoBehaviour
{
    [SerializeField]Camera m_camera;
    [SerializeField]Color defaultcolor;
    [SerializeField]Color backgroundcolor;
    [Header("Subscribe Event")]
    [SerializeField] GameStateEvent gameStateEvent;
    private void OnEnable()
    {
        gameStateEvent.Register(OnGameStateChange);
    }
    private void OnDisable()
    {
        gameStateEvent.Unregister(OnGameStateChange);
    }
    private void OnGameStateChange(GameState state)
    {
        switch(state)
        {
            case GameState.GameClear:
                CamereColer();break;
            case GameState.Initializing:
                ResetColor(); break;
        }
    }
    [OnInspectorButton]
    private void CamereColer()
    {
        m_camera.backgroundColor = backgroundcolor;
    }
    [OnInspectorButton]
    private void ResetColor()
    {
        m_camera.backgroundColor = defaultcolor;
    }
}
