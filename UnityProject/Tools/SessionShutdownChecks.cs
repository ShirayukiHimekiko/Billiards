using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameLogic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>通过 Pipeline 回归测试实际对象销毁顺序；结束后恢复到开始菜单。</summary>
public static class SessionShutdownChecks
{
    public static async Task<object> Main()
    {
        await WaitForMenu();
        var passed=new List<string>();
        var manager=GameManager.Instance;
        try
        {
            for(int mode=0;mode<4;mode++)
            {
                await manager.StartGameAsync();
                var session=manager.Session;
                var board=UnityEngine.Object.FindAnyObjectByType<BoardView>();
                if(session==null || board==null)throw new Exception("关卡未准备。");
                session.Shoot(Vector2.right,.03f);
                session.Tick(.1f);
                board.Runner.Bind(null);
                string name;
                if(mode==0) name="正常返回菜单";
                else if(mode==1)
                {
                    name="白球先销毁";
                    UnityEngine.Object.Destroy(board.Ball.gameObject);
                }
                else if(mode==2)
                {
                    name="球面子对象先销毁";
                    var renderer=board.Ball.GetComponentInChildren<SpriteRenderer>();
                    UnityEngine.Object.Destroy(renderer.gameObject);
                }
                else
                {
                    name="整张台面先销毁";
                    UnityEngine.Object.Destroy(board.gameObject);
                }
                // 必须等到实际销毁，保留托管引用才能覆盖用户报错中的退出顺序。
                await UniTask.DelayFrame(2);
                await manager.ReturnToMenuAsync();
                session.Release();session.Release();session.Tick(1);
                if(session.Shoot(Vector2.right,1))throw new Exception("释放后仍允许出杆。");
                await manager.ReturnToMenuAsync();
                await UniTask.DelayFrame(2);
                if(manager.Session!=null || UnityEngine.Object.FindObjectsByType<WhiteBall>().Length!=0 ||
                    UnityEngine.Object.FindObjectsByType<BoardView>().Length!=0)
                    throw new Exception("清理仍残留对象："+name);
                passed.Add(name+"：清理、重复释放、旧会话 Tick 和出杆检查通过");
            }
            return new {count=passed.Count,passed};
        }
        finally {await manager.ReturnToMenuAsync();}
    }

    public static async Task<object> Rolling()
    {
        await WaitForMenu();
        await GameManager.Instance.StartGameAsync();
        var session=GameManager.Instance.Session;
        if(session==null)throw new Exception("关卡未准备。");
        session.Shoot(Vector2.right,.03f);
        return new {rolling=true};
    }

    public static object ExitButton()
    {
        var main=GameObject.Find("MainUI");
        if(main==null)throw new Exception("主界面未打开。");
        main.GetComponent<UIBindComponent>().GetComponent<Button>(9).onClick.Invoke();
        return new {exitButtonInvoked=true};
    }

    private static async UniTask WaitForMenu()
    {
        for(int frame=0;frame<600 && GameObject.Find("StartUI")==null;frame++)await UniTask.NextFrame();
        if(GameObject.Find("StartUI")==null)throw new Exception("开始菜单未出现。");
    }
}
