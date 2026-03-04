using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;


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
    public Type BaseType { get; private set; }

    public SerializeReferenceViewAttribute(Type baseType = null)
    {
        BaseType = baseType;
    }
}


