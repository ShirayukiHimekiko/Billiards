using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameLogic;
using UnityEngine;
using UnityEngine.UI;

public static class InitialDemoExitCheck
{
    public static async Task<object> Main()
    {
        for(int frame=0;frame<600 && GameObject.Find("StartUI")==null;frame++)await UniTask.NextFrame();
        var menu=GameObject.Find("StartUI");if(menu==null)throw new Exception("开始菜单未出现");
        menu.GetComponent<UIBindComponent>().GetComponent<Button>(4).onClick.Invoke();
        return new {menuExitInvoked=true};
    }
}
