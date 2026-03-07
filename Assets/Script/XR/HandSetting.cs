using UnityEngine;
using Unity.Netcode;
using NUnit.Framework;
using System.Collections.Generic;
public class HandSetting : NetworkBehaviour
{
    [Header("Owner Camera")]
    [SerializeField] private Camera ownerCamera;
    [Header("Network Head")]
    [SerializeField] private GameObject avatorRoot;
    [SerializeField] private GameObject networkHead;
    [Header("Owner Hand")]
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;
    [SerializeField] private GameObject leftController;
    [SerializeField] private GameObject rightController;
    [Header("Network Hand")]
    [SerializeField] private GameObject networkLeftHand;
    [SerializeField] private GameObject networkRightHand;
    [SerializeField] private GameObject networkLeftController;
    [SerializeField] private GameObject networkRightController;

    [Header("Tracking Object")]
    [SerializeField] List<GameObject> trackingList;
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
        }
        else
        {
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
    private void LateUpdate()
    {
        if (IsOwner)
        {
            avatorRoot.transform.position = ownerCamera.transform.position;
            Vector3 headrotation = ownerCamera.transform.localRotation.eulerAngles;
            networkHead.transform.localRotation = Quaternion.Euler(-headrotation.y, headrotation.z, -headrotation.x);
            networkLeftHand.transform.SetPositionAndRotation(leftHand.transform.position, leftHand.transform.rotation);
            networkRightHand.transform.SetPositionAndRotation(rightHand.transform.position, rightHand.transform.rotation);
            networkLeftController.transform.SetPositionAndRotation(leftController.transform.position, leftController.transform.rotation);
            networkRightController.transform.SetPositionAndRotation(rightController.transform.position, rightController.transform.rotation);
        }
    }
}
