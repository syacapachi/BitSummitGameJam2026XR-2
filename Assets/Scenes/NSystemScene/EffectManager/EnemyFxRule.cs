using UnityEngine;

public class EnemyFxRule : MonoBehaviour
{
    [SerializeField] private PlayerJob visibleToJobsAll = PlayerJob.Both;

    public PlayerJob VisibleToJobsAll => visibleToJobsAll;

    public bool IsEffectiveFor(PlayerJob shooterJob)
    {
        return (shooterJob & visibleToJobsAll) != 0;
    }

    public bool IsVisibleTo(PlayerJob viewerJob)
    {
        return (viewerJob & visibleToJobsAll) != 0;
    }
}