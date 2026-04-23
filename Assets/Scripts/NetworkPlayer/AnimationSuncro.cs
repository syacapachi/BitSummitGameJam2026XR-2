using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class AnimationSuncro : NetworkBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] NetworkAnimator NetworkAnimator;
    [SerializeField] AvatarSyncronize syncronizeSetting;
    [Header("Subscribe Event")]
    [SerializeField] VoidEvent jumpEvent;
    [SerializeField] Vector2Event moveEvent;
    //すべてのスポーンが終わってから購読
    protected override void OnNetworkPostSpawn()
    {
        if (IsOwner)
        {
            //syncronizeSetting.Reciever.OnJumped += OnJump;
            jumpEvent.Register(OnJump);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            //syncronizeSetting.Reciever.OnJumped -= OnJump;
            jumpEvent.Unregister(OnJump);
        }
    }
    private void Update()
    {
        if (IsOwner)
        {
            Vector2 moveInput = moveEvent.CurrentValue;
            //NetworkAnimator(Owner Authority Mode)を使ってアニメーションを同期する
            //アニメーションのパラメーターを設定(たいていは同期される)
            if (animator != null)
            {
                animator?.SetFloat("Speed", moveInput.magnitude);
                animator?.SetFloat("Direction", moveInput.x);
            }
        }
    }
    private void OnJump()
    {
        //セットトリガーは通常のAnimatorでは反映されないので、NetworkAnimatorを使用する
        NetworkAnimator?.SetTrigger("Jump");
    }
}
