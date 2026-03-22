using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
public class SyncronizeSetting : NetworkBehaviour
{
    [Header("Root")]
    [SerializeField] Transform playerRootTransfrom;
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

    private bool canUpdate = false;
    private static WaitForSeconds wait1 = new WaitForSeconds(1f);

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
            StartCoroutine(WaitForStable());
        }
        else
        {
            // xrOrigin.gameObject.SetActive(false);
            // DisableComponentAndObject(leftHand);
            // DisableComponentAndObject(rightHand);
            // DisableComponentAndObject(leftController);
            // DisableComponentAndObject(rightController);
        }
    }
    public override void OnNetworkDespawn()
    {
        canUpdate = false;
    }
    private IEnumerator WaitForStable()
    {
        if (canUpdate) yield break;
        yield return wait1;
        canUpdate = true;
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
        //MonoBehaviour[] components = root.GetComponentsInChildren<MonoBehaviour>();
        //foreach (MonoBehaviour component in components)
        //{
        //    if(component is NetworkBehaviour)
        //    {
        //        continue; // NetworkBehaviourは無効化しない
        //    }
        //    component.enabled = false;
        //}
        root.SetActive(false);
    }
    /// <summary>
    /// アニメーションがある場合は、全ての適応後に更新
    /// </summary>
    private void LateUpdate()
    {
        if (IsOwner && canUpdate)
        {
            //ルートの移動（重要）

            Vector3 offset = xrOrigin.Camera.transform.position - playerRootTransfrom.position;
            offset.y = 0;//高さの影響を消す(埋まり防止)
            if (!IsValid(offset)) return;
            playerRootTransfrom.position += offset;
            xrOrigin.transform.position -= offset;

            if(Mathf.Abs(xrOrigin.transform.localRotation.eulerAngles.y) > 0.01f)
            {
                playerRootTransfrom.rotation = xrOrigin.transform.rotation;
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
    private bool IsValid(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsInfinity(v.x) ||
             float.IsNaN(v.y) || float.IsInfinity(v.y) ||
             float.IsNaN(v.z) || float.IsInfinity(v.z));
    }
    bool IsXRValid()
    {
        var cam = xrOrigin.Camera;
        if (cam == null) return false;

        Vector3 pos = cam.transform.position;

        return !(float.IsNaN(pos.x) || float.IsInfinity(pos.x));
    }
}
