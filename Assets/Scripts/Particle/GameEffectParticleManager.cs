using UnityEngine;

public class GameEffectParticleManager : MonoBehaviour
{
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
        var ps = fx.GetComponent<ParticleSystem>();

        if (ps != null)
        {
            var main = ps.main;
            float lifetime = main.duration + main.startLifetime.constantMax;
            Destroy(fx, lifetime);
        }
        else
        {
            Destroy(fx, 2f);
        }
    }
}