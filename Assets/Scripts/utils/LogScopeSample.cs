using System;
using System.Threading.Tasks;
using UnityEngine;

public class LogScopeSample : MonoBehaviour
{
    [ContextMenu("01 Basic")]
    private void Basic()
    {
        using var log = LogScope.Create(this);

        LogScope.Log("開始");
        LogScope.Warning("警告");
        LogScope.Error("エラー");

        // Dispose時にまとめて出力
    }

    [ContextMenu("02 Nested Method")]
    private void NestedMethod()
    {
        using var log = LogScope.Create(this);

        Func1();
    }

    private void Func1()
    {
        LogScope.Log("Func1");

        Func2();

        LogScope.Log("Func1 End");
    }

    private void Func2()
    {
        LogScope.Log("Func2");

        Func3();
    }

    private void Func3()
    {
        LogScope.Log("Func3");
    }

    [ContextMenu("03 Params")]
    private void ParamsSample()
    {
        using var log = LogScope.Create(this);

        LogScope.Log(
            "Player",
            "Enemy",
            "Boss");
    }

    [ContextMenu("04 Caller Info")]
    private void CallerInfo()
    {
        using var log = LogScope.Create(this);

        TestCaller();
    }

    private void TestCaller()
    {
        LogScope.LogWithCaller("呼び出し元付き");
    }

    [ContextMenu("05 LogException")]
    private void ExceptionSample()
    {
        using var log = LogScope.Create(this);

        try
        {
            throw new InvalidOperationException("テスト例外");
        }
        catch (Exception e)
        {
            LogScope.LogException(e);
        }
    }

    [ContextMenu("06 Operator")]
    private void OperatorSample()
    {
        using var log = LogScope.Create(this);

        _ = log + "Operatorで追加";

        string text = log;

        Debug.Log(text);
    }

    [ContextMenu("07 Thread Safe")]
    private async void ThreadSafe()
    {
        var task1 = Task.Run(() =>
        {
            using var log = LogScope.Create();

            LogScope.Log("Task1-1");
            LogScope.Log("Task1-2");
        });

        var task2 = Task.Run(() =>
        {
            using var log = LogScope.Create();

            LogScope.Log("Task2-1");
            LogScope.Log("Task2-2");
        });

        await Task.WhenAll(task1, task2);

        Debug.Log("All Tasks Finished");
    }

    [ContextMenu("08 Nested Builder")]
    private void NestedBuilder()
    {
        using (LogScope.Create(this))
        {
            LogScope.Log("Outer");

            Inner();

            LogScope.Log("Outer End");
        }
    }

    private void Inner()
    {
        using (LogScope.Create(this))
        {
            LogScope.Log("Inner");

            Deep();

            LogScope.Log("Inner End");
        }
    }

    private void Deep()
    {
        LogScope.Log("Deep");
    }
}