using UnityEngine;
using System.Collections.Generic;
using Syacapachi.Attribute;
using System;
using UnityEditor;
using UnityEngine.Events;
public class SampleScript : MonoBehaviour
{
    [ShowInspector, SerializeField] int a;
    [ShowInspector, SerializeField] float b;
    [ShowInspector, SerializeField] Vector3 vec;
    [ShowInspector, SerializeField] Color color;
    [ShowInspector, SerializeField] GameObject obj;
    [SerializeField] InlineClass clazz;
    [ShowInspector, SerializeField] List<float> list = new List<float>();
    [ShowInspector, SerializeField] List<InlineClass> classList = new List<InlineClass>();
    [ShowInspector, SerializeField] Dictionary<int,string> adic = new Dictionary<int,string>();

    [SerializeReference,SerializeReferenceView]
    IInLineInterface resultCollector;
    public interface IInLineInterface
    {
        public void InlineMethod();
    }
    public interface IInLineGenericInterface<T>
    {
        public void InlineMethod(T value);
    }
    [Serializable]
    public class InlineClass : IInLineInterface
    {
        public string name;
        public void InlineMethod()
        {
            Debug.Log($"This is an inline method. name = {name}");
        }
    }
    public class InLineClass2 : IInLineInterface
    {
        public int number;
        public void InlineMethod()
        {
            Debug.Log($"This is an inline method. number = {number}");
        }
    }
    public class InLineClass3 : IInLineGenericInterface<string>
    {
        public string name;
        public void InlineMethod(string value)
        {
            Debug.Log($"This is an inline method. name = {name}, value = {value}");
        }
    }
    public class GeneticClass<T>
    {
        public T value;
    }

    [Flags]
    public enum SampleEnum
    {
        Value1,
        Value2,
        Value3
    }
    [OnInspectorButton]
    public void SampleMethod()
    {
        Debug.Log("This is a sample method.");
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(string message, int number)
    {
        Debug.Log("Message: " + message + ", Number: " + number);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(SampleEnum message)
    {
        Debug.Log("Message: " + message);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(List<float> value, List<ScriptableObject> valueObject,List<InlineClass> inlineClasses,List<IInLineInterface> inlineInterfaces, List<IInLineGenericInterface<int>> inlineGenericInterfaces)
    {
        string valueString = string.Join(", ", value);
        string valueObjectString = string.Join(", ", valueObject);
        string inlineClassString = string.Join(", ", inlineClasses);
        string inlineInterfaceString = string.Join(", ", inlineInterfaces);
        string inlineGenericInterfaceString = string.Join(", ", inlineGenericInterfaces);
        Debug.Log("Value: " + valueString + ", " + valueObjectString + ", " + inlineClassString + ", " + inlineInterfaceString + ", " + inlineGenericInterfaceString);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(Dictionary<InlineClass, IInLineInterface> dic,Dictionary<IInLineGenericInterface<string>,IInLineGenericInterface<int>> dic2)
    {
        string dicString = string.Join(", ", dic);
        string dic2String = string.Join(", ", dic2);
        Debug.Log("Message: " + dicString + ", " + dic2String);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(UnityEvent invokeEvent)
    {
        invokeEvent.Invoke();
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(LayerMask mask,Quaternion quatanion, DateTime time)
    {
        Debug.Log("Value: " + mask + ", " + quatanion + ", " + time);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter<F>(GeneticClass<F> invokeEvent)
    {
        Debug.Log("Value: " + invokeEvent.value);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(GeneticClass<int> invokeEvent)
    {
        Debug.Log("Value: " + invokeEvent.value);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(IInLineInterface resisterable,IInLineGenericInterface<string> invokeEvent)
    {
        resisterable.InlineMethod();
        invokeEvent.InlineMethod("Sample Value");
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(IInLineGenericInterface<int> invokeEvent)
    {
        invokeEvent.InlineMethod(1);
    }
}
