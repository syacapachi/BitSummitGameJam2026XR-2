using UnityEngine;

public class NBulletImpactAudio : MonoBehaviour
{
    [SerializeField] AudioEffectData hitEnemyAudioData;
    [SerializeField] AudioEffectData hitWallAudioData;
    [Header("Publish Event")]
    [SerializeField] GameEffectEvent gameEffectEvent;

    private bool alreadyPlayed = false;


    public void HandleImpact(Transform targetAll, Vector3 pointAll)
    {
        if (alreadyPlayed) return;
        alreadyPlayed = true;

        bool hitEnemy =
            targetAll.GetComponent<NEnemy>() != null ||
            targetAll.GetComponentInParent<NEnemy>() != null;

        if (hitEnemy)
        {
            gameEffectEvent.Invoke(new GameEffect(hitEnemyAudioData.ToRuntimeData(), pointAll));
        }
        else
        {
            gameEffectEvent.Invoke(new GameEffect(hitWallAudioData.ToRuntimeData(), pointAll));
        }
    }
}
