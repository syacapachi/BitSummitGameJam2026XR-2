using UnityEngine;
using Unity.Netcode;
public class HandSetting : NetworkBehaviour
{
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
            leftHand.SetActive(false);
            rightHand.SetActive(false);
            leftController.SetActive(false);
            rightController.SetActive(false);
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
    private void Update()
    {
        if (IsOwner)
        {
            networkLeftHand.transform.SetPositionAndRotation(leftHand.transform.position, leftHand.transform.rotation);
            networkRightHand.transform.SetPositionAndRotation(rightHand.transform.position, rightHand.transform.rotation);
            networkLeftController.transform.SetPositionAndRotation(leftController.transform.position, leftController.transform.rotation);
            networkRightController.transform.SetPositionAndRotation(rightController.transform.position, rightController.transform.rotation);
        }
    }
}
