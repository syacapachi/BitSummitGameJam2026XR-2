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
        if (e.FxPrefab == null) return;

        GameObject fx = Instantiate(e.FxPrefab, e.Position, Quaternion.identity);
        
        if (fx.TryGetComponent<ParticleSystem>(out var ps))
        {
            var mainModule = ps.main;
            float lifetime = mainModule.duration + mainModule.startLifetime.constantMax;
            Destroy(fx, lifetime);
        }
        else
        {
            Destroy(fx, 2f);
        }
    }
}