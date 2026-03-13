using UnityEngine;
using Unity.Netcode;

public class HandLaser : NetworkBehaviour
{
    public Transform leftHand;
    public Transform rightHand;

    public LineRenderer leftLaser;
    public LineRenderer rightLaser;

    public float distance = 20f;

    void Update()
    {
        if (!IsOwner) return;

        UpdateLaser(leftHand, leftLaser);
        UpdateLaser(rightHand, rightLaser);
    }

    void UpdateLaser(Transform hand, LineRenderer laser)
    {
        if (hand == null || laser == null) return;

        Ray ray = new Ray(hand.position, hand.forward);

        laser.SetPosition(0, ray.origin);

        if (Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            laser.SetPosition(1, hit.point);
        }
        else
        {
            laser.SetPosition(1, ray.origin + ray.direction * distance);
        }
    }
}