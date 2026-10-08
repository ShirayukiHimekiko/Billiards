using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameLogic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

/// <summary>在 Play Mode 通过实际 UI 回调检查二十关目录及通关导航，结束时回到选关。</summary>
public static class CampaignUIChecks
{
    /// <summary>记录通过的流程断言，不改写关卡状态来伪造通关。</summary>
    public static async Task<object> Main()
    {
        var passed = new List<string>();
        await Wait(() => GameObject.Find("StartUI") != null, "开始菜单未出现");
        Click(GameObject.Find("StartUI"), "m_btn_Start");
        await Wait(() => GameObject.Find("LevelSelectUI") != null, "开始按钮没有打开选关");
        await UniTask.DelayFrame(3);
        var window = GameObject.Find("LevelSelectUI");
        var cards = Cards(window);
        Check(cards.Count == 20, "动态生成二十张关卡卡片", passed);
        for (int i = 1; i <= 20; i++)
            Check(cards.ContainsKey(i), "目录包含关卡 " + i, passed);
        var scroll = window.GetComponentInChildren<ScrollRect>();
        Canvas.ForceUpdateCanvases();
        Check(scroll.content.rect.height > scroll.viewport.rect.height, "列表内容可滚动", passed);
        scroll.verticalNormalizedPosition = 0;
        Canvas.ForceUpdateCanvases();
        await UniTask.DelayFrame(2);
        var last = cards[20].GetComponent<RectTransform>();
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, last);
        Check(bounds.max.y >= scroll.viewport.rect.yMin && bounds.min.y <= scroll.viewport.rect.yMax,
            "滚动到底可见第二十关", passed);
        Click(cards[20], "m_btn_Enter");
        await Wait(() => GameManager.Instance.Session != null, "第二十关进入失败");
        Check(CurrentId() == 20, "卡片进入正确关卡 ID", passed);
        Click(GameObject.Find("MainUI"), "m_btn_Menu");
        await Wait(() => GameObject.Find("LevelSelectUI") != null && GameManager.Instance.Session == null,
            "游戏内返回选关失败");
        await UniTask.DelayFrame(3);
        Check(UnityEngine.Object.FindObjectsByType<WhiteBall>().Length == 0,
            "返回选关回收白球", passed);
        cards = Cards(GameObject.Find("LevelSelectUI"));
        Click(cards[10], "m_btn_Enter");
        await Wait(() => GameManager.Instance.Session != null, "第十关进入失败");
        Complete(10);
        Check(GameManager.Instance.Session.State == SessionState.Won, "第十关真实会话通关", passed);
        Click(GameObject.Find("MainUI"), "m_btn_Pause");
        await Wait(() => CurrentId() == 11 && GameManager.Instance.Session != null, "第十关下一关失败");
        Check(CurrentId() == 11, "第十关下一关进入第十一关", passed);
        await GameManager.Instance.ReturnToLevelSelectAsync();
        await UniTask.DelayFrame(3);
        Click(Cards(GameObject.Find("LevelSelectUI"))[20], "m_btn_Enter");
        await Wait(() => GameManager.Instance.Session != null && CurrentId() == 20, "第二十关再次进入失败");
        Complete(20);
        Check(GameManager.Instance.Session.State == SessionState.Won, "第二十关真实会话通关", passed);
        Click(GameObject.Find("MainUI"), "m_btn_Pause");
        await Wait(() => GameObject.Find("LevelSelectUI") != null && GameManager.Instance.Session == null,
            "最后一关下一关没有返回选关");
        Check(true, "最后一关返回选关", passed);
        Click(GameObject.Find("LevelSelectUI"), "m_btn_Back");
        await Wait(() => GameObject.Find("StartUI") != null, "选关返回主菜单失败");
        Check(true, "选关返回主菜单", passed);
        Click(GameObject.Find("StartUI"), "m_btn_Start");
        await Wait(() => GameObject.Find("LevelSelectUI") != null, "再次打开选关失败");
        await UniTask.DelayFrame(3);
        Check(Cards(GameObject.Find("LevelSelectUI")).Count == 20, "重新打开列表数量正确", passed);
        var report = new { status = "passed", checks = passed };
        string project = Directory.GetParent(Application.dataPath).FullName;
        File.WriteAllText(Path.Combine(project, "Docx/功能开发/数学桌球-二十关UI流程检查.json"),
            JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    /// <summary>通过参考击球输入推进真实 Session，包括固定步长结算与事件发布。</summary>
    private static void Complete(int levelId)
    {
        string project = Directory.GetParent(Application.dataPath).FullName;
        var references = JsonConvert.DeserializeObject<Document>(File.ReadAllText(Path.Combine(project,
            "Docx/功能开发/数学桌球-二十关候选参考解.json")));
        var reference = Array.Find(references.levels, x => x.levelId == levelId);
        var board = UnityEngine.Object.FindAnyObjectByType<BoardView>();
        var session = GameManager.Instance.Session;
        board.Runner.Bind(null);
        try
        {
            foreach (var shot in reference.shots)
            {
                float radians = shot.angleDegrees * Mathf.Deg2Rad;
                if (!session.Shoot(new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)), shot.power))
                    throw new InvalidOperationException("真实会话拒绝参考出杆：" + levelId);
                for (int step = 0; step < 3600 && session.State == SessionState.Rolling; step++)
                    session.Tick(1f / 120);
                if (session.State == SessionState.Rolling)
                    throw new InvalidOperationException("真实会话未停球：" + levelId);
            }
        }
        finally { board.Runner.Bind(session); }
    }

    /// <summary>按卡片中显示的关卡编号建立映射，排除隐藏模板。</summary>
    private static Dictionary<int, GameObject> Cards(GameObject window)
    {
        var cards = new Dictionary<int, GameObject>();
        foreach (var text in window.GetComponentsInChildren<Text>())
            if (text.name == "m_text_Number")
                cards.Add(int.Parse(text.text), text.GetComponentInParent<UIBindComponent>().gameObject);
        return cards;
    }

    /// <summary>使用实际按钮事件，不直接调用替代的流程入口。</summary>
    private static void Click(GameObject root, string name)
    {
        foreach (var button in root.GetComponentsInChildren<Button>())
            if (button.name == name)
            {
                if (!button.interactable) throw new InvalidOperationException("按钮不可用：" + name);
                button.onClick.Invoke();
                return;
            }
        throw new InvalidOperationException("找不到按钮：" + name);
    }

    /// <summary>只读当前流程保存的关卡 ID。</summary>
    private static int CurrentId() => (int)typeof(GameManager).GetField("_levelId",
        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameManager.Instance);

    /// <summary>等待异步 UI 或加载在帧循环中完成，最多六百帧。</summary>
    private static async Task Wait(Func<bool> condition, string message)
    {
        for (int frame = 0; frame < 600 && !condition(); frame++) await UniTask.NextFrame();
        if (!condition()) throw new InvalidOperationException(message);
    }

    /// <summary>记录成功项，失败时保留明确原因。</summary>
    private static void Check(bool value, string description, List<string> passed)
    {
        if (!value) throw new InvalidOperationException(description);
        passed.Add(description);
    }

    [Serializable] public sealed class Document { public ReferenceLevel[] levels; }
    [Serializable] public sealed class ReferenceLevel { public int levelId; public Shot[] shots; }
    [Serializable] public sealed class Shot { public float angleDegrees, power; }
}
