using Syacapachi.Attribute;
using System;
using UnityEngine;
[Serializable]
public class EnemyJobSetting : JobSettingBase
{
    public EnemyJobSetting(PlayerJob enemyJob, PlayerJob attackableJobs, int ColliderLayer):base(enemyJob,attackableJobs, ColliderLayer)
    {
    }
    public EnemyJobSetting() { }
}