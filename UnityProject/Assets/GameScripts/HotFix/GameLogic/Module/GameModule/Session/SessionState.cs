namespace GameLogic
{
    /// <summary>
    /// 桌球会话的运行状态。
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// 所有球已停止，可以瞄准和出杆。
        /// </summary>
        Ready,

        /// <summary>
        /// 正在推进本杆的多球运动与接触事件。
        /// </summary>
        Rolling,

        /// <summary>
        /// 会话暂停，暂不推进模拟。
        /// </summary>
        Paused,

        /// <summary>
        /// 全部黑球入洞，本关完成。
        /// </summary>
        Won,

        /// <summary>
        /// 机会耗尽且仍有黑球未入洞。
        /// </summary>
        Lost
    }
}
