public interface IResultCollector
{
    public ulong ClientId { get; }
    public void SendMessage(string ket, object value);
}
