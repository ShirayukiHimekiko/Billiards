using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 显示折射预测道具的剩余次数、启用状态与可交互状态。
    /// </summary>
    public sealed partial class PredictionPropWidget : UIWidget
    {
        /// <summary>
        /// 初始化为关卡加载期间的不可交互状态。
        /// </summary>
        protected override void OnCreate()
        {
            SetState(0, false, false);
        }

        /// <summary>
        /// 按会话快照刷新道具的文字、标记及按钮状态。
        /// </summary>
        /// <param name="remaining">尚可使用的次数。</param>
        /// <param name="armed">是否已为下一杆启用。</param>
        /// <param name="interactable">当前会话状态是否允许切换。</param>
        public void SetState(int remaining, bool armed, bool interactable)
        {
            bool depleted = remaining <= 0;
            m_btn_Use.interactable = interactable && !depleted;
            m_go_Active.SetActive(armed);
            m_text_Count.text = $"×{remaining}";
            m_text_State.text = armed
                ? "折射预测 · 已启用"
                : depleted
                    ? "折射预测 · 已耗尽"
                    : interactable
                        ? "折射预测 · 下一杆"
                        : "折射预测 · 暂不可用";

            Color iconColor = depleted
                ? new Color(.45f, .45f, .5f, .72f)
                : armed
                    ? new Color(.55f, 1f, 1f, 1f)
                    : Color.white;

            if (!interactable && !depleted)
            {
                iconColor.a = .62f;
            }

            m_img_Icon.color = iconColor;
        }

        /// <summary>
        /// 请求为下一次成功出杆切换折射预测。
        /// </summary>
        private partial void OnClick_UseBtn()
        {
            GameManager.Instance.Session?.TryTogglePredictionProp();
        }

        /// <summary>
        /// 释放生成绑定注册的按钮回调。
        /// </summary>
        protected override void OnDestroy()
        {
            if (m_bindComponent != null)
            {
                m_btn_Use.onClick.RemoveListener(OnClick_UseBtn);
            }
        }
    }
}
