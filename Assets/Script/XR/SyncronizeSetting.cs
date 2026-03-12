using UnityEngine;
using Unity.Netcode;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
public class SyncronizeSetting : NetworkBehaviour
{
    [Header("Root")]
    [SerializeField] Transform localXRRootTransfrom;
    [SerializeField] Transform avatorRootTransfrom;
    [Header("XROrigin")]
    [SerializeField] private XROrigin xrOrigin;
    [Header("Owner Camera")]
    [SerializeField] private Camera ownerCamera;
    [Header("Avator Head")]
    [SerializeField] private Transform networkHead;
    [Header("Owner Hands")]
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;
    [SerializeField] private GameObject leftController;
    [SerializeField] private GameObject rightController;
    [Header("Avator Hands")]
    [SerializeField] private GameObject networkLeftHand;
    [SerializeField] private GameObject networkRightHand;
    [SerializeField] private GameObject networkLeftController;
    [SerializeField] private GameObject networkRightController;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            leftHand.SetActive(true);
            rightHand.SetActive(true);
            leftController.SetActive(true);
            rightController.SetActive(true);
            DisableMeshRenderer(networkLeftHand);
            DisableMeshRenderer(networkRightHand);
            DisableMeshRenderer(networkLeftController);
            DisableMeshRenderer(networkRightController);
            DisableMeshRenderer(avatorRootTransfrom.gameObject);
        }
        else
        {
            xrOrigin.gameObject.SetActive(false);
            DisableComponentAndObject(leftHand);
            DisableComponentAndObject(rightHand);
            DisableComponentAndObject(leftController);
            DisableComponentAndObject(rightController);
        }
    }
    private void DisableMeshRenderer(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
    }
    private void DisableComponentAndObject(GameObject root)
    {
        MonoBehaviour[] components = root.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if(component is NetworkBehaviour)
            {
                continue; // NetworkBehaviourは無効化しない
            }
            component.enabled = false;
        }
        root.SetActive(false);
    }
    /// <summary>
    /// アニメーションがある場合は、全ての適応後に更新
    /// </summary>
    private void LateUpdate()
    {
        if (IsOwner)
        {
            //ルートの移動（重要）

            Vector3 offset = xrOrigin.Camera.transform.position - localXRRootTransfrom.position;
            offset.y = 0;//高さの影響を消す(埋まり防止)

            localXRRootTransfrom.position += offset;
            xrOrigin.transform.position -= offset;

            if(xrOrigin.transform.localRotation.eulerAngles.y != 0f)
            {
                localXRRootTransfrom.rotation = xrOrigin.transform.rotation;
                xrOrigin.transform.localRotation = Quaternion.Euler(Vector3.zero);
            }
            //頭の角度
            Vector3 headrotation = ownerCamera.transform.localRotation.eulerAngles;
            networkHead.localRotation = Quaternion.Euler(-headrotation.y, headrotation.z, -headrotation.x);

            //手・コントローラー
            networkLeftHand.transform.SetPositionAndRotation(leftHand.transform.position, leftHand.transform.rotation);
            networkRightHand.transform.SetPositionAndRotation(rightHand.transform.position, rightHand.transform.rotation);
            networkLeftController.transform.SetPositionAndRotation(leftController.transform.position, leftController.transform.rotation);
            networkRightController.transform.SetPositionAndRotation(rightController.transform.position, rightController.transform.rotation);
        }
    }
}
