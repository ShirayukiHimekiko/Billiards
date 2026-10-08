using System;
using GameLogic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>通过 Pipeline run_script 制作并持久化 2D 桌球资源。</summary>
public static class TableAuthoring
{
    private const string Art = "Assets/AssetArt/UIRaw/Raw/";
    private const string Content = "Assets/AssetRaw/Actor/Billiards/";
    /// <summary>保存由实体 Prefab 引用的网格，不作为独立加载入口。</summary>
    private const string MeshContent = "Assets/AssetArt/Meshes/Primitives/";
    /// <summary>保存由 Renderer 引用的材质。</summary>
    private const string MaterialContent = "Assets/AssetArt/Materials/";
    private static Mesh _quad, _disk;
    private static Material _tint, _felt, _ballMaterial;

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("制作需要编辑模式。");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/main.unity" || scene.isDirty ||
            PrefabStageUtility.GetCurrentPrefabStage() != null) throw new Exception("main 场景需已保存且没有 Prefab Stage。");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Content + "BilliardsTable.prefab") != null)
            throw new Exception("桌球 Prefab 已存在，不能重复制作。");
        Folder("Assets/AssetRaw/Actor", "Billiards");
        Folder("Assets/AssetArt", "Meshes");
        Folder("Assets/AssetArt/Meshes", "Primitives");
        Folder("Assets/AssetArt", "Materials");
        foreach (string category in new[] { "Common", "Table", "Balls", "Aiming" })
            Folder("Assets/AssetArt/Materials", category);
        _quad = MakeQuad();
        _disk = MakeDisk();
        AssetDatabase.CreateAsset(_quad, MeshContent + "TableQuad.asset");
        AssetDatabase.CreateAsset(_disk, MeshContent + "TableDisk.asset");
        _tint = Material("WorldTint", "Billiards/WorldTint", "Common");
        _felt = Material("Felt", "Billiards/WorldTint", "Table");
        _felt.SetFloat("_Felt", 1); EditorUtility.SetDirty(_felt);
        _ballMaterial = Material("RollingBall", "Billiards/RollingBall", "Balls");
        // 球面着色器使用完整正方形 UV，单独设置白球贴图的网格导入方式。
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "Balls/ball_normal.png");
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings); importer.SaveAndReimport();

        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject table = BuildTable();
            SceneManager.MoveGameObjectToScene(table, preview);
            PrefabUtility.SaveAsPrefabAsset(table, Content + "BilliardsTable.prefab", out bool success);
            Object.DestroyImmediate(table);
            if (!success) throw new Exception("桌球 Prefab 保存失败。");
            UpdateHud();
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        Camera camera = Camera.main;
        Undo.RecordObjects(new Object[] { camera, camera.transform }, "设置桌球正交相机");
        camera.orthographic = true; camera.orthographicSize = 6.8f;
        camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
        camera.backgroundColor = new Color(.025f, .04f, .04f);
        camera.cullingMask = ~(1 << 5);
        EditorUtility.SetDirty(camera); EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("main 场景保存失败。");
        AssetDatabase.SaveAssets();
        return new { table = Content + "BilliardsTable.prefab", hud = "Assets/AssetRaw/UI/GameMainUI.prefab", scene = scene.path };
    }

    private static void Folder(string parent, string name)
    { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
    /// <summary>创建材质并保存到调用方指定的用途分类目录。</summary>
    /// <param name="name">材质资源名称。</param>
    /// <param name="shader">Shader 的内部查找名称。</param>
    /// <param name="category">Materials 下的用途分类。</param>
    /// <returns>已保存的材质。</returns>
    private static Material Material(string name, string shader, string category)
    {
        Shader source = Shader.Find(shader);
        if (source == null || !source.isSupported) throw new Exception("Shader 不可用：" + shader);
        var material = new Material(source) { name = name };
        AssetDatabase.CreateAsset(material, MaterialContent + category + "/" + name + ".mat");
        return material;
    }
    private static GameObject Node(string name, Transform parent, Vector3 position)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "制作 2D 桌球");
        go.transform.SetParent(parent, false); go.transform.localPosition = position;
        return go;
    }
    private static Mesh MakeQuad()
    {
        var mesh = new Mesh { name = "TableQuad" };
        mesh.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(-.5f,.5f,0), new Vector3(.5f,.5f,0), new Vector3(.5f,-.5f,0) };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        mesh.triangles = new[] { 0,1,2,0,2,3 }; mesh.RecalculateBounds();
        return mesh;
    }
    private static Mesh MakeDisk()
    {
        var mesh = new Mesh { name = "TableDisk" };
        var vertices = new Vector3[65]; var colors = new Color[65]; var indices = new int[64 * 3];
        colors[0] = Color.white;
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2 / 64;
            vertices[i+1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * .5f;
            colors[i+1] = Color.white;
            indices[i*3] = 0; indices[i*3+1] = i+1; indices[i*3+2] = (i+1)%64+1;
        }
        mesh.vertices = vertices; mesh.colors = colors; mesh.triangles = indices; mesh.RecalculateBounds();
        return mesh;
    }
    /// <summary>制作基础形状，并将局部颜色材质保存到指定用途分类。</summary>
    /// <param name="name">形状与材质的名称。</param>
    /// <param name="parent">形状的父节点。</param>
    /// <param name="position">局部位置。</param>
    /// <param name="size">局部尺寸。</param>
    /// <param name="color">局部颜色。</param>
    /// <param name="order">渲染排序。</param>
    /// <param name="disk">是否使用圆盘网格。</param>
    /// <param name="material">用于复制的源材质；为空时使用公共材质。</param>
    /// <param name="materialCategory">材质用途分类，默认用于桌台表现。</param>
    /// <returns>创建的形状节点。</returns>
    private static Transform Shape(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order, bool disk = false, Material material = null, string materialCategory = "Table")
    {
        var go = Node(name, parent, position); go.transform.localScale = new Vector3(size.x, size.y, 1);
        Undo.AddComponent<MeshFilter>(go).sharedMesh = disk ? _disk : _quad;
        var renderer = Undo.AddComponent<MeshRenderer>(go); renderer.sharedMaterial = material ?? _tint;
        renderer.sortingOrder = order; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // 保存颜色为材质，PropertyBlock 不会序列化。
        var tint = new Material(renderer.sharedMaterial) { name = name + "Tint" };
        tint.SetColor("_Color",color);
        AssetDatabase.CreateAsset(tint, MaterialContent + materialCategory + "/" + name + ".mat");
        renderer.sharedMaterial = tint;
        return go.transform;
    }
    private static GeometryGraphic Geometry(string name, Transform parent, Color color, int order)
    {
        var go = Node(name,parent,Vector3.zero);
        var filter = Undo.AddComponent<MeshFilter>(go);
        var renderer = Undo.AddComponent<MeshRenderer>(go); renderer.sharedMaterial = _tint; renderer.sortingOrder = order;
        var geometry = Undo.AddComponent<GeometryGraphic>(go);
        Bind(geometry,"_filter",filter);
        var serialized = new SerializedObject(geometry); serialized.FindProperty("_color").colorValue = color;
        serialized.ApplyModifiedProperties(); return geometry;
    }
    private static LineRenderer Line(string name, Transform parent, Color color, float width, int order)
    {
        var go = Node(name,parent,Vector3.zero); var line = Undo.AddComponent<LineRenderer>(go);
        line.sharedMaterial = _tint; line.useWorldSpace = false; line.positionCount = 2;
        line.startColor = line.endColor = color; line.startWidth = line.endWidth = width;
        line.sortingOrder = order; line.numCapVertices = 5;
        return line;
    }
    private static void Bind(Component target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedProperties();
    }
    private static GameObject BuildTable()
    {
        var root = Node("BilliardsTable",null,new Vector3(-2.8f,0,0));
        var board = Undo.AddComponent<BoardView>(root);
        Shape("TableShadow",root.transform,new Vector2(.12f,-.24f),new Vector2(17.7f,9.7f),new Color(0,0,0,.45f),0);
        Shape("WoodFrame",root.transform,Vector2.zero,new Vector2(17.3f,9.3f),new Color(.27f,.14f,.075f),1);
        Shape("FrameInlay",root.transform,Vector2.zero,new Vector2(16.9f,8.9f),new Color(.65f,.44f,.21f),2);
        Shape("Cushion",root.transform,Vector2.zero,new Vector2(16.5f,8.5f),new Color(.035f,.18f,.12f),3);
        var felt = Shape("Felt",root.transform,Vector2.zero,new Vector2(16,8),new Color(.055f,.32f,.23f),4,false,_felt);
        for (int i=0;i<4;i++)
        {
            float x=-6+4*i;
            Shape("TopSight"+i,root.transform,new Vector2(x,4.43f),Vector2.one*.09f,new Color(.89f,.83f,.65f),5).localRotation=Quaternion.Euler(0,0,45);
            Shape("BottomSight"+i,root.transform,new Vector2(x,-4.43f),Vector2.one*.09f,new Color(.89f,.83f,.65f),5).localRotation=Quaternion.Euler(0,0,45);
        }
        var triangle = Geometry("Target",root.transform,new Color(1,.79f,.33f),8);
        var inner = Geometry("InnerHint",root.transform,new Color(.43f,.93f,.85f,.65f),7);
        var outer = Geometry("OuterHint",root.transform,new Color(.82f,.70f,1,.55f),7);
        var point = Shape("TargetPoint",root.transform,new Vector2(3,0),Vector2.one*.09f,new Color(1,.9f,.55f),9,true,materialCategory: "Aiming");
        var itemGo = Node("FlipItem",root.transform,new Vector2(-1.5f,-1.4f));
        var item = Undo.AddComponent<SpriteRenderer>(itemGo);
        item.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Props/toggle_active.png"); item.sortingOrder=10;
        var ballGo = Node("WhiteBall",root.transform,new Vector2(-5,0));
        var ball = Undo.AddComponent<WhiteBall>(ballGo);
        // WhiteBall 的 RequireComponent 已添加必需组件，避免制作时重复挂载。
        var collider = ballGo.GetComponent<CircleCollider2D>(); collider.isTrigger=true;collider.radius=.28f;
        Shape("BallShadow",ballGo.transform,Vector2.zero,Vector2.one,new Color(0,0,0,.3f),11,true,materialCategory: "Balls");
        var surface = Node("Surface",ballGo.transform,Vector3.zero);
        var sprite = Undo.AddComponent<SpriteRenderer>(surface);
        sprite.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Balls/ball_normal.png");
        sprite.sharedMaterial=_ballMaterial;sprite.sortingOrder=12;
        var aim=Line("Aim",root.transform,new Color(.76f,.96f,.89f,.4f),.023f,10);
        var cue=Line("Cue",root.transform,new Color(.78f,.55f,.28f),.085f,13);
        var runner=Undo.AddComponent<SessionRunner>(root);
        Bind(board,"_felt",felt);Bind(board,"_triangle",triangle);Bind(board,"_innerHint",inner);Bind(board,"_outerHint",outer);
        Bind(board,"_item",itemGo.transform);Bind(board,"_targetPoint",point);Bind(board,"_itemRenderer",item);
        Bind(board,"_aim",aim);Bind(board,"_cue",cue);Bind(board,"_ball",ball);Bind(board,"_runner",runner);
        return root;
    }
    private static void UpdateHud()
    {
        const string path="Assets/AssetRaw/UI/GameMainUI.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var bind=new SerializedObject(root.GetComponent<UIBindComponent>());
            bind.FindProperty("m_components").GetArrayElementAtIndex(0).objectReferenceValue=root.transform;
            bind.ApplyModifiedProperties();
            foreach(string name in new[]{"Background","TableFrame","Board"})
            {
                var child=root.transform.Find(name);
                if(child!=null)Object.DestroyImmediate(child.gameObject);
            }
            foreach(RectTransform child in root.GetComponentsInChildren<RectTransform>(true))
            {
                if(child.parent!=root.transform)continue;
                Vector2 pos=child.anchoredPosition;
                if(pos.x>300)child.anchoredPosition=new Vector2(610+(pos.x-520)*.85f,pos.y);
            }
            var sidebar=(RectTransform)root.transform.Find("Sidebar");
            sidebar.sizeDelta=new Vector2(340,766);
            foreach(Text text in root.GetComponentsInChildren<Text>(true))
            {
                if(text.transform.parent==root.transform && ((RectTransform)text.transform).anchoredPosition.x>400)
                    ((RectTransform)text.transform).sizeDelta=new Vector2(300,((RectTransform)text.transform).sizeDelta.y);
                if(text.name=="Title")
                { text.text="数学桌球";((RectTransform)text.transform).anchoredPosition=new Vector2(-195,376); }
                if(text.name=="Instructions")
                { text.text="瞄准右侧目标 · 按住左键蓄力 · 松开出杆 · 右键取消";((RectTransform)text.transform).anchoredPosition=new Vector2(-195,-375);text.fontSize=21; }
            }
            // 缩窄按钮以适配右侧 HUD。
            foreach(Button button in root.GetComponentsInChildren<Button>(true))
            {
                var rect=(RectTransform)button.transform;
                if(rect.sizeDelta.x>200)rect.sizeDelta=new Vector2(290,rect.sizeDelta.y);
                else rect.sizeDelta=new Vector2(128,rect.sizeDelta.y);
            }
            PrefabUtility.SaveAsPrefabAsset(root,path,out bool success);
            if(!success)throw new Exception("HUD 保存失败。");
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
