using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using GameLogic;
using GameConfig;
using Luban;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/// <summary>通过 Pipeline 对真实导出配置及 PhysicsWorld 执行隔离回放，不修改玩家进度。</summary>
public static class CampaignChecks
{
    /// <summary>只记录二维坐标的字段，避免将 Vector2 的派生属性写入回放报告。</summary>
    private sealed class ReportContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization serialization)
        {
            var properties = base.CreateProperties(type, serialization);
            if (type == typeof(Vector2))
                return new List<JsonProperty>(properties).FindAll(property => property.PropertyName == "x" || property.PropertyName == "y");
            return properties;
        }
    }
    [Serializable] public sealed class ReferenceDocument { public ReferenceLevel[] levels; }
    [Serializable] public sealed class ReferenceLevel { public int levelId; public ReferenceShot[] shots; }
    [Serializable] public sealed class ReferenceShot { public float angleDegrees; public float power; public Vector2 aimPoint; public string expected; }
    [Serializable] public sealed class ShotResult
    {
        public float angleDegrees, power;
        public bool accepted, stopped;
        public int remainingBlack;
        public Vector2 whiteStart, whiteEnd;
        public float whiteRadius;
        public bool growing;
        public bool whiteFoul;
        public List<BallResult> balls = new List<BallResult>();
        public List<string> events = new List<string>();
    }
    /// <summary>保存失败路线中各球的实际终态，便于修正下一杆输入或布局。</summary>
    [Serializable] public sealed class BallResult
    {
        public int id;
        public bool active;
        public Vector2 position;
        public float radius;
    }
    [Serializable] public sealed class LevelResult
    {
        public int levelId;
        public bool passed;
        public string error;
        public string name;
        public int maxShots;
        public bool[] completedGoals;
        public int[] propUses;
        public List<ShotResult> shots = new List<ShotResult>();
    }
    [Serializable] public sealed class Report
    {
        public string status;
        public int passed, total;
        public List<LevelResult> levels = new List<LevelResult>();
    }

    /// <summary>逐杆连续回放，仅通过正常 Shoot、Step 和犯规复位操作推进。</summary>
    public static object Replay()
    {
        string project = Directory.GetParent(Application.dataPath).FullName;
        var references = JsonConvert.DeserializeObject<ReferenceDocument>(File.ReadAllText(Path.Combine(project,"Docx/功能开发/数学桌球-二十关候选参考解.json")));
        var report = new Report { total = references.levels.Length };
        var tables = new Tables(name => new ByteBuf(File.ReadAllBytes(Path.Combine(Application.dataPath,"AssetRaw/Configs/bytes/"+name+".bytes"))));
        var singleton = typeof(ConfigSystem).GetField("_instance", BindingFlags.Static|BindingFlags.NonPublic);
        object previous = singleton.GetValue(null);
        var isolated = new ConfigSystem();
        typeof(ConfigSystem).GetField("_tables",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(isolated,tables);
        typeof(ConfigSystem).GetField("_init",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(isolated,true);
        singleton.SetValue(null,isolated);
        try
        {
            var manager = new LevelManager();
            foreach (var reference in references.levels)
            {
                var result = new LevelResult { levelId=reference.levelId };
                report.levels.Add(result);
                // 一关失败仍继续其他关，确保报告指出所有需要修正的布局。
                try
                {
                    var level = manager.LoadLevel(reference.levelId,CancellationToken.None);
                    result.name = level.Name;
                    result.maxShots = level.MaxShots;
                    var world = new PhysicsWorld(level);
                    foreach (var shot in reference.shots)
                    {
                        var record = new ShotResult { angleDegrees=shot.angleDegrees,power=shot.power,whiteStart=world.Balls[world.WhiteIndex].State.Position };
                        result.shots.Add(record);
                        float radians = shot.angleDegrees*Mathf.Deg2Rad;
                        record.accepted = world.Shoot(new Vector2(Mathf.Cos(radians),Mathf.Sin(radians)),shot.power);
                        if (!record.accepted) throw new InvalidOperationException("出杆尺寸无法放置。");
                        var completed = (bool[])world.GeometryCompleted.Clone();
                        var uses = (int[])world.PropUses.Clone();
                        var pockets = (PocketState[])world.PocketStates.Clone();
                        for (int step=0;world.Moving && step<3600;step++)
                        {
                            world.Step(level.Physics.Step);
                            for(int i=0;i<completed.Length;i++)
                                if(completed[i]!=world.GeometryCompleted[i])
                                {
                                    completed[i]=world.GeometryCompleted[i];
                                    record.events.Add("图形 "+level.Geometries[i].Id+"="+completed[i]);
                                }
                            for(int i=0;i<uses.Length;i++)
                                if(uses[i]!=world.PropUses[i])
                                {
                                    uses[i]=world.PropUses[i];
                                    record.events.Add("翻转 "+level.Props[i].Id+" 次数="+uses[i]);
                                }
                            for(int i=0;i<pockets.Length;i++)
                                if(pockets[i]!=world.PocketStates[i])
                                {
                                    pockets[i]=world.PocketStates[i];
                                    record.events.Add("洞 "+level.Pockets[i].Slot+"="+pockets[i]);
                                }
                        }
                        record.stopped=!world.Moving;
                        if(!record.stopped) throw new InvalidOperationException("30 秒模拟预算内未停球。");
                        record.whiteFoul = world.WhiteFoul;
                        if(world.WhiteFoul) world.RespawnWhite();
                        var white = world.Balls[world.WhiteIndex];
                        record.whiteEnd=white.State.Position;
                        record.whiteRadius=white.State.Radius;
                        record.growing=white.Growing;
                        record.remainingBlack=world.RemainingBlack;
                        foreach (var ball in world.Balls)
                            record.balls.Add(new BallResult { id=ball.Data.Id, active=ball.Active,
                                position=ball.State.Position, radius=ball.State.Radius });
                    }
                    result.completedGoals = (bool[])world.GeometryCompleted.Clone();
                    result.propUses = (int[])world.PropUses.Clone();
                    result.passed=world.RemainingBlack==0 && result.shots.Count<=level.MaxShots;
                    if(!result.passed) result.error="参考杆执行后仍有 "+world.RemainingBlack+" 颗黑球。";
                }
                catch(Exception exception) { result.error=exception.Message; }
                if(result.passed) report.passed++;
            }
        }
        finally { singleton.SetValue(null,previous); }
        report.status=report.passed==report.total ? "passed" : "needs_correction";
        File.WriteAllText(Path.Combine(project,"Docx/功能开发/数学桌球-二十关回放结果.json"),JsonConvert.SerializeObject(report,Formatting.Indented,new JsonSerializerSettings { ContractResolver = new ReportContractResolver() }));
        return report;
    }

    /// <summary>检查选关资源的保存引用与动态列表布局，不改动用户场景。</summary>
    public static object Prefabs()
    {
        var paths = new[] {"Assets/AssetRaw/UI/LevelSelectUI.prefab","Assets/AssetArt/UI/Widgets/LevelItemWidget.prefab"};
        foreach(string path in paths)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(root==null) throw new InvalidOperationException("缺少资源："+path);
            foreach(var node in root.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject)>0)
                    throw new InvalidOperationException("缺少脚本："+node.name);
            var components=new SerializedObject(root.GetComponent<UIBindComponent>()).FindProperty("m_components");
            for(int i=0;i<components.arraySize;i++)
                if(components.GetArrayElementAtIndex(i).objectReferenceValue==null)
                    throw new InvalidOperationException("绑定为空："+path+"#"+i);
            foreach(var text in root.GetComponentsInChildren<Text>(true))
                if(text.font==null) throw new InvalidOperationException("缺少字体："+text.name);
        }
        var window=AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]);
        var scroll=window.GetComponentInChildren<ScrollRect>(true);
        var grid=scroll.content.GetComponent<GridLayoutGroup>();
        if(!scroll.vertical || scroll.viewport==null || grid.constraintCount!=3 ||
           scroll.content.GetComponent<ContentSizeFitter>().verticalFit!=ContentSizeFitter.FitMode.PreferredSize)
            throw new InvalidOperationException("选关滚动布局配置不完整。");
        return new { savedAssets=paths,columns=grid.constraintCount,scrolling=true,bindings=true };
    }
}
