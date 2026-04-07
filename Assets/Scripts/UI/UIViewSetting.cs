using UnityEngine;

public class UIViewSetting : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Canvas canvas;
    [SerializeField] Camera playerCamera;
    [Header("PositonSetting")]
    [SerializeField] float distance = 2f;
    [SerializeField] Vector3 panelOffset;
    [Header("Subsctibe Event")]
    [SerializeField] VoidEventSO uiEvent;
    private bool isShowing = false;
    private void OnEnable()
    {
        uiEvent.Register(UIEventCallback);
        canvas.gameObject.SetActive(isShowing);
    }
    private void OnDisable()
    {
        uiEvent.Unregister(UIEventCallback);
    }
    private void UIEventCallback()
    {
        isShowing = !isShowing;
        if (isShowing)
        {
            //プレイヤーの前方 
            Vector3 foward = playerCamera.transform.forward;
            //プレイヤーの前方 + プレイヤーの向きベクトル*距離 + 高さ
            Vector3 targetPos = playerCamera.transform.position + foward * distance + panelOffset;
            //パネルの場所-プレイヤーの場所で向きを作る(関数で向きに変換)
            Vector3 lookDir = targetPos - playerCamera.transform.position;
            lookDir.y = 0;

            canvas.transform.SetPositionAndRotation(targetPos, Quaternion.LookRotation(lookDir));
        }
        canvas.gameObject.SetActive(isShowing);
    }
}