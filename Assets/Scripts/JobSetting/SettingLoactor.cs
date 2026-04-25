using UnityEngine;
/// <summary>
/// インスペクター上で見えるようにデータを置いておいてるだけ
/// </summary>
public class SettingLoactor : MonoBehaviour
{
    [Header("このクラスはインスペクター上でデータを管理するためのものです")]
    [Header("ジョブの設定")]
    [SerializeField] JobSettingGenerator[] setting;
    [Header("敵のデータベース")]
    [SerializeField] EnemyDataBase enemyDataBase;
}
