using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
/// <summary>
/// プレイヤークラスのルートコンポーネント。プレイヤーに関連するすべてのコンポーネントを管理するためのクラス。プレイヤーの入力、キャラクターコントロール、ヘルス、プロパティ、カメラ設定などを統括する役割を持つ。
/// </summary>
public class LocalPlayerRoot : MonoBehaviour
{
    /// <summary>
    /// オブジェクトの参照
    /// </summary>
    [SerializeField] Transform playerRoot;
    [SerializeField] Transform leftHand;
    [SerializeField] Transform rightHand;
    [SerializeField] Transform leftController;
    [SerializeField] Transform rightController;
    [SerializeField] XROrigin xrOrigin;
    [SerializeField] Canvas playerCanvas;
    [SerializeField] InputReciever InputReciever;
    [SerializeField] LocalCharactorControll characterControll;
    [SerializeField] LocalCameraSetting cameraSetting;
    private PlayerManager playerManager;
    public Transform PlayerRoot => playerRoot;
    public Transform LeftHand => leftHand;
    public Transform RightHand => rightHand;
    public Transform LeftController => leftController;
    public Transform RightController => rightController;
    public XROrigin XROrigin => xrOrigin;
    public Canvas PlayerCanvas => playerCanvas;
    public InputReciever InputReciver => InputReciever;
    public LocalCameraSetting CameraSetting => cameraSetting;
    public LocalCharactorControll CharacterControll => characterControll;

    private void Start()
    {
        if(XRSettings.isDeviceActive)
        {
            cameraSetting.enabled = false;
            characterControll.enabled = false;
        }
    }

}
