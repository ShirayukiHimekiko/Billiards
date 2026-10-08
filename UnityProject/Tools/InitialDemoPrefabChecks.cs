using System;
using System.Collections.Generic;
using GameLogic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InitialDemoPrefabChecks
{
    public static object Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("请在编辑模式检查 Prefab。");
        var passed=new List<string>();string[] names={"StartUI","GameMainUI","SettingsUI"};int[] counts={6,12,13};
        for(int i=0;i<names.Length;i++)
        {
            var root=PrefabUtility.LoadPrefabContents("Assets/AssetRaw/UI/"+names[i]+".prefab");
            try
            {
                if(root.GetComponent<Canvas>()==null || root.GetComponent<GraphicRaycaster>()==null)throw new Exception(names[i]+" 缺少画布或射线组件");
                var bindings=new SerializedObject(root.GetComponent<UIBindComponent>()).FindProperty("m_components");
                if(bindings.arraySize!=counts[i])throw new Exception(names[i]+" 绑定数量错误");
                for(int index=0;index<bindings.arraySize;index++)if(bindings.GetArrayElementAtIndex(index).objectReferenceValue==null)throw new Exception(names[i]+" 空绑定 "+index);
                foreach(var node in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject)>0)throw new Exception(names[i]+" Missing Script");
                foreach(var graphic in root.GetComponentsInChildren<GeometryGraphic>(true))if(graphic.GetComponent<CanvasRenderer>()==null)throw new Exception("几何图形没有 CanvasRenderer");
                foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if(script is not BoardView && script is not BallBase)continue;
                    var iterator=new SerializedObject(script).GetIterator();
                    while(iterator.NextVisible(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference && iterator.objectReferenceValue==null)throw new Exception(script.GetType().Name+" 空引用 "+iterator.name);
                }
                passed.Add(names[i]+"：画布、绑定、业务引用和脚本完整");
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        return new {passed=passed.ToArray(),count=passed.Count};
    }
}
