using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameLogic;
using UnityEngine;
using UnityEngine.UI;

public static class InitialDemoLifecycleChecks
{
    public static async Task<object> Main()
    {
        await GameManager.Instance.ReturnToMenuAsync();
        for(int frame=0;frame<600 && GameObject.Find("StartUI")==null;frame++)await UniTask.NextFrame();
        if(GameObject.Find("StartUI")==null)throw new Exception("正常入口未显示菜单");
        for(int i=0;i<3;i++)
        {
            GameObject.Find("StartUI").GetComponent<UIBindComponent>().GetComponent<Button>(0).onClick.Invoke();
            for(int frame=0;frame<300 && GameManager.Instance.Session==null;frame++)await UniTask.NextFrame();
            var session=GameManager.Instance.Session;if(session==null)throw new Exception("再次开始没有会话");
            if(UnityEngine.Object.FindObjectsByType<WhiteBall>().Length!=1)throw new Exception("白球重复生成");
            var bind=GameObject.Find("MainUI").GetComponent<UIBindComponent>();
            bind.GetComponent<Button>(7).onClick.Invoke();if(session.State!=SessionState.Paused)throw new Exception("暂停按钮无效");
            bind.GetComponent<Button>(7).onClick.Invoke();if(session.State!=SessionState.Ready)throw new Exception("继续按钮无效");
            var board=UnityEngine.Object.FindAnyObjectByType<BoardView>();
            bool visible=board.transform.Find("InnerHint").gameObject.activeSelf;
            bind.GetComponent<Button>(10).onClick.Invoke();
            if(board.transform.Find("InnerHint").gameObject.activeSelf==visible)throw new Exception("目标提示按钮无效");
            bind.GetComponent<Button>(6).onClick.Invoke();if(session.ShotsLeft!=3)throw new Exception("重开按钮无效");
            bind.GetComponent<Button>(8).onClick.Invoke();
            for(int frame=0;frame<300 && GameObject.Find("StartUI")==null;frame++)await UniTask.NextFrame();
            await UniTask.DelayFrame(3);
            if(GameManager.Instance.Session!=null || UnityEngine.Object.FindObjectsByType<WhiteBall>().Length!=0 || GameObject.Find("MainUI")!=null)throw new Exception("返回菜单未清理");
        }
        GameManager.Instance.StartGameAsync().Forget();await GameManager.Instance.ReturnToMenuAsync();await UniTask.DelayFrame(10);
        if(GameManager.Instance.Session!=null || GameObject.Find("MainUI")!=null || GameObject.Find("StartUI")==null)throw new Exception("开始后立即返回残留主界面");
        return new {roundTrips=3,pause=true,restart=true,hints=true,sessionCleanup=true,immediateReturn=true};
    }
}
