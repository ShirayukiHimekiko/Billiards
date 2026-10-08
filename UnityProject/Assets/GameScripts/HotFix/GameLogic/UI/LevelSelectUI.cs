using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 按配置顺序展示关卡目录，控件绑定由项目生成器维护。
    /// </summary>
    [Window(UILayer.UI, "LevelSelectUI", fullScreen: true)]
    public sealed partial class LevelSelectUI : UIWindow
    {
        /// <summary>
        /// 选关界面复用的关卡卡片列表。
        /// </summary>
        private readonly List<LevelItemWidget> _items = new List<LevelItemWidget>();

        /// <summary>
        /// 隐藏仅用于创建卡片的编辑器模板。
        /// </summary>
        protected override void OnCreate()
        {
            m_item_LevelItemWidget.SetActive(false);
        }

        /// <summary>
        /// 按关卡目录复用卡片，滚动回列表起点。
        /// </summary>
        protected override void OnRefresh()
        {
            var levels = GameManager.Instance.LevelCatalogue;
            AdjustIconNum(_items, levels.Count, m_tf_Content, m_item_LevelItemWidget);

            for (int i = 0; i < levels.Count; i++)
            {
                _items[i].SetLevel(levels[i]);
            }

            m_scroll_Levels.verticalNormalizedPosition = 1;
        }

        /// <summary>
        /// 显示选择提示或加载失败原因。
        /// </summary>
        /// <param name="message">失败原因；空字符串显示默认提示。</param>
        public void SetMessage(string message)
        {
            m_text_Message.text = string.IsNullOrEmpty(message) ? "选择任意一关开始练习" : message;
        }

        /// <summary>
        /// 返回开始菜单。
        /// </summary>
        private partial void OnClick_BackBtn()
        {
            GameManager.Instance.ReturnToMenuAsync().Forget();
        }

        /// <summary>
        /// 释放生成绑定注册的回调；子卡片由框架先行销毁。
        /// </summary>
        protected override void OnDestroy()
        {
            // 异步加载完成前关闭窗口时，绑定尚未创建。
            if (m_bindComponent != null)
            {
                m_btn_Back.onClick.RemoveListener(OnClick_BackBtn);
            }
        }
    }
}
