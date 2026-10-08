using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameLogic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class InitialDemoInputChecks
{
    public static async Task<object> Main()
    {
        var board=UnityEngine.Object.FindAnyObjectByType<BoardView>();
        var session=GameManager.Instance.Session;session.Restart();
        Vector2 position=board.WorldCamera.WorldToScreenPoint(board.ToWorld(new Vector2(3,0)));
        var backup=UnityEngine.Object.Instantiate(InputSystem.settings);
        var settings=UnityEngine.Object.Instantiate(backup);
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings=settings;
        var original=Mouse.current;bool wasEnabled=original.enabled;
        InputSystem.DisableDevice(original);
        var mouse=InputSystem.AddDevice<Mouse>("BilliardsInputCheck");mouse.MakeCurrent();
        try
        {
            InputSystem.QueueStateEvent(mouse,new MouseState {position=position,buttons=0});
            await UniTask.DelayFrame(3);
            InputSystem.QueueStateEvent(mouse,new MouseState {position=position,buttons=1});
            await UniTask.DelayFrame(3);
            if(!board.Ball.Shoot.Charging) throw new Exception("鼠标按下未蓄力；坐标="+Mouse.current.position.ReadValue());
            await UniTask.Delay(220,ignoreTimeScale:true);
            float power=board.Ball.Shoot.Power;
            InputSystem.QueueStateEvent(mouse,new MouseState {position=position,buttons=0});
            await UniTask.DelayFrame(3);
            if(session.ShotsLeft!=2)throw new Exception("鼠标释放未出杆；坐标="+Mouse.current.position.ReadValue()+"；状态="+session.State+"；蓄力="+board.Ball.Shoot.Charging);
            return new {charging=true,power,shotsLeft=session.ShotsLeft,state=session.State.ToString(),passed=true};
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);InputSystem.settings=backup;
            if(settings!=null)UnityEngine.Object.Destroy(settings);
            if(wasEnabled)InputSystem.EnableDevice(original);original.MakeCurrent();session.Restart();
        }
    }
}
