namespace Syacapachi.Attribute
{
    using System;
    using UnityEngine;
    /// <summary>
    /// 任意の [SerializeReference] フィールドで使用できる「型選択＋表示」属性。
    /// < param name="basetype"> AAbasetype </param>
    /// 例：
    /// [SerializeReference, SerializeReferenceView]
    /// private BaseClass data;</param>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SerializeReferenceViewAttribute : PropertyAttribute
    {
        // 特定の基底クラスに限定したい場合などのために
        public readonly Type BaseType;

        public SerializeReferenceViewAttribute(Type baseType = null)
        {
            BaseType = baseType;
        }
    }


}