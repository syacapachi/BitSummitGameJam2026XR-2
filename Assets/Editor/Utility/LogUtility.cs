namespace Syacapachi.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    public class LogUtility
    {
        public static string BuildParameterLog(MethodInfo method, object[] values)
        {
            if (values == null || values.Length == 0)
                return "Parameters: (none)";

            var parameters = method.GetParameters();

            var lines = new List<string>();
            lines.Add("Parameters:");

            for (int i = 0; i < values.Length; i++)
            {
                string name = parameters[i].Name;
                object val = values[i];

                lines.Add($"  {name} = {FormatValue(val)}");
            }

            return string.Join("\n", lines);
        }
        static string FormatValue(object value)
        {
            if (value == null)
                return "null";

            Type t = value.GetType();

            // UnityEngine.Object
            if (value is UnityEngine.Object uo)
                return $"{uo.name} ({t.Name})";

            // string
            if (value is string s)
                return $"\"{s}\"";

            // 配列
            if (t.IsArray)
            {
                var arr = (Array)value;
                return $"Array[{arr.Length}]";
            }

            // IList
            if (value is IList list)
            {
                return $"List[{list.Count}]";
            }

            // Dictionary
            if (value is IDictionary dict)
            {
                return $"Dictionary[{dict.Count}]";
            }

            // Vector系
            if (value is Vector2 v2) return v2.ToString("F2");
            if (value is Vector3 v3) return v3.ToString("F2");
            if (value is Vector4 v4) return v4.ToString("F2");

            // Color
            if (value is Color c)
                return $"RGBA({c.r:F2},{c.g:F2},{c.b:F2},{c.a:F2})";

            // Enum
            if (t.IsEnum)
                return value.ToString();

            // fallback
            return value.ToString();
        }
    }
}