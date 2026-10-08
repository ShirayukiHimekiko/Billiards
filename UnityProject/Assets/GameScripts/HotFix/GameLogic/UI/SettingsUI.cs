using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 管理四类音频的开关和音量。
    /// </summary>
    [Window(UILayer.Top, "SettingsUI", fullScreen: false)]
    public sealed partial class SettingsUI : UIWindow
    {
        /// <summary>
        /// 打开设置时刷新当前音频配置。
        /// </summary>
        protected override void OnRefresh()
        {
            RefreshAudioSettings();
        }

        /// <summary>
        /// 刷新四类音频的开关、音量和可交互状态。
        /// </summary>
        private void RefreshAudioSettings()
        {
            var audio = GameModule.Audio;
            RefreshAudioControl(m_btn_Music, m_slider_Music, m_text_Music, audio.MusicEnable, audio.MusicVolume);
            RefreshAudioControl(m_btn_Sound, m_slider_Sound, m_text_Sound, audio.SoundEnable, audio.SoundVolume);
            RefreshAudioControl(m_btn_UISound, m_slider_UISound, m_text_UISound, audio.UISoundEnable, audio.UISoundVolume);
            RefreshAudioControl(m_btn_Voice, m_slider_Voice, m_text_Voice, audio.VoiceEnable, audio.VoiceVolume);
        }

        /// <summary>
        /// 应用一类音频的显示值和交互状态。
        /// </summary>
        /// <param name="icon">音频开关按钮。</param>
        /// <param name="slider">音量滑条。</param>
        /// <param name="label">开关及音量文字。</param>
        /// <param name="enabled">音频是否启用。</param>
        /// <param name="volume">零到一的音量。</param>
        private static void RefreshAudioControl(
            Button icon,
            Slider slider,
            Text label,
            bool enabled,
            float volume)
        {
            int value = Mathf.Clamp(Mathf.RoundToInt(volume * 100), 1, 100);
            slider.SetValueWithoutNotify(value);
            slider.interactable = enabled;
            icon.image.color = enabled ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.75f);
            label.text = $"{(enabled ? "开" : "关")}   {value}%";
        }

        /// <summary>
        /// 关闭当前设置窗口。
        /// </summary>
        private partial void OnClick_CloseBtn()
        {
            GameModule.UI.CloseUI<SettingsUI>();
        }

        /// <summary>
        /// 切换背景音乐开关。
        /// </summary>
        private partial void OnClick_MusicBtn()
        {
            GameModule.Audio.MusicEnable = !GameModule.Audio.MusicEnable;
            RefreshAudioSettings();
        }

        /// <summary>
        /// 切换游戏音效开关。
        /// </summary>
        private partial void OnClick_SoundBtn()
        {
            GameModule.Audio.SoundEnable = !GameModule.Audio.SoundEnable;
            RefreshAudioSettings();
        }

        /// <summary>
        /// 切换界面音效开关。
        /// </summary>
        private partial void OnClick_UISoundBtn()
        {
            GameModule.Audio.UISoundEnable = !GameModule.Audio.UISoundEnable;
            RefreshAudioSettings();
        }

        /// <summary>
        /// 切换声音开关。
        /// </summary>
        private partial void OnClick_VoiceBtn()
        {
            GameModule.Audio.VoiceEnable = !GameModule.Audio.VoiceEnable;
            RefreshAudioSettings();
        }

        /// <summary>
        /// 应用背景音乐音量并刷新显示。
        /// </summary>
        /// <param name="value">一到一百的音量。</param>
        private partial void OnSlider_MusicChange(float value)
        {
            GameModule.Audio.MusicVolume = value / 100;
            RefreshAudioSettings();
        }

        /// <summary>
        /// 应用游戏音效音量并刷新显示。
        /// </summary>
        /// <param name="value">一到一百的音量。</param>
        private partial void OnSlider_SoundChange(float value)
        {
            GameModule.Audio.SoundVolume = value / 100;
            RefreshAudioSettings();
        }

        /// <summary>
        /// 应用界面音效音量并刷新显示。
        /// </summary>
        /// <param name="value">一到一百的音量。</param>
        private partial void OnSlider_UISoundChange(float value)
        {
            GameModule.Audio.UISoundVolume = value / 100;
            RefreshAudioSettings();
        }

        /// <summary>
        /// 应用声音音量并刷新显示。
        /// </summary>
        /// <param name="value">一到一百的音量。</param>
        private partial void OnSlider_VoiceChange(float value)
        {
            GameModule.Audio.VoiceVolume = value / 100;
            RefreshAudioSettings();
        }

        /// <summary>
        /// 释放生成绑定所注册的音频设置回调。
        /// </summary>
        protected override void OnDestroy()
        {
            // 异步加载完成前关闭窗口时，生成绑定尚未初始化。
            if (m_bindComponent == null)
            {
                return;
            }

            m_btn_Close.onClick.RemoveListener(OnClick_CloseBtn);
            m_btn_Music.onClick.RemoveListener(OnClick_MusicBtn);
            m_btn_Sound.onClick.RemoveListener(OnClick_SoundBtn);
            m_btn_UISound.onClick.RemoveListener(OnClick_UISoundBtn);
            m_btn_Voice.onClick.RemoveListener(OnClick_VoiceBtn);
            m_slider_Music.onValueChanged.RemoveListener(OnSlider_MusicChange);
            m_slider_Sound.onValueChanged.RemoveListener(OnSlider_SoundChange);
            m_slider_UISound.onValueChanged.RemoveListener(OnSlider_UISoundChange);
            m_slider_Voice.onValueChanged.RemoveListener(OnSlider_VoiceChange);
        }
    }
}
