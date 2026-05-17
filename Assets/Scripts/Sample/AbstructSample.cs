using Syacapachi.Attribute;
using UnityEngine;

public abstract class AbstructSample : MonoBehaviour
{
    [OnInspectorButton]
    public abstract void AbstructMethod();
}
