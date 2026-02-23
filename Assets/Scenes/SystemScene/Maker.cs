using UnityEngine;

public class Marker : MonoBehaviour
{
    public float lifeTime = 5f; // マーカーが消えるまでの時間
    public Color color = Color.red;

    void Start()
    {
        // マーカーの色を変更（Renderer があれば）
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = color;
        }

        // lifeTime 秒後に自動で消す
        Destroy(gameObject, lifeTime);
    }
}
