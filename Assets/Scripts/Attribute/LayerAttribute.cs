namespace Syacapachi.Attribute
{
    using UnityEngine;

    public class LayerAttribute : PropertyAttribute
    {
        public bool useMask;
        public LayerAttribute(bool useMask = false)
        {
            this.useMask = useMask;
        }
    }

}