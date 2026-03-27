using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class AnimationSuncro : NetworkBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] NetworkAnimator NetworkAnimator;
    [SerializeField] Syncronize syncronizeSetting;
    //すべてのスポーンが終わってから購読
    protected override void OnNetworkPostSpawn()
    {
        if (IsOwner)
        {
            syncronizeSetting.Reciever.OnJumped += OnJump;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            syncronizeSetting.Reciever.OnJumped -= OnJump;
        }
    }
    private void Update()
    {
        if (IsOwner)
        {
            Vector2 moveInput = syncronizeSetting.Reciever.MoveInput;
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
