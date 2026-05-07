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
        if (!IsServer) return;
        shooter.ShootFromAnimationEvent();
    }
    //アニメーションイベントはAnimationControllerと同じところでないと呼ばれない。
    //アニメーションは全てで呼ばれる
    void OnVisible()
    {
        Debug.Log("OnVisible");
        //if (!IsServer) return;
        enemy.SetVisibleServer();
    }

    void OnInvisible()
    {
        //if (!IsServer) return;
        enemy.RestoreLayerServer();
    }

    void OnDie()
    {
        if (!IsServer) return;
        enemy.Die();
    }

    void StartMove()
    {
        if (!IsServer) return;
        enemy.MovePosition();
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
