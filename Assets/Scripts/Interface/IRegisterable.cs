public interface IResisterable<T> 
{
    public void Register(T manager);
    public void Unregister(T manager);
}
