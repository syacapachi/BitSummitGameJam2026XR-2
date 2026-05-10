using UnityEngine;

public interface IJobSetting
{
    /// <summary>
    /// 設定対象の職業
    /// </summary>
    public PlayerJob SettingJob { get; }
    /// <summary>
    /// 攻撃が当たる職業。複数選択可能。
    /// </summary>
    public PlayerJob AttackableJobs { get; }
    /// <summary>
    /// コライダーのレイヤー
    /// </summary>
    public int CollidersLayer { get; }

    public bool IsAttackableJob(PlayerJob targetJob)
    {
        return (AttackableJobs & targetJob) != 0;
    }
}
