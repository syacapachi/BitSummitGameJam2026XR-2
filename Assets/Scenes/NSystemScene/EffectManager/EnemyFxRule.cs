using UnityEngine;

public class EnemyFxRule : MonoBehaviour
{
    [SerializeField] private PlayerPropaty.PlayerJob visibleToJobsAll = PlayerPropaty.PlayerJob.Both;

    public PlayerPropaty.PlayerJob VisibleToJobsAll => visibleToJobsAll;

    public bool IsEffectiveFor(PlayerPropaty.PlayerJob shooterJob)
    {
        return (shooterJob & visibleToJobsAll) != 0;
    }

    public bool IsVisibleTo(PlayerPropaty.PlayerJob viewerJob)
    {
        return (viewerJob & visibleToJobsAll) != 0;
    }
}