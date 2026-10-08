using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 游戏开始菜单的业务逻辑，控件绑定由项目生成器维护。
    /// </summary>
    [Window(UILayer.UI, "StartUI", fullScreen: true)]
    public sealed partial class StartUI : UIWindow
    {
        /// <summary>
        /// 初始化尚未开放的菜单入口。
        /// </summary>
        protected override void OnCreate()
        {
            m_btn_Save.interactable = false;
            m_btn_Credits.interactable = false;
        }

        /// <summary>
        /// 显示菜单提示。
        /// </summary>
        /// <param name="message">提示内容。</param>
        public void SetMessage(string message)
        {
            m_text_Message.text = message;
        }

        /// <summary>
        /// 打开选关界面。
        /// </summary>
        private partial void OnClick_StartBtn()
        {
            GameManager.Instance.ShowLevelSelectAsync().Forget();
        }

        /// <summary>
        /// 打开音量设置窗口。
        /// </summary>
        private partial void OnClick_SettingsBtn()
        {
            GameModule.UI.ShowUIAsync<SettingsUI>();
        }

        /// <summary>
        /// 保留尚未开放的存档入口回调。
        /// </summary>
        private partial void OnClick_SaveBtn()
        {
            // 存档入口保持禁用，当前没有对应业务。
        }

        /// <summary>
        /// 保留尚未开放的制作人员入口回调。
        /// </summary>
        private partial void OnClick_CreditsBtn()
        {
            // 制作人员入口保持禁用，当前没有对应业务。
        }

        /// <summary>
        /// 请求退出游戏。
        /// </summary>
        private partial void OnClick_ExitBtn()
        {
            GameManager.Instance.ExitGame();
        }

        /// <summary>
        /// 释放生成绑定所注册的菜单回调。
        /// </summary>
        protected override void OnDestroy()
        {
            // 异步加载完成前关闭窗口时，生成绑定尚未初始化。
            if (m_bindComponent == null)
            {
                return;
            }

            m_btn_Start.onClick.RemoveListener(OnClick_StartBtn);
            m_btn_Settings.onClick.RemoveListener(OnClick_SettingsBtn);
            m_btn_Save.onClick.RemoveListener(OnClick_SaveBtn);
            m_btn_Credits.onClick.RemoveListener(OnClick_CreditsBtn);
            m_btn_Exit.onClick.RemoveListener(OnClick_ExitBtn);
        }
    }
}
