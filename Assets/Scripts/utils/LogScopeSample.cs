using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class LogScopeSample : MonoBehaviour
{
    [ContextMenu("01 Basic")]
    private void Basic()
    {
        using var log = LogScope.Create(this,"basic");

        LogScope.Log("開始");
        LogScope.Warning("警告");
        LogScope.Error("エラー");

        // Dispose時にまとめて出力
    }

    [ContextMenu("02 Nested Method")]
    private void NestedMethod()
    {
        using var log = LogScope.Create(this,"nested");

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
        using var log = LogScope.Create(this,"params");

        LogScope.Log(
            "Player",
            "Enemy",
            "Boss");
    }

    [ContextMenu("04 Caller Info")]
    private void CallerInfo()
    {
        using var log = LogScope.Create(this,"infos");

        TestCaller();
    }

    private void TestCaller()
    {
        LogScope.LogWithCaller("呼び出し元付きログ");
        LogScope.WarningWithCaller("呼び出し元付き警告");
        LogScope.ErrorWithCaller("呼び出し元付きエラー");
    }

    [ContextMenu("05 LogException")]
    private void ExceptionSample()
    {
        using var log = LogScope.Create(this,"exception");

        try
        {
            throw new InvalidOperationException("テスト例外");
        }
        catch (Exception e)
        {
            LogScope.LogException(e);
        }
    }

    [ContextMenu("06 Thread Safe")]
    private async void ThreadSafe()
    {
        var task1 = Task.Run(async () =>
        {
            // Taskの中では、競合するのでLogScope.CreateIsolated()を使う。
            using var log = LogScope.CreateIsolated(this,"task1");

            for(int i=0; i<10; i++)
            {
                Debug.Log($"Task1-{i}");
                LogScope.Log($"Task1-{i}");
                await Task.Delay(500);
            }
            
        });

        var task2 = Task.Run(async () =>
        {
            using var log = LogScope.CreateIsolated(this,"task2");

            for (int i = 0; i < 10; i++)
            {
                Debug.Log($"Task2-{i}");
                LogScope.Log($"Task2-{i}");
                await Task.Delay(1000);
            }
        });

        await Task.WhenAll(task1, task2);

        Debug.Log("All Tasks Finished");
    }

    [ContextMenu("07 Nested Builder")]
    private void NestedBuilder()
    {
        using (LogScope.Create(this,"nestBuilder"))
        {
            LogScope.Log("Outer");

            Inner();

            LogScope.Log("Outer End");
        }
    }

    private void Inner()
    {
        using (LogScope.Create(this,"InnerBuilder"))
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

    [ContextMenu("08 Coroutine")]
    private void Coroutine()
    {
        using (LogScope.Create(this,"outer"))
        {
            LogScope.Log("Outer Start");

            LogScope.Log($"LogCoroutine Call");
            StartCoroutine(LogScope.Run(LogCoroutine(10),this,"innerCoroutine"));

            LogScope.Log("Outer End");
        }
    }
    [ContextMenu("09 InnerCoroutine")]
    private void InnerCoroutine()
    {
        using (LogScope.Create(this,"outer"))
        {
            LogScope.Log("Outer Start");

            LogScope.Log($"ScopeLogCoroutine Call");
            StartCoroutine(LogScope.Run(ScopeLogCoroutine(10,1),this, "innerCoroutine"));

            LogScope.Log("Outer End");
        }
        LogScope.Log($"out ScopeLogCoroutine Call");
        StartCoroutine(LogScope.Run(ScopeLogCoroutine(10,2),this, "outerCoroutine"));
    }
    [ContextMenu("10 NestedCoroutine")]
    private void NestedCoroutine()
    {
        using (LogScope.Create(this,"outer"))
        {
            LogScope.Log("Outer Start");

            LogScope.Log($"IsolatedScopeLogCoroutine Call");
            StartCoroutine(LogScope.Run(NestedCoroutine(10,1),this,"innerCoroutine"));

            LogScope.Log("Outer End");
        }
        LogScope.Log($"out IsolatedScopeLogCoroutine Call");
        StartCoroutine(NestedCoroutine(10,10));
    }
    IEnumerator LogCoroutine(int roop, int id = 0)
    {
        LogScope.Log($"LogCoroutine{id} Start");
        for (int i = 0;i < roop;i++)
        {
            yield return null;
            LogScope.Log($"LogCoroutine{id} {i}");
        }
        LogScope.Log($"LogCoroutine{id} End");
    }
    IEnumerator ScopeLogCoroutine(int roop, int id = 0)
    {
        using (var outscope = LogScope.Create(this, "scopeLog"))
        {
            LogScope.Log($"ScopeLogCoroutine{id} Start");
        }
        
        using var scope = LogScope.Create(this);
        LogScope.Log($"ScopeLogCoroutine{id} Create");
        for (int i = 0; i < roop; i++)
        {
            yield return null;
            LogScope.Log($"ScopeLogCoroutine{id} {i}");
        }
        LogScope.Log($"ScopeLogCoroutine{id} End");
    }
    IEnumerator NestedCoroutine(int roop,int id)
    {
        using(var scope  = LogScope.Create(this,"nestedCoroutine"))
        {
            LogScope.Log("Start Nest");
        }
        LogScope.Log($"NestedCoroutine{id} Create");
        for (int i = 0; i < 3; i++)
        {
            yield return LogScope.RunRecrucive(ScopeLogCoroutine(roop, id + i),this, "innerCoroutine");
            LogScope.Log($"NestedCoroutine{id} {i}");
        }
        LogScope.Log($"NestedCoroutine{id} End");
    }
}