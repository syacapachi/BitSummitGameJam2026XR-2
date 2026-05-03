using Unity.Netcode;
using UnityEngine;
[RequireComponent(typeof(Animator))]
public class AnimationEventController : NetworkBehaviour
{
    [SerializeField] NEnemyShoot shooter;
    [SerializeField] Renderer[] renderers;

    //アニメーションイベントはAnimationControllerと同じところでないと呼ばれない。
    void OnAttack()
    {
        if (!IsServer) return;
        shooter.ShootFromAnimationEvent();
    }
    //アニメーションイベントはAnimationControllerと同じところでないと呼ばれない。
    //アニメーションは全てで呼ばれる
    void OnDeath()
    {
        //レイヤーをデフォルトに設定
        foreach(var render in renderers)
        {
            render.gameObject.layer = 0;
        }
    }
#if UNITY_EDITOR
    void Reset()
    {
        shooter = GetComponentInParent<NEnemyShoot>();
        renderers = GetComponentsInChildren<Renderer>();
    }
#endif
}
