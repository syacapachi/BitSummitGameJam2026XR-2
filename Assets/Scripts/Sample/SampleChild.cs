using UnityEngine;

public class SampleChild : SampleScript
{
    public override void VirtualMethod()
    {
        base.VirtualMethod();
        Debug.Log($"THis is override Method invoke {this.GetType()}", gameObject);
    }
    public override void VirtualMethod(string name)
    {
        base.VirtualMethod();
        Debug.Log($"THis is override Method invoke {this.GetType()}, name = {name}", gameObject);
    }
}
