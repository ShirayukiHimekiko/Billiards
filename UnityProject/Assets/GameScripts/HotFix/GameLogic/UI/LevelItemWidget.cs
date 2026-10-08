using Cysharp.Threading.Tasks;
using GameConfig;

namespace GameLogic
{
    /// <summary>
    /// 显示一个关卡的学习主题，并提交该关卡 ID。
    /// </summary>
    public sealed partial class LevelItemWidget : UIWidget
    {
        /// <summary>
        /// 当前卡片绑定的关卡 ID。
        /// </summary>
        private int _levelId;

        /// <summary>
        /// 数据绑定完成前不接受点击。
        /// </summary>
        protected override void OnCreate()
        {
            m_btn_Enter.interactable = false;
        }

        /// <summary>
        /// 绑定关卡目录记录，不创建关卡世界。
        /// </summary>
        /// <param name="level">由关卡管理器排序的配置记录。</param>
        public void SetLevel(Level level)
        {
            _levelId = level.Id;
            m_text_Number.text = level.Order.ToString("D2");
            m_text_Name.text = level.Name;
            m_text_Difficulty.text = $"{level.Difficulty} · {level.MaxShots} 次机会";
            m_text_Objective.text = level.Objective;
            m_btn_Enter.interactable = true;
        }

        /// <summary>
        /// 请求进入绑定的关卡，重复请求由流程管理器拦截。
        /// </summary>
        private partial void OnClick_EnterBtn()
        {
            GameManager.Instance.StartGameAsync(_levelId).Forget();
        }

        /// <summary>
        /// 释放生成绑定注册的回调。
        /// </summary>
        protected override void OnDestroy()
        {
            if (m_bindComponent != null)
            {
                m_btn_Enter.onClick.RemoveListener(OnClick_EnterBtn);
            }
        }
    }
}
