using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyResultRow : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;

    public void Setup(Sprite sprite, string enemyName, int count)
    {
        icon.sprite = sprite;
        nameText.text = enemyName;
        countText.text = $"{count} kill";
    }
}