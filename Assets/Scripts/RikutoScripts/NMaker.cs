using UnityEngine;

public class NMarker : MonoBehaviour
{
    public float lifeTime = 5f; // �}�[�J�[��������܂ł̎���
    public Color color = Color.red;

    void Start()
    {
        // �}�[�J�[�̐F��ύX�iRenderer ������΁j
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = color;
        }

        // lifeTime �b��Ɏ����ŏ���
        Destroy(gameObject, lifeTime);
    }
}
