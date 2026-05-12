using Syacapachi.Attribute;
using System;
using UnityEngine;
[Serializable]
public class JobSettingBase
{
    /// <summary>
    /// 設定対象の職業
    /// </summary>
    [SerializeField, SingleFlagOnly]
    public PlayerJob SettingJob;
    /// <summary>
    /// 攻撃が当たる職業。複数選択可能。
    /// </summary>
    [SerializeField]
    public PlayerJob AttackableJobs;
    /// <summary>
    /// コライダーのレイヤー
    /// </summary>
    [SerializeField,Layer]
    public int CollidersLayer;
    public JobSettingBase()
    {

    }
    public JobSettingBase(PlayerJob job, PlayerJob attackableJob, int colliderLayer)
    {
        SettingJob = job;
        AttackableJobs = attackableJob;
        CollidersLayer = colliderLayer;
    }

    public bool IsAttackableJob(PlayerJob targetJob)
    {
        return (AttackableJobs & targetJob) != 0;
    }
}
