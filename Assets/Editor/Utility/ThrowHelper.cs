namespace Syacapachi.util
{
    using UnityEngine;

    public static class ThrowHelper
    {
        public static void Throw(string message)
        {
            throw new System.Exception(message);
        }
    }
}