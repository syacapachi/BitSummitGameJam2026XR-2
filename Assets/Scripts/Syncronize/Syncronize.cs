using Oculus.Interaction;
using Unity.Netcode;
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
    public readonly NetworkVariable<int> JumpCount = new(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
    private LocalPlayerRoot playerRoot;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerRoot = ManagerLocator.Instance.AllPlayerManager.LocalPlayerRoot;
            xrOrigin = playerRoot.XROrigin;
            playerRootTransfrom = playerRoot.transform;
            leftHand = playerRoot.LeftHand;
            rightHand = playerRoot.RightHand;
            leftController = playerRoot.LeftController;
            rightController = playerRoot.RightController;
            ownerCamera = xrOrigin.Camera;
        }
    }
    /// <summary>
    /// アニメーションを計算するタイミングで更新
    /// Animatorと同じGameObjectにないと呼ばれない。
    /// </summary>
    private void OnAnimatorIK()
    {
        if (IsOwner)
        {
            //現状は、rootが(0.0.0)なのでpositonと同じだが一応
            Vector3 headOffsetLocal = avatorRootTransfrom.InverseTransformPoint(networkHead.position);
            //カメラのY座標は、地面からの距離
            //Avatorの頭を動かす不自然になる。->rootを調整
            Vector3 cameraPos = xrOrigin.Camera.transform.position;
            // rootを補正
            avatorRootTransfrom.SetPositionAndRotation(cameraPos - headOffsetLocal, xrOrigin.transform.rotation);

            // ★手
            networkLeftController.SetPositionAndRotation(leftController.position, leftController.rotation);
            networkRightController.SetPositionAndRotation(rightController.position, rightController.rotation);
        }
        if (animator == null) return;
        if (IsOwner)
        {
            //頭の角度
            Vector3 headrotation = ownerCamera.transform.localRotation.eulerAngles;
            Quaternion headQuaternion = Quaternion.Euler(-headrotation.y, headrotation.z, -headrotation.x);

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
    }
    /// <summary>
    /// アニメーションがある場合は、全ての適応後に更新
    /// </summary>
    //private void LateUpdate()
    //{
    //    if (IsOwner)
    //    {
    //        Vector3 playerPos = xrOrigin.Camera.transform.position;
    //        //ルートの移動（重要）
    //        float offset = xrOrigin.transform.position.y - playerPos.y;

    //        playerPos.y += offset;

    //        avatorRootTransfrom.position = playerPos;

    //        if (xrOrigin.transform.localRotation.eulerAngles.y - lastOrigonRotation > 0.1f)
    //        {
    //            avatorRootTransfrom.rotation = xrOrigin.transform.rotation;
    //            lastOrigonRotation = avatorRootTransfrom.rotation.eulerAngles.y;
    //        }
    //        //頭の角度
    //        Vector3 headrotation = ownerCamera.transform.localRotation.eulerAngles;
    //        networkHead.localRotation = Quaternion.Euler(-headrotation.y, headrotation.z, -headrotation.x);

    //        //手・コントローラー
    //        networkLeftHand.SetPositionAndRotation(leftHand.position, leftHand.rotation);
    //        networkRightHand.SetPositionAndRotation(rightHand.position, rightHand.rotation);
    //        networkLeftController.SetPositionAndRotation(leftController.position, leftController.rotation);
    //        networkRightController.SetPositionAndRotation(rightController.position, rightController.rotation);
    //    }
    //}
}
