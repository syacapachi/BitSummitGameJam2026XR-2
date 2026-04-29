using UnityEngine;

public class GameEffectParticleManager : MonoBehaviour
{
    [Header("Subscribe Event")]
    [SerializeField] private GameEffectEvent gameEffectEvent;

    private void OnEnable()
    {
        gameEffectEvent.Register(OnEventReceived);
    }

    private void OnDisable()
    {
        gameEffectEvent.Unregister(OnEventReceived);
    }

    private void OnEventReceived(GameEffect e)
    {
        if (e.FxEffect is not FxEffect fx) return;

        GameObject fxObject = Instantiate(fx.FxPrefab, e.Position, Quaternion.identity);
        
        if (fxObject.TryGetComponent<ParticleSystem>(out var ps))
        {
            var mainModule = ps.main;
            float lifetime = mainModule.duration + mainModule.startLifetime.constantMax;
            Destroy(fxObject, lifetime);
        }
        else
        {
            Destroy(fxObject, 2f);
        }
    }
}