using UnityEngine;

public class HandLaser : MonoBehaviour
{

    [SerializeField] Transform leftHand;
    [SerializeField] Transform rightHand;

    [SerializeField] LineRenderer leftLaser;
    [SerializeField] LineRenderer rightLaser;

    public float distance = 20f;

    void Update()
    {
        UpdateLaser(leftHand, leftLaser, distance);
        UpdateLaser(rightHand, rightLaser, distance);
    }

    static void UpdateLaser(Transform hand, LineRenderer laser,float distance)
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