namespace Syacapachi.Attribute
{
    using UnityEngine;
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public bool showOnlyPlayMode;
        public ReadOnlyAttribute(bool showOnlyPlayMode = false)
        {
            this.showOnlyPlayMode = showOnlyPlayMode;
        }
    }
}