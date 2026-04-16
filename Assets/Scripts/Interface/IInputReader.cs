using UnityEngine.InputSystem;

/// <summary>
/// out T => は共変性をもつ。
/// class Parent : Child のとき、
/// IInputReader<Child> : IInputReader<Parent>となる(継承関係を維持する。)
/// つまり、
/// IInputReader<Child> childReader : IInputReader<Parent>();ができる。
/// => このとき、関数の引数にできるが、返り値にはできない。
/// 例;
/// IInputReader<Parent> parentRead = new IInputReader<Child>();
/// Parent p = parentResit.Read();//左辺値より、関数の返り値はParentで、実体はChild=>アップキャスト安全!!
/// 
/// IInputReader<Child> childRead = new IInputReader<Parent>();
/// Child c = childResist.Read();//左辺値より、関数の返り値はChildだが、実体はParent=>ダウンキャストで危険!!
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IInputReader<out T>
{
    T ReadValue(InputAction.CallbackContext context);
}
