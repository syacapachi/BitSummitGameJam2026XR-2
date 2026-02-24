using Unity.XR.CoreUtils;
using UnityEngine;
using Unity.Netcode;

public class PlayerPropaty : NetworkBehaviour
{
    [SerializeField] GameObject PlayerRoot;
    public enum PlayerJob { Human, Ghost, Both }
    [SerializeField] PlayerJob playerjob;
    public PlayerJob Job {
        get => playerjob; 
        set 
        {
            if (playerjob != value)
            {
                playerjob = value;
                string layerName = playerjob switch
                {
                    PlayerJob.Human => "Human",
                    PlayerJob.Ghost => "Ghost",
                    PlayerJob.Both => "Default",
                    _ => throw new System.NotImplementedException(),
                };
                PlayerRoot.layer = LayerMask.NameToLayer(layerName);
            }
        }
    }
    public bool CanSeeEnemy
    {
        get => playerjob != PlayerJob.Human;
    }
}
