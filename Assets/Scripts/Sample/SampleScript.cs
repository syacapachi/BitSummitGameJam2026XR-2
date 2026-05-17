using Syacapachi.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
public class SampleScript : AbstructSample
{
    [ShowInspector, SerializeField] int a;
    [ShowInspector, SerializeField] float b;
    [ShowInspector, SerializeField] Vector3 vec;
    [ShowInspector, SerializeField] Color color;
    [ShowInspector, SerializeField] GameObject obj;
    [SerializeField] InlineClass clazz;
    [ShowInspector, SerializeField] List<float> list = new List<float>();
    [ShowInspector, SerializeField] List<InlineClass> classList = new List<InlineClass>();
    [ShowInspector, SerializeField] Dictionary<int, string> adic = new Dictionary<int, string>();

    [SerializeReference, SerializeReferenceView,SerializeField]
    IInLineInterface resultCollector;
    [SerializeReference, SerializeReferenceView]
    IInLineInterface[] resultCollectors;
    [SerializeReference, SerializeReferenceView]
    List<IInLineInterface> resultCollectorList;
    public interface IInLineInterface
    {
        public string Name { get;}
        [OnInspectorButton("Interface")]
        public void InlineMethod();
    }
    public interface IInLineGenericInterface<T>
    {
        public T InlineValue {  get;}
        public void InlineMethod(T value);
    }
    [Serializable]
    public class InlineClass : IInLineInterface
    {
        public string name;
        public int[] ints;
        public string Name => name;
        [OnInspectorButton("Throw Exception")]
        public void InlineMethod()
        {
            Debug.Log($"This is an inline method. name = {name}, ints ={string.Join(",",ints)}");
            throw new Exception();
        }
    }
    public class InLineClass2 : IInLineInterface
    {
        public int number;
        public string name;
        public string Name => name;
        public void InlineMethod()
        {
            Debug.Log($"This is an inline method. number = {number}");
        }
    }
    public class InLineClass3 : IInLineGenericInterface<string>
    {
        public string name;
        public string InlineValue => name;
        public void InlineMethod(string value)
        {
            Debug.Log($"This is an inline method. name = {name}, value = {value}");
        }
    }
    public class InlineMonoClass : MonoBehaviour, IInLineInterface
    {
        [SerializeField] AnimationCurve curve;
        public string Name => name;
        public void InlineMethod()
        {
            Debug.Log($"curve {curve.Evaluate(0f)}", this.gameObject);
        }
    }
    public struct InLineStruct : IInLineInterface, IInLineGenericInterface<string>
    {
        public string name;
        public readonly string Name => name;
        public readonly string InlineValue => name;
        public readonly void InlineMethod()
        {
            Debug.Log($"This is an inline method. name = {name}");
        }

        public readonly void InlineMethod(string value)
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
    //privateにすると見えない
    [OnInspectorButton(Order = -1)]
    public void PublicMethod()
    {
        Debug.Log($"This is a public method invoked by {this.GetType()}", gameObject);
    }
    //privateにすると見えない
    [OnInspectorButton(Order = -1)]
    protected void ProtectedMethod()
    {
        Debug.Log($"This is a protected method invoked by {this.GetType()}.", gameObject);
    }
    //privateにすると見えない
    [OnInspectorButton(Order = -1)]
    internal void InternalMethod()
    {
        Debug.Log($"This is a internal method invoked by {this.GetType()}.", gameObject);
    }
    [OnInspectorButton(Order = -1)]
    private void PrivateMethod()
    {
        Debug.Log($"This is a private methodinvoked by {this.GetType()}", gameObject);
    }
    [OnInspectorButton(Order = -1)]
    public virtual void VirtualMethod()
    {
        Debug.Log($"This is a base method invoked by {this.GetType()}", gameObject);
    }
    [OnInspectorButton(Order = -1)]
    public virtual void VirtualMethod(string name)
    {
        Debug.Log($"This is a base method invoked by {this.GetType()} and name = {name}", gameObject);
    }
    [OnInspectorButton("Absctuct Override")]
    public override void AbstructMethod()
    {
        Debug.Log("This is a abstruct Methid", gameObject);
    }
    [OnInspectorButton(ValidateInvoke = true)]
    public void ValidateImvokeMethodWithSerializableClass(InlineClass lass)
    {
        if (lass.name.Length > 100)
        {
            Debug.Log("Too Long Name", gameObject);
            return;
        }
        Debug.Log("This is a sample method.", gameObject);
        lass.name = lass.name + name;
    }
    [OnInspectorButton(ValidateInvoke = true,HideWhenChildClass = true,Order = -2)]
    public void ValidateImvokeMethodWithSerializableClassArray(InLineStruct[] script)
    {
        Debug.Log(string.Join(",",script.Select(t => t.name)), gameObject);
    }
    [OnInspectorButton(HideWhenChildClass = true)]
    public void SampleMethodWithValueTYpe(string message, int number)
    {
        Debug.Log("Message: " + message + ", Number: " + number, gameObject);
    }
    [OnInspectorButton(HideWhenChildClass = true)]
    public void SampleMethodWithEnum(SampleEnum message)
    {
        Debug.Log("Message: " + message, gameObject);
    }
    [OnInspectorButton(HideWhenChildClass = true)]
    public void SampleMethodWithList(List<float> value, List<ScriptableObject> valueObject, List<InlineClass> inlineClasses, List<IInLineInterface> inlineInterfaces, List<IInLineGenericInterface<int>> inlineGenericInterfaces)
    {
        string valueString = string.Join(", ", value);
        string valueObjectString = string.Join(", ", valueObject);
        string inlineClassString = string.Join(", ", inlineClasses.Select(t => t.name + " " + string.Join(",", t.ints)));
        string inlineInterfaceString = string.Join(", ", inlineInterfaces.Select(t => t.Name));
        string inlineGenericInterfaceString = string.Join(", ", inlineGenericInterfaces.Select(t => t.InlineValue));
        Debug.Log("Value: " + valueString + ", " + valueObjectString + ", " + inlineClassString + ", " + inlineInterfaceString + ", " + inlineGenericInterfaceString, gameObject);
    }
    [OnInspectorButton(HideWhenChildClass = true)]
    public void SampleMethodWithDic(Dictionary<InlineClass, IInLineInterface> dic, Dictionary<IInLineGenericInterface<string>, IInLineGenericInterface<int>> dic2)
    {
        string dicString = string.Join(", ", dic.Keys,dic.Values);
        string dic2String = string.Join(", ", dic2.Keys,dic2.Values);
        Debug.Log("Message: " + dicString + ", " + dic2String, gameObject);
    }
    [OnInspectorButton(HideWhenChildClass = true)]
    public void SampleMethodWithUnityEvent(UnityEvent invokeEvent)
    {
        invokeEvent.Invoke();
    }
    [OnInspectorButton(HideWhenChildClass = true)]
    public void SampleMethodWithUnityClass(LayerMask mask, Quaternion quatanion, DateTime time, AnimationCurve curve)
    {
        Debug.Log("Value: " + mask + ", " + quatanion + ", " + time + curve.Evaluate(0f), gameObject);
    }
    [OnInspectorButton(HideWhenChildClass = true)]
    public void SampleMethodWithArrays(int[] arr, InlineClass[] inlineClasses, IInLineInterface[] inlineInterfaces, IInLineGenericInterface<string>[] inLineGenericInterface)
    {
        Debug.Log(string.Join
            ("\n",
                string.Join(",", arr),
                string.Join(",", inlineClasses.Select(t => t.name +" "+ string.Join(",",t.ints))),
                string.Join(",", inlineInterfaces.Select(t => t.Name)),
                string.Join(",", inLineGenericInterface.Select(t => t.InlineValue))
            )
            ,gameObject);
                
    }
    [OnInspectorButton(HideWhenChildClass = true)]
    public void SampleMethodWithGeneticClass<F>(GeneticClass<F> invokeEvent)
    {
        Debug.Log("Value: " + invokeEvent.value, gameObject);
    }
    [OnInspectorButton(HideWhenChildClass = true)]
    public void SampleMethodWithGeneticClass(GeneticClass<int> invokeEvent)
    {
        Debug.Log("Value: " + invokeEvent.value, gameObject);
    }
    [OnInspectorButton(HideWhenChildClass = true,Order = -2)]
    public void SampleMethodWithInterface(IInLineInterface resisterable, IInLineGenericInterface<string> invokeEvent)
    {
        resisterable.InlineMethod();
        invokeEvent.InlineMethod("Sample Value");
    }
    [OnInspectorButton(HideWhenChildClass = true)]
    public void SampleMethodWithGeneticInterface(IInLineGenericInterface<int> invokeEvent)
    {
        invokeEvent.InlineMethod(1);
    }
}
