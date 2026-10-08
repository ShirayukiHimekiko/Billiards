using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 显示桌球状态并提供暂停、重试和返回入口。
    /// </summary>
    [Window(UILayer.UI, "GameMainUI", fullScreen: true)]
    public sealed partial class MainUI : UIWindow
    {
        /// <summary>
        /// 当前图形提示的显示开关。
        /// </summary>
        private bool _hintsVisible = true;

        /// <summary>
        /// 本关机会总数，用于显示剩余次数。
        /// </summary>
        private int _maxShots;

        /// <summary>
        /// 本关是否需要显示图形提示入口。
        /// </summary>
        private bool _hasGeometry;

        /// <summary>
        /// 本关是否需要显示道具触发状态。
        /// </summary>
        private bool _hasProps;

        /// <summary>
        /// 最近显示的整数力度百分比，避免重复刷新。
        /// </summary>
        private int _lastPower = -1;

        /// <summary>
        /// 获取当前界面关联的世界台面。
        /// </summary>
        public BoardView Board
        {
            get;
            private set;
        }

        /// <summary>
        /// 将界面绑定到当前关卡与世界台面。
        /// </summary>
        /// <param name="level">关卡配置。</param>
        /// <param name="board">世界台面。</param>
        public void ConfigureLevel(LevelData level, BoardView board)
        {
            Board = board;
            _maxShots = level.MaxShots;
            _hintsVisible = level.ShowHints;
            _hasGeometry = level.Geometries.Count > 0;
            _hasProps = level.Props.Count > 0;
            m_btn_Hints.gameObject.SetActive(_hasGeometry);
        }

        /// <summary>
        /// 监听会话快照；控件监听由生成绑定注册。
        /// </summary>
        protected override void RegisterEvent()
        {
            AddUIEvent<SessionUISnapshot>(SessionEvents.StateChanged, OnStateChanged);
        }

        /// <summary>
        /// 初始化关卡准备期间的提示与交互状态。
        /// </summary>
        protected override void OnCreate()
        {
            m_text_Status.text = "正在准备关卡…";
            m_slider_Power.interactable = false;
            m_btn_Restart.interactable = false;
            m_btn_Pause.interactable = false;
        }

        /// <summary>
        /// 根据会话快照刷新状态提示与操作按钮。
        /// </summary>
        /// <param name="snapshot">当前会话状态。</param>
        private void OnStateChanged(SessionUISnapshot snapshot)
        {
            m_text_Status.text = snapshot.Message;
            m_text_Shots.text = $"剩余机会  {snapshot.Shots} / {_maxShots}";
            m_text_Trend.text = $"剩余黑球：{snapshot.RemainingBlack}\n大小趋势：" + (snapshot.Growing ? "变大" : "变小");

            if (_hasProps)
            {
                m_text_Trend.text += "\n道具：" + (snapshot.ItemUsed ? "已触发" : "等待触发");
            }

            m_btn_Restart.interactable = true;
            m_btn_Pause.interactable = snapshot.State != SessionState.Lost;
            m_text_Pause.text = snapshot.State == SessionState.Won ? "下一关" : snapshot.State == SessionState.Paused ? "继续" : "暂停";
            m_text_Status.color = snapshot.State == SessionState.Won ? new Color(0.5f, 1, 0.7f) : new Color(0.94f, 0.9f, 0.78f);
        }

        /// <summary>
        /// 在蓄力百分比变化时刷新力量显示。
        /// </summary>
        protected override void OnUpdate()
        {
            if (GameManager.Instance.Session == null || Board == null)
            {
                return;
            }

            int power = Mathf.RoundToInt(Board.Ball.Shoot.Power * 100);

            if (power == _lastPower)
            {
                return;
            }

            _lastPower = power;
            m_slider_Power.SetValueWithoutNotify(power / 100f);
            m_text_Power.text = $"力量  {power}%";
        }

        /// <summary>
        /// 保留只读力量条的生成回调，蓄力由球体输入控制。
        /// </summary>
        /// <param name="value">力量条显示值。</param>
        private partial void OnSlider_PowerChange(float value)
        {
            // 力量条只用于显示；OnUpdate 使用 SetValueWithoutNotify 刷新。
        }

        /// <summary>
        /// 重置当前关卡。
        /// </summary>
        private partial void OnClick_RestartBtn()
        {
            GameManager.Instance.Session?.Restart();
        }

        /// <summary>
        /// 通关后请求进入下一关，否则切换当前会话的暂停状态。
        /// </summary>
        private partial void OnClick_PauseBtn()
        {
            if (GameManager.Instance.Session?.State == SessionState.Won)
            {
                GameManager.Instance.NextLevelAsync().Forget();
            }
            else
            {
                GameManager.Instance.Session?.TogglePause();
            }
        }

        /// <summary>
        /// 释放当前关卡并返回选关。
        /// </summary>
        private partial void OnClick_MenuBtn()
        {
            GameManager.Instance.ReturnToLevelSelectAsync().Forget();
        }

        /// <summary>
        /// 请求退出游戏。
        /// </summary>
        private partial void OnClick_ExitBtn()
        {
            GameManager.Instance.ExitGame();
        }

        /// <summary>
        /// 切换世界台面的目标提示。
        /// </summary>
        private partial void OnClick_HintsBtn()
        {
            if (Board == null)
            {
                return;
            }

            _hintsVisible = !_hintsVisible;
            Board.SetHints(_hintsVisible);
        }

        /// <summary>
        /// 释放生成绑定所注册的关卡操作回调。
        /// </summary>
        protected override void OnDestroy()
        {
            // 异步加载完成前关闭窗口时，生成绑定尚未初始化。
            if (m_bindComponent == null)
            {
                return;
            }

            m_slider_Power.onValueChanged.RemoveListener(OnSlider_PowerChange);
            m_btn_Restart.onClick.RemoveListener(OnClick_RestartBtn);
            m_btn_Pause.onClick.RemoveListener(OnClick_PauseBtn);
            m_btn_Menu.onClick.RemoveListener(OnClick_MenuBtn);
            m_btn_Exit.onClick.RemoveListener(OnClick_ExitBtn);
            m_btn_Hints.onClick.RemoveListener(OnClick_HintsBtn);
        }
    }
}
