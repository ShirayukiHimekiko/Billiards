using System;
using System.Collections.Generic;
using GameLogic;
using UnityEngine;
using UnityEngine.UI;

public static class InitialDemoSettingsChecks
{
    public static object Main()
    {
        var root=GameObject.Find("SettingsUI");if(root==null)throw new Exception("请打开设置。");
        var bind=root.GetComponent<UIBindComponent>();var audio=GameModule.Audio;
        float[] volumes={audio.MusicVolume,audio.SoundVolume,audio.UISoundVolume,audio.VoiceVolume};
        bool[] enabled={audio.MusicEnable,audio.SoundEnable,audio.UISoundEnable,audio.VoiceEnable};
        var passed=new List<string>();string[] names={"Music","Sound","UISound","Voice（声音）"};
        try
        {
            for(int i=0;i<4;i++)
            {
                var icon=bind.GetComponent<Button>(1+3*i);var slider=bind.GetComponent<Slider>(2+3*i);
                if(!Enabled(i))icon.onClick.Invoke();
                int value=37+10*i;slider.value=value;
                Check(Mathf.Abs(Volume(i)-value/100f)<.0001f,names[i]+" 滑条改变模块音量",passed);
                icon.onClick.Invoke();Check(!Enabled(i) && !slider.interactable && Mathf.Abs(Volume(i)-value/100f)<.0001f,names[i]+" 关闭且保留音量",passed);
                icon.onClick.Invoke();Check(Enabled(i) && slider.interactable && Mathf.Abs(Volume(i)-value/100f)<.0001f,names[i]+" 再次打开恢复控制",passed);
            }
            return new {count=passed.Count,passed=passed.ToArray()};
        }
        finally
        {
            for(int i=0;i<4;i++)
            {
                bind.GetComponent<Slider>(2+3*i).value=volumes[i]*100;
                if(Enabled(i)!=enabled[i])bind.GetComponent<Button>(1+3*i).onClick.Invoke();
            }
        }
    }
    private static bool Enabled(int i)=>i==0?GameModule.Audio.MusicEnable:i==1?GameModule.Audio.SoundEnable:i==2?GameModule.Audio.UISoundEnable:GameModule.Audio.VoiceEnable;
    private static float Volume(int i)=>i==0?GameModule.Audio.MusicVolume:i==1?GameModule.Audio.SoundVolume:i==2?GameModule.Audio.UISoundVolume:GameModule.Audio.VoiceVolume;
    private static void Check(bool ok,string name,List<string> passed){if(!ok)throw new Exception(name);passed.Add(name);}
}
