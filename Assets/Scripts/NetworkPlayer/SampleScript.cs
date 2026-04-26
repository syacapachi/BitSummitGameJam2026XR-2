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
    public void SampleMethodWithParameter(string message)
    {
        Debug.Log("Message: " + message);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(SampleEnum message)
    {
        Debug.Log("Message: " + message);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(int number)
    {
        Debug.Log("Number: " + number);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(List<float> value)
    {
        string valueString = string.Join(", ", value);
        Debug.Log("Value: " + valueString);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(List<GameObject> value)
    {
        string valueString = string.Join(", ", value);
        Debug.Log("Value: " + valueString);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(InlineClass inlineClass)
    {
        inlineClass.InlineMethod();
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(Dictionary<int,bool> dic, string message)
    {
        string dicString = string.Join(", ", dic);
        Debug.Log("Message: " + dicString + message);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(GameObject obj, PhaseSO so)
    {
        Debug.Log("Message: " + obj.name + so.name);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(IInLineInterface resisterable)
    {
        resisterable.InlineMethod();
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(UnityEvent invokeEvent)
    {
        invokeEvent.Invoke();
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(LayerMask invokeEvent)
    {
        Debug.Log("Value: " + invokeEvent.value);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(Quaternion invokeEvent)
    {
        Debug.Log("Value: " + invokeEvent);
    }
    [OnInspectorButton]
    public void SampleMethodWithParameter(DateTime invokeEvent)
    {
        Debug.Log("Value: " + invokeEvent);
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
}
