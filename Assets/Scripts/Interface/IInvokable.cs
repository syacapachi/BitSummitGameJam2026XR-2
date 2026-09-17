using System;

public interface IInvokable<T>
{
      public void Invoke(in T value);
}
public interface IInvokable
{
    public void Invoke();
}