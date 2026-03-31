using UnityEngine;
using Unity.Netcode;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
public class Syncronize : NetworkBehaviour
{
    XROrigin xrOrigin;
    Transform playerRootTransfrom, leftHand, rightHand, leftController, rightController;
    Camera ownerCamera;
    [Header("Root")]
    [SerializeField] Transform avatorRootTransfrom;
    [Header("Avator Head")]
    [SerializeField] private Transform networkHead;
    [Header("Avator Hands")]
    [SerializeField] private Transform networkLeftHand;
    [SerializeField] private Transform networkRightHand;
    [SerializeField] private Transform networkLeftController;
    [SerializeField] private Transform networkRightController;
    public readonly NetworkVariable<int> JumpCount = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
    private LocalPlayerRoot playerRoot;
    private float lastOrigonRotation = 0;
    public InputReciever Reciever => playerRoot.InputReciver;
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

            Reciever.OnJumped += () => JumpCount.Value += 1;
        }
    }
    /// <summary>
    /// アニメーションがある場合は、全ての適応後に更新
    /// </summary>
    private void LateUpdate()
    {
        if (IsOwner)
        {
            Vector3 playerPos = xrOrigin.Camera.transform.position;
            //ルートの移動（重要）
            float offset = xrOrigin.transform.position.y - playerPos.y;

            playerPos.y += offset;

            avatorRootTransfrom.position = playerPos;

            if(xrOrigin.transform.localRotation.eulerAngles.y - lastOrigonRotation >  0.1f)
            {
                avatorRootTransfrom.rotation = xrOrigin.transform.rotation;
            }
            //頭の角度
            Vector3 headrotation = ownerCamera.transform.localRotation.eulerAngles;
            networkHead.localRotation = Quaternion.Euler(-headrotation.y, headrotation.z, -headrotation.x);

            //手・コントローラー
            networkLeftHand.SetPositionAndRotation(leftHand.position, leftHand.rotation);
            networkRightHand.SetPositionAndRotation(rightHand.position, rightHand.rotation);
            networkLeftController.SetPositionAndRotation(leftController.position, leftController.rotation);
            networkRightController.SetPositionAndRotation(rightController.position, rightController.rotation);
        }
    }
}
