using NUnit.Framework.Constraints;
using Oculus.Interaction;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.XR.CoreUtils;
using UnityEngine;
public class Syncronize : NetworkBehaviour
{
    XROrigin xrOrigin;
    Transform playerRootTransfrom, leftHand, rightHand, leftController, rightController;
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
    public readonly NetworkVariable<int> JumpCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private LocalPlayerRoot playerRoot;
    private bool isInitialized = false;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            StartCoroutine(WaitForEnable());
            NetworkManager.SceneManager.OnLoadComplete += OnSceneLoaded;
            NetworkManager.SceneManager.OnUnload += OnSceneUnLoad;
        }
    }

    private void OnSceneUnLoad(ulong clientId, string sceneName, AsyncOperation asyncOperation)
    {
        isInitialized = false;
    }

    private void OnSceneLoaded(ulong clientId,string name,UnityEngine.SceneManagement.LoadSceneMode loadMode)
    {
        StartCoroutine(WaitForEnable());
    }
    private IEnumerator WaitForEnable()
    {
        while (true)
        {
            if (   ManagerLocator.Instance != null
                && ManagerLocator.Instance.AllPlayerManager != null
                && ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot != null)
            {
                ResistLocalPlayer();
                yield break;
            }
            yield return null;
        }
    }
    private void ResistLocalPlayer()
    {
        playerRoot = ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot;
        xrOrigin = playerRoot.XROrigin;
        playerRootTransfrom = playerRoot.transform;
        leftHand = playerRoot.LeftHand;
        rightHand = playerRoot.RightHand;
        leftController = playerRoot.LeftController;
        rightController = playerRoot.RightController;
        ownerCamera = xrOrigin.Camera;

        Debug.Log($"xrOrigin {xrOrigin != null}");
        Debug.Log($"Camera {xrOrigin.Camera != null}");
        isInitialized = true;
    }
    public override void OnNetworkDespawn()
    {
        isInitialized = false;
        NetworkManager.SceneManager.OnLoadComplete -= OnSceneLoaded;
        Debug.Log("Avator Despawn");
    }
    /// <summary>
    /// アニメーションを計算するタイミングで更新
    /// Animatorと同じGameObjectにないと呼ばれない。
    /// </summary>
    private void OnAnimatorIK()
    {
        if (!isInitialized) return;
        if (animator == null) return;
        if (IsOwner)
        {
            //頭
            //Weight は、IK優先度 0.0f->IK反映なし、1.0f->IK完全反映
            animator.SetLookAtWeight(1.0f);
            animator.SetLookAtPosition(ownerCamera.transform.position + ownerCamera.transform.forward * 2);
            //左手
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);// 重みを設定
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftController.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftController.rotation);
            //右手
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1.0f);//重みを設定
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1.0f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightController.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightController.rotation);

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
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);// 重みを設定
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, networkLeftController.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, networkLeftController.rotation);
            //右手
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1.0f);//重みを設定
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1.0f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, networkRightController.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, networkRightController.rotation);
        }
        //足は共通
        Vector3 leftFootPos = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        Vector3 rightFootPos = animator.GetIKPosition(AvatarIKGoal.RightFoot);
        

        if (Physics.Raycast(leftFootPos, Vector3.down, out RaycastHit leftHit))
        {
            Quaternion leftRotation = Quaternion.FromToRotation(leftFootPos, leftHit.normal);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1.0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1.0f);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftHit.point);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftRotation);
        }
        if (Physics.Raycast(rightFootPos, Vector3.down, out RaycastHit rightHit))
        {
            Quaternion rightRotation = Quaternion.FromToRotation(rightFootPos, rightHit.normal);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1.0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1.0f);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, rightHit.point);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, rightRotation);

        }
    }
    /// <summary>
    /// アニメーションがある場合は、全ての適応後に更新
    /// </summary>
    private void LateUpdate()
    {
        if (!isInitialized) return;
        if (IsOwner)
        {
            //avatorRootTransfromからみた、networkHeadの相対座標
            Vector3 headOffsetLocal = avatorRootTransfrom.InverseTransformPoint(networkHead.position);
            //カメラのY座標は、地面からの距離
            //Avatorの頭を動かす不自然になる。->rootを調整
            Vector3 cameraPos = xrOrigin.Camera.transform.position + xrOrigin.Camera.transform.forward * 2f;
            // rootを補正
            avatorRootTransfrom.SetPositionAndRotation(cameraPos - headOffsetLocal, xrOrigin.transform.rotation);

            // ★手
            networkLeftController.SetPositionAndRotation(leftController.position, leftController.rotation);
            networkRightController.SetPositionAndRotation(rightController.position, rightController.rotation);
        }
    }

}
