using System;
using System.Collections.Generic;
using GameLogic;
using UnityEngine;

/// <summary>仅由 Pipeline 在试玩中执行，检查后恢复初始关卡，不进入游戏程序集。</summary>
public static class InitialDemoRuntimeChecks
{
    public static object Main()
    {
        var manager=GameManager.Instance;var session=manager.Session;
        if(session==null) throw new InvalidOperationException("请先进入关卡。");
        var board=UnityEngine.Object.FindAnyObjectByType<BoardView>();
        var ball=board.Ball;var passed=new List<string>();board.Runner.Bind(null);
        try
        {
            session.Restart();Check(session.State==SessionState.Ready && session.ShotsLeft==3,"重开恢复三次机会",passed);
            Check(session.Shoot(Vector2.right,.03f),"可以出杆",passed);
            Check(!session.Shoot(Vector2.right,.03f) && session.ShotsLeft==2,"运动中禁止重复出杆扣次数",passed);
            session.Tick(.05f);var before=ball.State;float time=ball.Simulation.RemainingTime;
            session.TogglePause();session.Tick(10);
            Check(session.State==SessionState.Paused && ball.State.Position==before.Position && ball.Simulation.RemainingTime==time,"暂停冻结位置半径和运动时间",passed);
            session.TogglePause();RunToEnd(session);
            Check(session.State==SessionState.Won && !session.ItemUsed,"远距离滚动内切成功",passed);
            Check(ball.State.Velocity==Vector2.zero && !session.Shoot(Vector2.right,1),"成功停球并锁定后续出杆",passed);
            session.Restart();session.Shoot(Vector2.right,1);RunToEnd(session);
            Check(session.State==SessionState.Won && session.Message=="外接成功！","真实配置：外接目标可达成",passed);
            for(int i=0;i<2;i++)
            {
                if(i==0)session.Restart();session.Shoot(Vector2.up,.9f);RunToEnd(session);
                Check(session.State==SessionState.Ready && session.ShotsLeft==2-i && ball.Simulation.Growing && !session.ItemUsed,"失败自动复位 "+(i+1),passed);
            }
            session.Shoot(Vector2.up,.9f);RunToEnd(session);
            Check(session.State==SessionState.Lost && session.ShotsLeft==0 && !session.Shoot(Vector2.right,.15f),"三次失败进入最终失败并锁定",passed);
            session.Restart();for(int i=0;i<2;i++){session.Shoot(Vector2.up,.9f);RunToEnd(session);}
            session.Shoot(Vector2.right,.03f);RunToEnd(session);
            Check(session.State==SessionState.Won && session.ShotsLeft==0,"最后一次机会成功优先于次数耗尽",passed);
            session.Restart();session.LoseFocus();session.LoseFocus();
            Check(session.State==SessionState.Paused,"失焦不会反复切换回继续",passed);
            session.Restart();
            session.Shoot((new Vector2(-1.5f,-1.4f)-new Vector2(-5,0)).normalized,.3f);
            for(int i=0;i<300 && !session.ItemUsed && session.State==SessionState.Rolling;i++)session.Tick(1f/60);
            Check(session.ItemUsed && !ball.Simulation.Growing,"偏离直线路径接触翻转道具",passed);
            return new {passed=passed.ToArray(),count=passed.Count};
        }
        finally {session.Restart();board.Runner.Bind(session);}
    }
    private static void RunToEnd(Session session)
    {for(int i=0;i<500 && session.State==SessionState.Rolling;i++)session.Tick(1f/60);if(session.State==SessionState.Rolling)throw new Exception("运动未结束");}
    private static void Check(bool value,string name,List<string> passed)
    {if(!value)throw new Exception("验收失败："+name+"；状态="+GameManager.Instance.Session.State+"；提示="+GameManager.Instance.Session.Message);passed.Add(name);}
}
