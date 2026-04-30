using UnityEngine;

[CreateAssetMenu(fileName = "GameEffectData", menuName = "Game/Effect/GameEffectAsset")]
public class GameEffectAsset : ScriptableObject
{
    [SerializeField] AudioEffectData audioEffect;
    [SerializeField] FxEffectData fxEffect;

}
