using System;
using System.Collections.Generic;
using GameLogic;
using UnityEngine;

/// <summary>通过 Pipeline 执行的确定性规则检查，不修改配置或运行状态。</summary>
public static class InitialDemoChecks
{
    public static object Main()
    {
        var passed=new List<string>();
        var parameters=new BallConfigData(.1f,3,1.5f,3,12,.3f,2.4f,2.5f,.8f,.8f,1f/120);
        var level=new LevelData(1,Rect.MinMaxRect(-3,-3,7,7),
            new Vector2(2,3.2f),new Vector2(.960769515f,1.4f),new Vector2(3.039230485f,1.4f),
            new Vector2(1.0015625f,2),.35f,.08f,new Vector2(2.30701048f,2),.1f,3,true,parameters);
        Check(GoalJudge.Check(level,new BallState {Position=new Vector2(2,2),Radius=.6f})==GoalKind.Inscribed,"内切正例",passed);
        Check(GoalJudge.Check(level,new BallState {Position=new Vector2(2,2),Radius=1.2f})==GoalKind.Circumscribed,"外接正例",passed);
        Check(GoalJudge.Check(level,new BallState {Position=new Vector2(2,2),Radius=.9f})==GoalKind.None,"错误半径反例",passed);
        Check(GoalJudge.Check(level,new BallState {Position=new Vector2(2,4),Radius=.6f})==GoalKind.None,"外部球心反例",passed);
        var judge=new GoalJudge();
        var segment=new BallTrajectory(new BallState {Position=level.Spawn,Velocity=new Vector2(3,0),Radius=.3f},new Vector2(-1.8f,0),.8,1);
        double hit=judge.FindGoal(level,segment);
        Check(hit>0 && hit<.4 && GoalJudge.Check(level,segment.At(hit))==GoalKind.Inscribed,"运动途中内切及候选复核",passed);
        double item=judge.FindItem(level,segment);
        Check(item>0 && item<1,"道具圆周接触",passed);
        var growing=new BallTrajectory(new BallState {Position=new Vector2(-2,2),Radius=.35f},Vector2.zero,2,1);
        double boundary=judge.FindBoundary(level,growing);
        Check(Math.Abs(boundary-.325)<.0001,"半径增长造成越界",passed);
        var fast=new BallTrajectory(new BallState {Position=new Vector2(-10,2),Velocity=new Vector2(30,0),Radius=.6f},Vector2.zero,0,1);
        double fastHit=judge.FindGoal(level,fast);
        Check(fastHit>.3 && fastHit<.5 && GoalJudge.Check(level,fast.At(0))==GoalKind.None &&
            GoalJudge.Check(level,fast.At(1))==GoalKind.None,"高速区间两端未命中仍找到途中目标",passed);
        var roots=new PolynomialRoots();
        // (t-.2)^2(t-.7)(t-.9)，包括偶重根。
        double[] coefficients=Multiply(Multiply(new[]{-.2,1.0},new[]{-.2,1.0}),Multiply(new[]{-.7,1.0},new[]{-.9,1.0}));
        int count=roots.Find(coefficients,4,0,1);
        Check(count==3 && Math.Abs(roots.Root(0)-.2)<1e-7 && Math.Abs(roots.Root(1)-.7)<1e-7 && Math.Abs(roots.Root(2)-.9)<1e-7,"四次多项式含偶重根",passed);
        bool rejected=false;
        try { new LevelData(1,level.Bounds,Vector2.zero,Vector2.one,new Vector2(2,2),level.Spawn,.35f,.08f,level.ItemPosition,.1f,3,true,parameters); }
        catch(ArgumentException) { rejected=true; }
        Check(rejected,"退化三角形拒绝发布",passed);
        for(int i=0;i<=4;i++)
        {
            double speed=3+9*i*.25, deceleration=speed*speed/(2*2.5),time=speed/deceleration;
            Check(Math.Abs(speed*time-.5*deceleration*time*time-2.5)<1e-9,"固定路程力量档 "+i,passed);
        }
        return new {passed=passed.Count,cases=passed};
    }
    private static double[] Multiply(double[] a,double[] b)
    {
        var c=new double[a.Length+b.Length-1];
        for(int i=0;i<a.Length;i++) for(int j=0;j<b.Length;j++) c[i+j]+=a[i]*b[j];return c;
    }
    private static void Check(bool condition,string name,List<string> passed)
    { if(!condition) throw new InvalidOperationException("验证失败："+name);passed.Add(name); }
}
