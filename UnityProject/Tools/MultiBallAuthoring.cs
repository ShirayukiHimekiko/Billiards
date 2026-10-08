using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GameLogic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>通过 Pipeline run_script 迁移桌球 Prefab，保留已有台面美术和 UI 绑定。</summary>
public static class MultiBallAuthoring
{
    private const string Content="Assets/AssetRaw/Actor/Billiards/";
    /// <summary>实体 Prefab 引用的网格目录。</summary>
    private const string MeshContent="Assets/AssetArt/Meshes/Primitives/";
    /// <summary>保存按用途分类的制作期材质。</summary>
    private const string MaterialContent="Assets/AssetArt/Materials/";
    private static Material _tint;
    private static Mesh _disk,_quad;
    /// <summary>仅在现场与已保存场景完全一致时清除临时制作造成的脏标记。</summary>
    public static object RestoreCleanSceneMarker()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(!scene.isDirty) return new { cleared=false,reason="场景已经干净" };
        var preview=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(scene.path);
        try
        {
            bool identical=SceneSignature(scene)==SceneSignature(preview);
            if(identical)
            {
                var manager=typeof(UnityEditor.SceneManagement.EditorSceneManager);
                var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
                var clear=manager.GetMethod("MarkSceneClean",flags,null,new[]{typeof(UnityEngine.SceneManagement.Scene)},null)
                    ?? manager.GetMethod("ClearSceneDirtiness",flags,null,new[]{typeof(UnityEngine.SceneManagement.Scene)},null);
                if(clear==null) return new { cleared=false,reason="当前版本没有清理脏标记 API，保留场景现场" };
                clear.Invoke(null,new object[]{scene});
            }
            return new { cleared=identical,scene=scene.path,sceneDirty=scene.isDirty };
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
    }
    /// <summary>规范化场景对象引用后比较组件序列化内容，不写入场景。</summary>
    private static string SceneSignature(UnityEngine.SceneManagement.Scene scene)
    {
        var objects=new List<Object>(); var identities=new Dictionary<ulong,string>();
        var roots=scene.GetRootGameObjects();
        for(int i=0;i<roots.Length;i++) Collect(roots[i],i.ToString(),objects,identities);
        var result=new StringBuilder();
        foreach(var obj in objects)
        {
            string json=EditorJsonUtility.ToJson(obj);
            json=Regex.Replace(json,@"""instanceID"":\s*(-?\d+)",match=>
            {
                ulong id=unchecked((ulong)long.Parse(match.Groups[1].Value));
                if(id==0) return "\"reference\":\"null\"";
                if(identities.TryGetValue(id,out string identity)) return "\"reference\":\""+identity+"\"";
                var value=EditorUtility.EntityIdToObject(EntityId.FromULong(id));
                if(value!=null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long localId))
                    return "\"reference\":\""+guid+":"+localId+"\"";
                return match.Value;
            });
            result.Append(identities[EntityId.ToULong(obj.GetEntityId())]).Append(json);
        }
        return result.ToString();
    }
    /// <summary>为场景中的对象和组件建立不依赖实例 ID 的标识。</summary>
    private static void Collect(GameObject go,string path,List<Object> objects,Dictionary<ulong,string> identities)
    {
        identities.Add(EntityId.ToULong(go.GetEntityId()),path+"/GameObject"); objects.Add(go);
        var components=go.GetComponents<Component>();
        for(int i=0;i<components.Length;i++)
        {
            if(components[i]==null) throw new InvalidOperationException("场景含有 Missing Script，不能自动清除脏标记。");
            identities.Add(EntityId.ToULong(components[i].GetEntityId()),path+"/Component"+i); objects.Add(components[i]);
        }
        for(int i=0;i<go.transform.childCount;i++) Collect(go.transform.GetChild(i).gameObject,path+"/"+i,objects,identities);
    }
    /// <summary>读取保存结果的关键绑定，不进入 Play Mode 或修改场景。</summary>
    public static object Inspect()
    {
        var table=AssetDatabase.LoadAssetAtPath<GameObject>(Content+"BilliardsTable.prefab");
        var board=table.GetComponent<BoardView>();
        var serialized=new SerializedObject(board);
        string[] names={"_felt","_cue","_prediction","_predictionDot","_predictionDotRenderer","_geometryTemplate","_pocketTemplate","_runner"};
        foreach(string field in names)
            if(serialized.FindProperty(field).objectReferenceValue==null) throw new InvalidOperationException("桌面绑定未保存："+field);
        var white=AssetDatabase.LoadAssetAtPath<GameObject>(Content+"BilliardsWhiteBall.prefab");
        var black=AssetDatabase.LoadAssetAtPath<GameObject>(Content+"BilliardsBlackBall.prefab");
        var prop=AssetDatabase.LoadAssetAtPath<GameObject>(Content+"BilliardsFlipProp.prefab");
        if(white.GetComponent<WhiteBall>()==null || white.GetComponent<BallShootComponent>()==null || black.GetComponent<BlackBall>()==null ||
            black.GetComponent<BallShootComponent>()!=null || prop.GetComponent<FlipProp>()==null)
            throw new InvalidOperationException("实体类型或白球专属输入组件未保存。");
        return new { boardBindings=names.Length,white=true,black=true,flipProp=true,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
            sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty };
    }
    /// <summary>制作球、道具以及桌面模板，仅保存本任务涉及的 Prefab。</summary>
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("制作要求编辑模式。");
        if(UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()!=null) throw new InvalidOperationException("请先关闭用户正在编辑的 Prefab Stage。");
        _tint=AssetDatabase.LoadAssetAtPath<Material>(MaterialContent+"Common/WorldTint.mat");
        foreach(string category in new[]{"Table","Aiming"})
            if(!AssetDatabase.IsValidFolder(MaterialContent+category))
                AssetDatabase.CreateFolder(MaterialContent.TrimEnd('/'),category);
        _disk=AssetDatabase.LoadAssetAtPath<Mesh>(MeshContent+"TableDisk.asset");
        _quad=AssetDatabase.LoadAssetAtPath<Mesh>(MeshContent+"TableQuad.asset");
        var root=PrefabUtility.LoadPrefabContents(Content+"BilliardsTable.prefab");
        try
        {
            var source=root.GetComponentInChildren<WhiteBall>(true);
            if(source!=null) BuildEntities(source,root.transform.Find("FlipItem"));
            else if(AssetDatabase.LoadAssetAtPath<GameObject>(Content+"BilliardsWhiteBall.prefab")==null)
                throw new InvalidOperationException("缺少旧白球来源或新版球 Prefab。");
            foreach(string name in new[]{"Target","InnerHint","OuterHint","TargetPoint","FlipItem","WhiteBall","Aim","GeometryTemplate","PocketTemplate","Prediction","PredictionDot"})
            {
                var old=root.transform.Find(name); if(old!=null) Object.DestroyImmediate(old.gameObject);
            }
            var geometry=Node("GeometryTemplate",root.transform).AddComponent<GeometryView>();
            Bind(geometry,"_figure",Graphic("Figure",geometry.transform,8));
            Bind(geometry,"_circle",Graphic("ConditionCircle",geometry.transform,7));
            geometry.gameObject.SetActive(false);
            var pocket=Node("PocketTemplate",root.transform).AddComponent<PocketView>();
            var mouth=Mesh("Mouth",pocket.transform,_disk,Vector3.one,new Color(.03f,.04f,.04f),5);
            Bind(pocket,"_renderer",mouth);
            var cap=Node("Closed",pocket.transform);
            Mesh("BarA",cap.transform,_quad,new Vector3(.7f,.1f,1),new Color(.9f,.8f,.55f),6).transform.localRotation=Quaternion.Euler(0,0,45);
            Mesh("BarB",cap.transform,_quad,new Vector3(.7f,.1f,1),new Color(.9f,.8f,.55f),6).transform.localRotation=Quaternion.Euler(0,0,-45);
            Bind(pocket,"_cap",cap.transform); pocket.gameObject.SetActive(false);
            var prediction=Graphic("Prediction",root.transform,10);
            var dot=Mesh("PredictionDot",root.transform,_disk,Vector3.one*.12f,Color.white,10,materialCategory: "Aiming");
            var board=root.GetComponent<BoardView>();
            Bind(board,"_geometryTemplate",geometry); Bind(board,"_pocketTemplate",pocket);
            Bind(board,"_prediction",prediction); Bind(board,"_predictionDot",dot.transform); Bind(board,"_predictionDotRenderer",dot);
            prediction.gameObject.SetActive(false); dot.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,Content+"BilliardsTable.prefab",out bool saved);
            if(!saved) throw new InvalidOperationException("新版桌球台保存失败。");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        UpdateInstructions(); AssetDatabase.SaveAssets();
        RestoreCleanSceneMarker();
        return new { table=Content+"BilliardsTable.prefab",white=Content+"BilliardsWhiteBall.prefab",black=Content+"BilliardsBlackBall.prefab",prop=Content+"BilliardsFlipProp.prefab" };
    }
    /// <summary>将旧台面上的白球和道具迁移为资源系统独立加载的 Prefab。</summary>
    private static void BuildEntities(WhiteBall source,Transform sourceItem)
    {
        GameObject white=Object.Instantiate(source.gameObject);
        GameObject black=null,prop=null;
        try
        {
            white.name="BilliardsWhiteBall"; white.transform.SetParent(null); white.transform.localPosition=Vector3.zero;
            Save(white,"BilliardsWhiteBall");
            black=Object.Instantiate(white); black.name="BilliardsBlackBall";
            var original=black.GetComponent<WhiteBall>();
            // 先移除声明组件依赖的白球，再移除白球独有的能力组件。
            Object.DestroyImmediate(original); Object.DestroyImmediate(black.GetComponent<BallShootComponent>());
            Object.DestroyImmediate(black.GetComponent<BallSimulationComponent>());
            black.AddComponent<BlackBall>();
            black.transform.Find("Surface").GetComponent<SpriteRenderer>().color=new Color(.075f,.085f,.095f);
            Save(black,"BilliardsBlackBall");
            if(sourceItem==null) throw new InvalidOperationException("旧桌面缺少翻转道具来源。");
            prop=Object.Instantiate(sourceItem.gameObject); prop.name="BilliardsFlipProp";
            prop.transform.SetParent(null); prop.transform.localPosition=Vector3.zero;
            var flip=prop.AddComponent<FlipProp>(); var sprite=prop.GetComponent<SpriteRenderer>();
            prop.transform.localScale=Vector3.one*(.468f/sprite.sprite.bounds.size.x); Bind(flip,"_renderer",sprite);
            Save(prop,"BilliardsFlipProp");
        }
        finally { Object.DestroyImmediate(white); if(black!=null) Object.DestroyImmediate(black); if(prop!=null) Object.DestroyImmediate(prop); }
    }
    /// <summary>
    /// 迁移已存在的两个球 Prefab：清理旧序列化引用和黑球上的白球能力组件。
    /// 仅由 Unity Pipeline 在编辑模式下执行，不修改场景或其他资源。
    /// </summary>
    /// <returns>成功保存的白球和黑球 Prefab 路径。</returns>
    public static object MigrateBallComponents()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("球组件迁移要求编辑模式。");
        if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("球组件迁移前请先关闭正在编辑的 Prefab Stage。");

        string whitePath = Content + "BilliardsWhiteBall.prefab";
        string blackPath = Content + "BilliardsBlackBall.prefab";
        GameObject white = null, black = null;
        try
        {
            white = PrefabUtility.LoadPrefabContents(whitePath);
            black = PrefabUtility.LoadPrefabContents(blackPath);
            ValidateBallLayout(white);
            ValidateBallLayout(black);
            if (white.GetComponent<WhiteBall>() == null || white.GetComponent<BallShootComponent>() == null ||
                white.GetComponent<BallSimulationComponent>() == null || black.GetComponent<BlackBall>() == null ||
                black.GetComponent<WhiteBall>() != null)
                throw new InvalidOperationException("球 Prefab 类型或白球必需能力组件不符合迁移要求。");

            foreach (var component in black.GetComponents<BallShootComponent>())
                Object.DestroyImmediate(component);
            foreach (var component in black.GetComponents<BallSimulationComponent>())
                Object.DestroyImmediate(component);

            // Unity 按当前脚本字段重新序列化，清理已取消序列化的内部组件引用。
            Save(white, "BilliardsWhiteBall");
            Save(black, "BilliardsBlackBall");
            return new { white = whitePath, black = blackPath };
        }
        finally
        {
            if (black != null) PrefabUtility.UnloadPrefabContents(black);
            if (white != null) PrefabUtility.UnloadPrefabContents(white);
        }
    }

    /// <summary>
    /// 在保存迁移结果前确认球体自动获取组件所需的固定节点结构。
    /// </summary>
    /// <param name="root">已经加载的球体 Prefab 根节点。</param>
    private static void ValidateBallLayout(GameObject root)
    {
        Transform surface = root.transform.Find("Surface");
        if (surface == null)
            throw new InvalidOperationException(root.name + " 缺少 Surface 节点。");
        var renderer = surface.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
            throw new InvalidOperationException(root.name + " 的 Surface 缺少 SpriteRenderer 或球面 Sprite。");
        if (root.transform.Find("BallShadow") == null || root.GetComponent<CircleCollider2D>() == null)
            throw new InvalidOperationException(root.name + " 缺少 BallShadow 节点或 CircleCollider2D。");
    }

    /// <summary>保存一个明确目标的 Prefab。</summary>
    private static void Save(GameObject root,string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root,Content+name+".prefab",out bool saved);
        if(!saved) throw new InvalidOperationException(name+" 保存失败。");
    }
    /// <summary>创建制作期节点。</summary>
    private static GameObject Node(string name,Transform parent)
    {
        var go=new GameObject(name); Undo.RegisterCreatedObjectUndo(go,"制作新版桌球资源"); go.transform.SetParent(parent,false); return go;
    }
    /// <summary>制作 Mesh 表现并按用途分类保存局部颜色材质。</summary>
    /// <param name="name">节点和材质名称。</param>
    /// <param name="parent">父节点。</param>
    /// <param name="mesh">使用的基础网格。</param>
    /// <param name="scale">局部缩放。</param>
    /// <param name="color">材质颜色。</param>
    /// <param name="order">渲染排序。</param>
    /// <param name="materialCategory">材质用途分类，默认用于桌台表现。</param>
    /// <returns>创建的网格渲染器。</returns>
    private static MeshRenderer Mesh(string name,Transform parent,Mesh mesh,Vector3 scale,Color color,int order,string materialCategory="Table")
    {
        var go=Node(name,parent); go.transform.localScale=scale; go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>(); renderer.sortingOrder=order;
        string path=MaterialContent+materialCategory+"/MultiBall_"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) { material=new Material(_tint); AssetDatabase.CreateAsset(material,path); }
        material.SetColor("_Color",color); EditorUtility.SetDirty(material); renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; return renderer;
    }
    /// <summary>制作动态几何网格模板。</summary>
    private static GeometryGraphic Graphic(string name,Transform parent,int order)
    {
        var go=Node(name,parent); var filter=go.AddComponent<MeshFilter>();
        var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=_tint; renderer.sortingOrder=order;
        var graphic=go.AddComponent<GeometryGraphic>(); Bind(graphic,"_filter",filter); return graphic;
    }
    /// <summary>通过 SerializedObject 持久化组件引用。</summary>
    private static void Bind(Component component,string field,Object value)
    {
        var serialized=new SerializedObject(component); var property=serialized.FindProperty(field);
        if(property==null) throw new InvalidOperationException(component.GetType().Name+" 缺少字段 "+field);
        property.objectReferenceValue=value; serialized.ApplyModifiedProperties();
    }
    /// <summary>更新说明文本，保留现有生成绑定。</summary>
    private static void UpdateInstructions()
    {
        const string path="Assets/AssetRaw/UI/GameMainUI.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var text in root.GetComponentsInChildren<Text>(true))
                if(text.name=="Instructions") text.text="白球推动黑球入洞 · 按住左键蓄力 · 松开出杆 · 右键取消";
            SaveHud(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    /// <summary>保存本任务涉及的说明 Prefab。</summary>
    private static void SaveHud(GameObject root,string path)
    { PrefabUtility.SaveAsPrefabAsset(root,path,out bool saved); if(!saved) throw new InvalidOperationException("HUD 保存失败。"); }
}
