using Syacapachi.Attribute;
using UnityEngine;

public class EnemyJobSettingDataBase : ScriptableObject
{
    public struct EnemyJobSetting : IJobSetting
    {
        /// <summary>
        /// 設定対象の職業
        /// </summary>
        [SerializeField, SingleFlagOnly]
        PlayerJob TargetJob;
        /// <summary>
        /// 攻撃が当たる職業。複数選択可能。
        /// </summary>
        [SerializeField]
        PlayerJob AttackableJob;
        /// <summary>
        /// コライダーのレイヤー
        /// </summary>
        [SerializeField, Layer]
        int ColliderLayer;
        public readonly PlayerJob SettingJob => TargetJob;

        public readonly PlayerJob AttackableJobs => AttackableJob;

        public readonly int CollidersLayer => ColliderLayer;
        public readonly bool IsAttackableJob(PlayerJob targetJob)
        {
            return (AttackableJob & targetJob) != 0;
        }
        public override readonly string ToString()
        {
            return $"Job: {TargetJob}, AttackableJob: {AttackableJob}, CollidersLayer: {ColliderLayer}";
        }
    }
}
