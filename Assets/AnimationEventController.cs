using Unity.Netcode;
using UnityEngine;
[RequireComponent(typeof(Animator))]
public class AnimationEventController : NetworkBehaviour
{
    [SerializeField] NEnemyShoot shooter;
    [SerializeField] NEnemy enemy;
    [SerializeField] Renderer[] renderers;

    //アニメーションイベントはAnimationControllerと同じところでないと呼ばれない。
    void OnAttack()
    {
        shooter.ShootFromAnimationEvent();
    }
    //アニメーションイベントはAnimationControllerと同じところでないと呼ばれない。
    //アニメーションは全てで呼ばれる
    void OnVisible()
    {
        enemy.SetVisibleFromAnimationEvent();
    }

    void OnInvisible()
    {
        enemy.RestoreLayerFromAnimationEvent();
    }

    void OnDie()
    {
        if (!IsServer) return;
        enemy.DieFromAnimationServerEvent();
    }

    void StartMove()
    {
        if (!IsServer) return;
        enemy.MovePositionFromAnimationServerEvent();
    }

#if UNITY_EDITOR
    void Reset()
    {
        shooter = GetComponentInParent<NEnemyShoot>();
        enemy = GetComponentInParent<NEnemy>();
        renderers = GetComponentsInChildren<Renderer>();
    }
#endif
}
