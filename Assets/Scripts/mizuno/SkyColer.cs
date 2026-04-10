using Syacapachi.Attribute;
using UnityEngine;

public class SkyColer : MonoBehaviour
{
    [SerializeField]Camera m_camera;
    [SerializeField]Color defaultcolor;
    [SerializeField]Color backgroundcolor;

    [SerializeField] NGameManager nGameManager;
    private void OnEnable()
    {
        nGameManager.OnGameResetRpcEvent += ResetColor;
        nGameManager.OnGameClearRpcEvent += CamereColer;
    }
    public void OnGameClear()
    {
        CamereColer();
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
