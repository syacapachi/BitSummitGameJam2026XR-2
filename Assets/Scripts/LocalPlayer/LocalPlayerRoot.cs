using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
/// <summary>
/// プレイヤークラスのルートコンポーネント。プレイヤーに関連するすべてのコンポーネントを管理するためのクラス。プレイヤーの入力、キャラクターコントロール、ヘルス、プロパティ、カメラ設定などを統括する役割を持つ。
/// </summary>
public class LocalPlayerRoot : MonoBehaviour
{
    /// <summary>
    /// オブジェクトの参照
    /// </summary>
    [Header("同期用")]
    [SerializeField] Transform playerRoot;
    [SerializeField] Transform leftHand;
    [SerializeField] Transform rightHand;
    [SerializeField] Transform leftController;
    [SerializeField] Transform rightController;
    [SerializeField] XROrigin xrOrigin;
    [Header("それ以外")]
    [SerializeField] LocalCharactorControll characterControll;
    [SerializeField] LocalCameraSetting cameraSetting;
    private PlayerManager playerManager;
    public Transform PlayerRoot => playerRoot;
    public Transform LeftHand => leftHand;
    public Transform RightHand => rightHand;
    public Transform LeftController => leftController;
    public Transform RightController => rightController;
    public XROrigin XROrigin => xrOrigin;

    //Fpsモード
    private void Start()
    {
        if(XRSettings.isDeviceActive)
        {
            cameraSetting.enabled = false;
            characterControll.enabled = false;
        }
    }
}
