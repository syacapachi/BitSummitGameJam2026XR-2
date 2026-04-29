using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
[RequireComponent(typeof(Animator))]
public class AvatarSyncronize : NetworkBehaviour
{
    XROrigin xrOrigin;
    Transform leftHand, rightHand, leftController, rightController;
    Camera ownerCamera;
    [Header("Root")]
    [SerializeField] Transform avatorRootTransfrom;
    [Header("Avator Animator")]
    [SerializeField] Animator animator;
    [Header("Avator Head")]
    [SerializeField] private Transform networkHead;
    [Header("Avator Hands")]
    [SerializeField] private Transform networkLeftHand;
    [SerializeField] private Transform networkRightHand;
    [SerializeField] private Transform networkLeftController;
    [SerializeField] private Transform networkRightController;
    [Header("Setting")]
    [SerializeField] float footOffset = 0.1f;
    [SerializeField] float footWight = 0.2f;
    [SerializeField] float kneeWight = 0.2f;
    [Header("Calibration"),Tooltip("対象のアバターのスケールを慎重に応じて拡大・縮小します。(元のアバターの身長は1m,スケールは1.1.1にして下さい。)")]
    [SerializeField] int calibrationCount = 10;
    [Header("Debug")]
    [SerializeField] bool isDebugMode = false;
    public readonly NetworkVariable<int> JumpCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<float> AvatarScale = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField,ReadOnly] float avatarEyeHeight = 0;
    [SerializeField, ReadOnly] float avatarScale = 0;
    /// <summary>
    /// 更新が可能かのフラグ、オーナーがわで、ローカルプレイヤーを取得したかのために使う
    /// </summary>
    private bool isfoundLocalPlayer = false;
    private bool isCalibrationed = false;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            //初期化
            OnScaleChanged(1, 1);
            //avatorRootTransfromからみた、頭の相対座標のY成分を、アバターの目の高さとする
            avatarEyeHeight = avatorRootTransfrom.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Head).position).y;
            StartCoroutine(WaitForEnable());
            NetworkManager.SceneManager.OnLoadComplete += OnSceneLoaded;
            NetworkManager.SceneManager.OnUnload += OnSceneUnLoad;
        }
        else
        {
            //非オーナー側ではオッケー
            isfoundLocalPlayer = true;
        }
        AvatarScale.OnValueChanged += OnScaleChanged;
    }
    private void OnSceneUnLoad(ulong clientId, string sceneName, AsyncOperation asyncOperation)
    {
        isfoundLocalPlayer = false;
    }

    /// <summary>
    /// シーンをロードしたタイミングで呼ばれる。
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="name"></param>
    /// <param name="loadMode"></param>
    private void OnSceneLoaded(ulong clientId,string name,UnityEngine.SceneManagement.LoadSceneMode loadMode)
    {
        StartCoroutine(WaitForEnable());
    }
    private IEnumerator WaitForEnable()
    {
        while (true)
        {
            var Locator = ManagerLocator.Instance;
            if (   Locator != null
                && Locator.AllPlayerManager != null
                && Locator.AllPlayerManager.LocalPlayerRoot != null)
            {
                ResistLocalPlayer();
                yield return CalcHeight();
                yield break;
            }
            yield return null;
        }
    }
    private void ResistLocalPlayer()
    {
        var playerRoot = ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot;
        xrOrigin = playerRoot.XROrigin;
        leftHand = playerRoot.LeftHand;
        rightHand = playerRoot.RightHand;
        leftController = playerRoot.LeftController;
        rightController = playerRoot.RightController;
        ownerCamera = xrOrigin.Camera;
        isfoundLocalPlayer = true;
    }
    private IEnumerator CalcHeight()
    {
        if(isCalibrationed) yield break;
        isCalibrationed = true;
        float eyeHeight = 0;
        for(int i = 0;i< calibrationCount; i++)
        {
            eyeHeight += xrOrigin.Camera.transform.position.y;
            yield return null;
        }
        eyeHeight /= calibrationCount;
        AvatarScale.Value = eyeHeight / avatarEyeHeight;
        Debug.Log($"[{nameof(AvatarSyncronize)}] Scale is {AvatarScale.Value}");
    }
    public override void OnNetworkDespawn()
    {
        isfoundLocalPlayer = false;
        isCalibrationed = false;
        NetworkManager.SceneManager.OnLoadComplete -= OnSceneLoaded;
        NetworkManager.SceneManager.OnUnload -= OnSceneUnLoad;
        AvatarScale.OnValueChanged -= OnScaleChanged;
    }
    private void OnScaleChanged(float oldScale, float newScale)
    {
        avatarScale = newScale;
        avatorRootTransfrom.localScale = Vector3.one * newScale;
    }
    /// <summary>
    /// アニメーションを計算するタイミングで更新
    /// Animatorと同じGameObjectにないと呼ばれない。
    /// </summary>
    private void OnAnimatorIK()
    {   
        if (animator == null) return;
        if (IsOwner)
        {
            if (!isfoundLocalPlayer) return;
            //頭
            //Weight は、IK優先度 0.0f->IK反映なし、1.0f->IK完全反映
            animator.SetLookAtWeight(1.0f);
            animator.SetLookAtPosition(ownerCamera.transform.position + ownerCamera.transform.forward * 2);

            //左手
            SetIKPositonAndRotation(AvatarIKGoal.LeftHand, leftController);

            //右手
            SetIKPositonAndRotation(AvatarIKGoal.RightHand, rightController);

        }
        //非オーナーは、同期されているアバターの位置をAnimatorに反映させる
        //注意 AvatorにNetworkTransform付きオブジェクトを付けるとAnimationが狂う
        //NetworkTransform付きTransformをIKに使うな 同期できなくなる
        else
        {
            //頭
            animator.SetLookAtWeight(1.0f);
            animator.SetLookAtPosition(networkHead.position + networkHead.forward * 2);

            //左手
            SetIKPositonAndRotation(AvatarIKGoal.LeftHand, networkLeftController);

            //右手
            SetIKPositonAndRotation(AvatarIKGoal.RightHand, networkRightController);
        }
        //足は共通
        AdjustIKToPlane(AvatarIKGoal.LeftFoot, AvatarIKHint.LeftKnee);
        AdjustIKToPlane(AvatarIKGoal.RightFoot, AvatarIKHint.RightKnee);
    }
    private void AdjustIKToPlane(AvatarIKGoal goal,AvatarIKHint hint, float weight = 1.0f)
    {
        
        Vector3 ikPos = animator.GetIKPosition(goal);
        //プレイヤーの真下にRayを飛ばす。
        if (Physics.Raycast(ikPos, -transform.up, out RaycastHit hit))
        {
            Vector3 footPos = hit.point + hit.normal * footOffset;
            //ぶつかったオブジェクトの傾きを
            Quaternion footRot = Quaternion.LookRotation(
                //アバターの正面を床に投影(アバターの床と平行な成分をとる)
                Vector3.ProjectOnPlane(transform.forward, hit.normal),
                hit.normal
            );
            footPos += transform.right * (goal == AvatarIKGoal.LeftFoot ? -footWight : footWight);
            SetIKPositonAndRotation(goal, footPos, footRot, weight);

            //Vector3 hintPos = animator.GetIKHintPosition(hint);
            // 🔥 Knee Hint
            //ヒントは、0.3m+身長の0.4倍、左右にkneeWight、上に0.3mの位置にする
            Vector3 hintPos =
                footPos
                + transform.forward * 0.4f
                + transform.right * (goal == AvatarIKGoal.LeftFoot || goal == AvatarIKGoal.LeftHand ? -kneeWight : kneeWight)
                + Vector3.up * 0.3f;
            SetIKHintPosition(hint,hintPos);
        }
    }
    private void SetIKPositonAndRotation(AvatarIKGoal goal, Transform targetTransform, float weight = 1.0f)
    {
        SetIKPositonAndRotation(goal, targetTransform.position, targetTransform.rotation, weight);
    }
    private void SetIKPositonAndRotation(AvatarIKGoal goal, Vector3 targetPos, Quaternion rotation, float weight = 1.0f)
    {
        //Weight は、IK優先度 0.0f->IK反映なし、1.0f->IK完全反映
        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKRotationWeight(goal, weight);
        animator.SetIKPosition(goal, targetPos);
        animator.SetIKRotation(goal, rotation);
    }
    private void SetIKHintPosition(AvatarIKHint hint, Vector3 targetPos, float weight = 1.0f)
    {
        animator.SetIKHintPositionWeight(hint ,weight);
        animator.SetIKHintPosition(hint, targetPos);
    }
    /// <summary>
    /// アニメーションがある場合は、全ての適応後に更新
    /// </summary>
    private void LateUpdate()
    {
        if (IsOwner)
        {
            if (!isfoundLocalPlayer) return;
            //avatorRootTransfromからみた、左目の相対座標
            Vector3 headOffsetLocalY = avatorRootTransfrom.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftEye).position);
            //カメラのY座標は、地面からの距離
            //Avatorの頭を動かす不自然になる。->rootを調整
            Vector3 cameraPos = xrOrigin.Camera.transform.position;
            if (isDebugMode)
            {
                cameraPos += xrOrigin.Camera.transform.forward * 2f;
            }
            // rootを補正
            cameraPos.y -= headOffsetLocalY.y;
            avatorRootTransfrom.SetPositionAndRotation(cameraPos, xrOrigin.transform.rotation);

            networkHead.SetPositionAndRotation(xrOrigin.Camera.transform.position, xrOrigin.Camera.transform.rotation);

            if (XRSettings.isDeviceActive)
            {
                // ★手
                networkLeftController.SetPositionAndRotation(leftController.position, leftController.rotation);
                networkRightController.SetPositionAndRotation(rightController.position, rightController.rotation);
            }
            else
            {
                // ★手　XRが無効の時は、頭に付けることで疑似的FPS
                networkLeftController.SetPositionAndRotation(xrOrigin.Camera.transform.position, xrOrigin.Camera.transform.rotation);
                networkRightController.SetPositionAndRotation(xrOrigin.Camera.transform.position, xrOrigin.Camera.transform.rotation);
            }
        }
    }
#if UNITY_EDITOR
    private void Reset()
    {
        animator = GetComponent<Animator>();
    }
#endif
}
