// 仅通过已连接到本项目的 Unity Pipeline eval_file 执行。
// 使用预览场景隔离作者对象，不保存用户场景；绑定由现有生成器维护。
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("请在编辑模式生成选关资源。");
var projectPath = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var previousSelection = UnityEditor.Selection.activeObject;
var canvasObject = new UnityEngine.GameObject("LevelSelectAuthoringCanvas", typeof(UnityEngine.RectTransform), typeof(UnityEngine.Canvas), typeof(UnityEngine.UI.CanvasScaler));
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, preview);
var canvas = canvasObject.GetComponent<UnityEngine.Canvas>();
canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
var baker = UnityEngine.ScriptableObject.CreateInstance<HtmlToUGUI.HtmlToUGUIBaker>();
bool editingAssets = false;
try
{
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var type = typeof(HtmlToUGUI.HtmlToUGUIBaker);
    type.GetField("targetCanvas", flags).SetValue(baker, canvas);
    type.GetField("autoFindCanvas", flags).SetValue(baker, false);
    type.GetField("useLegacyText", flags).SetValue(baker, true);
    type.GetField("useScriptGeneratorNaming", flags).SetValue(baker, false);
    type.GetField("selectGeneratedRoot", flags).SetValue(baker, true);
    type.GetField("rawJsonString", flags).SetValue(baker, System.IO.File.ReadAllText(System.IO.Path.Combine(projectPath,"Tools/UI/LevelSelectUI.json")));
    var inputMode = type.GetField("currentMode", flags);
    inputMode.SetValue(baker, System.Enum.Parse(inputMode.FieldType,"RawString"));
    type.GetMethod("ExecuteBake",flags).Invoke(baker,null);
    var root = UnityEditor.Selection.activeGameObject;
    if (root == null || root.name != "LevelSelectUI") throw new System.InvalidOperationException("选关布局烘焙没有返回预期根对象。");
    root.AddComponent<UnityEngine.Canvas>().overrideSorting = true;
    root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/AssetRaw/UI/StartUI.prefab");
    var font = source.GetComponentInChildren<UnityEngine.UI.Text>(true).font;
    foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
    {
        text.font = font;
        text.raycastTarget = false;
        text.horizontalOverflow = UnityEngine.HorizontalWrapMode.Wrap;
        // 中文字体的行高可能超过 CSS 固定框高，允许完整绘制单行文字。
        text.verticalOverflow = UnityEngine.VerticalWrapMode.Overflow;
    }

    var scroll = root.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
    var originalContent = scroll.content;
    var content = originalContent.Find("m_tf_Content") as UnityEngine.RectTransform;
    content.SetParent(scroll.viewport,false);
    UnityEngine.Object.DestroyImmediate(originalContent.gameObject);
    scroll.content = content;
    scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
    content.anchorMin = new UnityEngine.Vector2(0,1);
    content.anchorMax = new UnityEngine.Vector2(1,1);
    content.pivot = new UnityEngine.Vector2(.5f,1);
    content.anchoredPosition = UnityEngine.Vector2.zero;
    content.sizeDelta = new UnityEngine.Vector2(0,1032);
    var grid = content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
    grid.cellSize = new UnityEngine.Vector2(510,230);
    grid.spacing = new UnityEngine.Vector2(24,24);
    grid.padding = new UnityEngine.RectOffset(16,16,8,8);
    grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
    grid.constraintCount = 3;
    var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
    fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

    var card = content.Find("m_item_LevelItemWidget").gameObject;
    var generatedPath = "Assets/GameScripts/HotFix/GameLogic/UI/Gen";
    // 卡片是窗口引用的模板，保存到 AssetArt；独立加载的窗口仍放在 AssetRaw。
    if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/AssetArt/UI"))
        UnityEditor.AssetDatabase.CreateFolder("Assets/AssetArt", "UI");
    if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/AssetArt/UI/Widgets"))
        UnityEditor.AssetDatabase.CreateFolder("Assets/AssetArt/UI", "Widgets");
    UnityEditor.AssetDatabase.StartAssetEditing();
    editingAssets = true;
    UnityEditor.Selection.activeGameObject = card;
    TEngine.Editor.UI.ScriptGenerator.GenerateUIComponentScript();
    card.GetComponent<GameLogic.UIBindComponent>().className = "LevelItemWidget";
    card.GetComponent<GameLogic.UIBindComponent>().uiType = "UIWidget";
    if (!TEngine.Editor.UI.ScriptGenerator.GenerateCSharpScript(true,false,true,generatedPath,"LevelItemWidget","UIWidget"))
        throw new System.InvalidOperationException("关卡卡片绑定生成失败。");
    card.SetActive(false);
    UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(card,"Assets/AssetArt/UI/Widgets/LevelItemWidget.prefab",UnityEditor.InteractionMode.AutomatedAction);
    UnityEditor.Selection.activeGameObject = root;
    TEngine.Editor.UI.ScriptGenerator.GenerateUIComponentScript();
    root.GetComponent<GameLogic.UIBindComponent>().className = "LevelSelectUI";
    root.GetComponent<GameLogic.UIBindComponent>().uiType = "UIWindow";
    if (!TEngine.Editor.UI.ScriptGenerator.GenerateCSharpScript(true,false,true,generatedPath,"LevelSelectUI","UIWindow"))
        throw new System.InvalidOperationException("选关窗口绑定生成失败。");
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,"Assets/AssetRaw/UI/LevelSelectUI.prefab");

    // 只更改本任务的返回入口文案，不保存任何打开的场景。
    var main = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/AssetRaw/UI/GameMainUI.prefab");
    try
    {
        foreach (var button in main.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            if (button.name == "m_btn_Menu") button.GetComponentInChildren<UnityEngine.UI.Text>(true).text = "选关";
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(main,"Assets/AssetRaw/UI/GameMainUI.prefab");
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(main); }

    // 业务代码维护在 GameLogic；重新制作资源只更新 Prefab 和生成绑定。
    return "LevelSelectUI.prefab、LevelItemWidget.prefab、窗口与卡片生成绑定已保存；没有运行游戏或验证。";
}
finally
{
    UnityEditor.Selection.activeObject = previousSelection;
    UnityEngine.Object.DestroyImmediate(baker);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
    if (editingAssets) UnityEditor.AssetDatabase.StopAssetEditing();
}
