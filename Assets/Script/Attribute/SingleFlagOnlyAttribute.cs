namespace Syacapachi.Attribute
{
    using UnityEngine;
    public class SingleFlagOnlyAttribute : PropertyAttribute
    {
        public bool allowNothing { get; private set; }

        public SingleFlagOnlyAttribute(bool allowNothing = true)
        {
            this.allowNothing = allowNothing;
        }
    }
}