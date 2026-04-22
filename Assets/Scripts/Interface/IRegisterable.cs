/// <summary>
/// in T => は反変性をもつ。
/// class Parent : Child のとき、
/// IResisterable<Parent> : IResistable<Child>となる(継承関係が逆になる)
/// つまり、
/// IResisterable<Child> childResist = new IResisterable<Parent>();ができる。
/// => このとき、関数の引数にできるが、返り値にはできない。
/// 例;
/// IResisterable<Parent> parentResit = new IResisterable<Child>();
/// parentResit.Resist((Parent) p);//左辺値より、関数の引数はParentだが、実体はChild=>ダウンキャスト危険!!
/// 
/// IResisterable<Child> childResist = new IResisterable<Parent>();
/// childResist.Resist((Child) c);//左辺値より、関数の引数はChildで、実体はParent=>アップキャストで安全!!
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IResisterable<in T> 
{
    public void Register(T manager);
    public void Unregister(T manager);
}
