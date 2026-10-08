using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameLogic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using SessionState = GameLogic.SessionState;

public static class Board2DChecks
{
    public static async Task<object> Start()
    {
        for(int i=0;i<600 && GameObject.Find("StartUI")==null;i++)await UniTask.NextFrame();
        if(GameObject.Find("StartUI")==null)throw new Exception("启动没有菜单。");
        await GameManager.Instance.StartGameAsync();
        if(GameManager.Instance.Session==null)throw new Exception("进入关卡失败。");
        var board=UnityEngine.Object.FindAnyObjectByType<BoardView>();
        board.SetAim(board.Ball.State.Position,Vector2.right,board.Ball.State.Radius,true);
        return new { ready=true, worldRoot=board.transform.parent==null, spawn=board.Ball.State.Position };
    }

    public static object Structure()
    {
        var board=UnityEngine.Object.FindAnyObjectByType<BoardView>();
        var session=GameManager.Instance.Session;
        if(board==null || session==null)throw new Exception("需要运行中的关卡。");
        var passed=new List<string>();
        Check(board.GetComponentInParent<Canvas>()==null,"桌球台在 Canvas 外",passed);
        Check(board.GetComponentsInChildren<RectTransform>(true).Length==0,"世界对象没有 RectTransform",passed);
        Check(board.GetComponentsInChildren<Graphic>(true).Length==0,"球与目标没有 UI Graphic",passed);
        Check(board.Ball.GetComponentInChildren<SpriteRenderer>()!=null,"球体使用 SpriteRenderer",passed);
        Check(board.Ball.GetComponent<CircleCollider2D>()!=null,"球体拥有 2D 碰撞体",passed);
        Check(board.WorldCamera.orthographic,"使用正交世界相机",passed);
        Vector2 screen=board.WorldCamera.WorldToScreenPoint(board.ToWorld(new Vector2(3,0)));
        Check(board.ScreenPoint(screen,out Vector2 local) && Vector2.Distance(local,new Vector2(3,0))<.0001f,"屏幕输入与台面目标坐标一致",passed);
        Check(!board.ScreenPoint(new Vector2(0,0),out _),"台面外坐标拒绝击球",passed);
        var ball=board.Ball;board.Runner.Bind(null);
        try
        {
            session.Restart();session.Shoot(Vector2.right,.03f);
            Vector2 start=ball.State.Position;
            var renderer=ball.GetComponentInChildren<SpriteRenderer>();
            var properties=new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);float phase=properties.GetFloat("_RollPhase");
            session.Tick(.1f);session.Tick(.1f);renderer.GetPropertyBlock(properties);
            Check(ball.State.Position.x>start.x+.5f && session.State==SessionState.Rolling,"白球从远处逐步滚动",passed);
            Check(Mathf.Abs(properties.GetFloat("_RollPhase")-phase)>.01f,"球面标记随路程滚动",passed);
            Check(Vector2.Distance(ball.transform.localPosition,ball.State.Position)<.0001f,"Transform 同步模拟位置",passed);
            Check(Mathf.Abs(ball.GetComponent<CircleCollider2D>().radius-ball.State.Radius)<.0001f,"碰撞半径同步尺寸",passed);
            renderer.GetPropertyBlock(properties);phase=properties.GetFloat("_RollPhase");
            session.TogglePause();session.Tick(1);renderer.GetPropertyBlock(properties);
            Check(properties.GetFloat("_RollPhase")==phase,"暂停冻结球面滚动",passed);
        }
        finally { session.Restart();board.Runner.Bind(session); }
        return new { count=passed.Count,passed };
    }

    public static async Task<object> Input()
    {
        var board=UnityEngine.Object.FindAnyObjectByType<BoardView>();
        var session=GameManager.Instance.Session;session.Restart();
        Vector2 target=board.WorldCamera.WorldToScreenPoint(board.ToWorld(new Vector2(3,0)));
        Vector2 another=board.WorldCamera.WorldToScreenPoint(board.ToWorld(new Vector2(-5,2)));
        // InputManager 会销毁被替换的临时默认配置，因此保留一份完整副本用于恢复。
        var originalSettings=UnityEngine.Object.Instantiate(InputSystem.settings);
        var settings=UnityEngine.Object.Instantiate(originalSettings);
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings=settings;
        var original=Mouse.current;
        bool wasEnabled=original.enabled;
        InputSystem.DisableDevice(original);
        var mouse=InputSystem.AddDevice<Mouse>("BilliardsInputCheck");
        mouse.MakeCurrent();
        try
        {
            InputSystem.QueueStateEvent(mouse,new MouseState {position=target,buttons=0});
            await UniTask.DelayFrame(3);
            InputSystem.QueueStateEvent(mouse,new MouseState {position=target,buttons=1});
            await UniTask.DelayFrame(3);
            if(!board.Ball.Shoot.Charging)throw new Exception("未开始蓄力。状态="+session.State+"；屏幕="+target+"；鼠标="+Mouse.current.position.ReadValue()+"；UI="+UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject());
            InputSystem.QueueStateEvent(mouse,new MouseState {position=another,buttons=1});
            await UniTask.DelayFrame(8);
            InputSystem.QueueStateEvent(mouse,new MouseState {position=another,buttons=0});
            await UniTask.DelayFrame(3);
            Vector2 velocity=board.Ball.State.Velocity;
            if(session.ShotsLeft!=2 || velocity.x<=0 || Mathf.Abs(velocity.y)>.001f)
                throw new Exception("蓄力方向锁定或松开出杆失败："+velocity);
            return new { charging=true,lockedDirection=true,shots=session.ShotsLeft,velocity };
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);InputSystem.settings=originalSettings;
            if(settings!=null)UnityEngine.Object.Destroy(settings);
            if(wasEnabled)InputSystem.EnableDevice(original);original.MakeCurrent();session.Restart();
        }
    }

    public static object Prefabs()
    {
        var passed=new List<string>();
        foreach(string path in new[]{"Assets/AssetRaw/Actor/Billiards/BilliardsTable.prefab","Assets/AssetRaw/UI/GameMainUI.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(Transform node in root.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject)==0,"脚本完整："+node.name,passed);
                if(root.GetComponent<BoardView>()!=null)
                {
                    foreach(MonoBehaviour script in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if(script is not BoardView && script is not BallBase && script is not GeometryGraphic)continue;
                        var iterator=new SerializedObject(script).GetIterator();
                        while(iterator.NextVisible(true))
                            if(iterator.propertyType==SerializedPropertyType.ObjectReference && iterator.objectReferenceValue==null)
                                throw new Exception("序列化引用缺失："+script.GetType().Name+"."+iterator.name);
                    }
                    Check(root.GetComponentsInChildren<Canvas>(true).Length==0,"桌球 Prefab 无 Canvas",passed);
                    foreach(Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                        Check(renderer.sharedMaterial!=null && renderer.sharedMaterial.shader.isSupported,"材质有效："+renderer.name,passed);
                }
                else
                {
                    Check(root.GetComponentsInChildren<BallBase>(true).Length==0,"HUD 不包含白球",passed);
                    Check(root.GetComponentsInChildren<BoardView>(true).Length==0,"HUD 不包含桌球场景",passed);
                    var array=new SerializedObject(root.GetComponent<UIBindComponent>()).FindProperty("m_components");
                    for(int i=0;i<array.arraySize;i++)Check(array.GetArrayElementAtIndex(i).objectReferenceValue!=null,"HUD 绑定："+i,passed);
                }
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        return new { count=passed.Count,passed };
    }
    private static void Check(bool value,string name,List<string> passed)
    { if(!value)throw new Exception("检查失败："+name);passed.Add(name); }
}
