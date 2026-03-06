using UnityEngine;
using System.Collections.Generic;
public class SampleScript : MonoBehaviour
{
    public class InlineClass
    {
        public string name;
        public void InlineMethod()
        {
            Debug.Log($"This is an inline method. name = {name}");
        }
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
}
