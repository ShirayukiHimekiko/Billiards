using System;
using System.Collections.Generic;
using GameLogic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>仅通过 Unity Pipeline run_script 执行的初版 Prefab 制作工具，不进入运行时程序集。</summary>
public static class InitialDemoAuthoring
{
    private const string Art="Assets/AssetArt/UIRaw/Raw/";
    private static Font _font;
    private static readonly Color Cream=new Color(0.94f,0.90f,0.78f);
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式制作。");
        foreach(string name in new[]{"StartUI","GameMainUI","SettingsUI"})
            if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetRaw/UI/"+name+".prefab")!=null)
                throw new InvalidOperationException("Prefab 已存在，先检查而不是覆盖："+name);
        _font=AssetDatabase.LoadAssetAtPath<Font>("Assets/AssetArt/Fonts/NotoSansCJK/NotoSansCJKsc-Regular.otf");
        if(_font==null) throw new InvalidOperationException("中文字体尚未导入。");
        Scene preview=EditorSceneManager.NewPreviewScene();
        try
        {
            Save(BuildStart(),"StartUI",preview);
            Save(BuildMain(),"GameMainUI",preview);
            Save(BuildSettings(),"SettingsUI",preview);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        return new { prefabs=new[]{"StartUI","GameMainUI","SettingsUI"},saved=true };
    }
    private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"制作数学桌球初版");
        var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0.5f,0.5f);
        rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);go.layer=5;return rect;
    }
    private static RectTransform Root(string name)
    {
        var root=Rect(name,null,0,0,0,0);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;
        Add<Canvas>(root.gameObject);Add<GraphicRaycaster>(root.gameObject);
        return root;
    }
    private static T Add<T>(GameObject go) where T:Component => Undo.AddComponent<T>(go);
    private static Image Image(string name,Transform parent,float x,float y,float w,float h,string sprite,Color? color=null)
    {
        var rect=Rect(name,parent,x,y,w,h);var image=Add<Image>(rect.gameObject);
        image.color=color??Color.white;image.raycastTarget=false;
        if(!string.IsNullOrEmpty(sprite))
        {
            image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+sprite+".png");
            if(image.sprite==null) throw new InvalidOperationException("缺少 Sprite："+sprite);
        }
        return image;
    }
    private static Text Label(string name,Transform parent,string text,float x,float y,float w,float h,int size=28)
    {
        var rect=Rect(name,parent,x,y,w,h);var label=Add<Text>(rect.gameObject);
        label.font=_font;label.fontSize=size;label.text=text;label.color=Cream;
        label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
        label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
        return label;
    }
    private static Button Button(string name,Transform parent,string label,float x,float y,float w=300,float h=64)
    {
        var image=Image(name,parent,x,y,w,h,"Common/button_normal");image.raycastTarget=true;
        var button=Add<Button>(image.gameObject);button.targetGraphic=image;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,0.95f,0.75f);
        colors.pressedColor=new Color(0.68f,0.65f,0.55f);colors.disabledColor=new Color(0.45f,0.45f,0.45f,0.65f);button.colors=colors;
        var navigation=button.navigation;navigation.mode=Navigation.Mode.None;button.navigation=navigation;
        Label("Label",image.transform,label,0,0,w-24,h-8,26);return button;
    }
    private static Slider Slider(string name,Transform parent,float x,float y,float width,bool volume)
    {
        var track=Image(name,parent,x,y,width,20,volume ? "Settings/settings_slider_track" : "HUD/power_frame");
        track.raycastTarget=volume;
        var fillArea=Rect("FillArea",track.transform,0,0,width-14,10);
        var fill=Image("Fill",fillArea,0,0,width-14,10,volume ? "Settings/settings_slider_fill" : "HUD/power_fill");
        fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;
        fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
        var handleArea=Rect("HandleArea",track.transform,0,0,width-14,32);
        var handle=Image("Handle",handleArea,0,0,28,28,"Settings/settings_slider_handle");handle.raycastTarget=volume;
        var slider=Add<Slider>(track.gameObject);slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;
        slider.targetGraphic=handle;slider.minValue=volume ? 1 : 0;slider.maxValue=volume ? 100 : 1;
        slider.wholeNumbers=volume;slider.value=volume ? 100 : 0;
        var navigation=slider.navigation;navigation.mode=Navigation.Mode.None;slider.navigation=navigation;
        if(!volume) handle.gameObject.SetActive(false);return slider;
    }
    private static void Bind(RectTransform root,params Component[] references)
    {
        var bind=Add<UIBindComponent>(root.gameObject);foreach(var reference in references) bind.AddComponent(reference);
        EditorUtility.SetDirty(bind);
    }
    private static void SetReference(Component target,string field,Object value)
    {
        var serialized=new SerializedObject(target);var property=serialized.FindProperty(field);
        if(property==null) throw new InvalidOperationException(target.GetType().Name+" 缺少字段 "+field);
        property.objectReferenceValue=value;serialized.ApplyModifiedProperties();
    }
    private static RectTransform BuildStart()
    {
        var root=Root("StartUI");var background=Image("Background",root,0,0,0,0,"Menu/start_menu_background");
        background.rectTransform.anchorMin=Vector2.zero;background.rectTransform.anchorMax=Vector2.one;
        background.rectTransform.offsetMin=background.rectTransform.offsetMax=Vector2.zero;
        Label("Title",root,"数学桌球",-540,280,500,100,66);
        Label("Subtitle",root,"MATH BILLIARDS",-540,205,500,42,23);
        var start=Button("m_btn_Start",root,"开始游戏",-540,100);
        var settings=Button("m_btn_Settings",root,"设置",-540,18);
        var save=Button("m_btn_Save",root,"存档",-540,-64);
        var credits=Button("m_btn_Credits",root,"制作详情",-540,-146);
        var exit=Button("m_btn_Exit",root,"退出",-540,-228);
        var message=Label("m_text_Message",root,"",0,-430,1400,70,22);
        Label("Version",root,"单关初版  ·  v0.1",570,-430,340,40,20);
        Bind(root,start,settings,save,credits,exit,message);return root;
    }
    private static RectTransform BuildMain()
    {
        var root=Root("GameMainUI");var background=Image("Background",root,0,0,0,0,null,new Color(0.035f,0.065f,0.12f));
        background.rectTransform.anchorMin=Vector2.zero;background.rectTransform.anchorMax=Vector2.one;
        background.rectTransform.offsetMin=background.rectTransform.offsetMax=Vector2.zero;
        Label("Title",root,"数学桌球",-240,440,720,65,42);
        Image("Sidebar",root,520,-15,420,766,"Common/ui_panel").type=UnityEngine.UI.Image.Type.Sliced;
        var shots=Label("m_text_Shots",root,"剩余机会",520,310,360,60,30);
        var trend=Label("m_text_Trend",root,"",520,200,350,90,24);
        var powerText=Label("m_text_Power",root,"力量  0%",520,102,330,45,25);
        var power=Slider("m_slider_Power",root,520,55,310,false);
        var status=Label("m_text_Status",root,"正在准备关卡…",520,-48,350,100,23);
        var hints=Button("m_btn_Hints",root,"目标提示",520,-157,310,58);
        var pause=Button("m_btn_Pause",root,"暂停",520,-227,145,58);pause.GetComponent<RectTransform>().anchoredPosition=new Vector2(437,-227);
        var restart=Button("m_btn_Restart",root,"重新开始",603,-227,145,58);
        var menu=Button("m_btn_Menu",root,"返回菜单",437,-298,145,58);
        var exit=Button("m_btn_Exit",root,"退出",603,-298,145,58);
        Label("Instructions",root,"鼠标瞄准  ·  左键按住蓄力，松开击球  ·  右键取消  ·  Esc 暂停",-240,-438,980,55,22);
        Bind(root,root,status,shots,trend,power,powerText,restart,pause,menu,exit,hints,pause.GetComponentInChildren<Text>());
        return root;
    }
    private static RectTransform BuildSettings()
    {
        var root=Root("SettingsUI");var dim=Image("Dim",root,0,0,0,0,null,new Color(0,0,0,0.65f));dim.raycastTarget=true;
        dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;
        dim.rectTransform.offsetMin=dim.rectTransform.offsetMax=Vector2.zero;
        var panel=Image("Panel",root,0,0,880,660,"Settings/settings_panel");
        Label("Title",panel.transform,"设置",0,235,500,70,42);
        var close=Button("m_btn_Close",panel.transform,"关闭",0,-235,190,56);
        var references=new List<Component>{close};
        string[] titles={"背景音乐","游戏音效","界面音效","声音"};
        string[] icons={"settings_icon_music","settings_icon_sound","settings_icon_ui_sound","settings_icon_voice_v2"};
        for(int i=0;i<4;i++)
        {
            float y=130-85*i;
            var image=Image("m_btn_Audio"+i,panel.transform,-280,y,70,70,"Settings/"+icons[i]);image.raycastTarget=true;
            var button=Add<Button>(image.gameObject);button.targetGraphic=image;
            Label("AudioLabel"+i,panel.transform,titles[i],-140,y,170,55,25);
            var slider=Slider("m_slider_Audio"+i,panel.transform,80,y,240,true);
            var value=Label("m_text_Audio"+i,panel.transform,"开  100%",268,y,130,50,22);
            references.Add(button);references.Add(slider);references.Add(value);
        }
        Bind(root,references.ToArray());return root;
    }
    private static void Save(RectTransform root,string name,Scene preview)
    {
        SceneManager.MoveGameObjectToScene(root.gameObject,preview);
        string path="Assets/AssetRaw/UI/"+name+".prefab";
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,path,out bool success);
        if(!success) throw new InvalidOperationException("Prefab 保存失败："+path);
        Object.DestroyImmediate(root.gameObject);
    }
}

