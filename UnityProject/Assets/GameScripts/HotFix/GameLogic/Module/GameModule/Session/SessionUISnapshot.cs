namespace GameLogic
{
    /// <summary>
    /// 界面刷新所需的会话状态快照。
    /// </summary>
    public readonly struct SessionUISnapshot
    {
        /// <summary>
        /// 尚未入洞的黑球数量。
        /// </summary>
        public readonly int RemainingBlack;

        /// <summary>
        /// 会话运行状态。
        /// </summary>
        public readonly SessionState State;

        /// <summary>
        /// 剩余击球次数。
        /// </summary>
        public readonly int Shots;

        /// <summary>
        /// 球体是否正在增大。
        /// </summary>
        public readonly bool Growing;

        /// <summary>
        /// 当前进度中是否已有道具使用记录。
        /// </summary>
        public readonly bool ItemUsed;

        /// <summary>
        /// 尚可使用的折射预测道具次数。
        /// </summary>
        public readonly int PredictionPropRemaining;

        /// <summary>
        /// 折射预测道具是否已为下一杆启用。
        /// </summary>
        public readonly bool PredictionPropArmed;

        /// <summary>
        /// 当前状态提示。
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// 创建界面状态快照。
        /// </summary>
        /// <param name="state">会话状态。</param>
        /// <param name="shots">剩余击球次数。</param>
        /// <param name="growing">球体是否正在增大。</param>
        /// <param name="itemUsed">当前进度中是否已有道具使用记录。</param>
        /// <param name="message">状态提示。</param>
        /// <param name="remainingBlack">尚未入洞的黑球数。</param>
        /// <param name="predictionPropRemaining">尚可使用的折射预测道具次数。</param>
        /// <param name="predictionPropArmed">折射预测道具是否已为下一杆启用。</param>
        public SessionUISnapshot(
            SessionState state,
            int shots,
            bool growing,
            bool itemUsed,
            string message,
            int remainingBlack,
            int predictionPropRemaining,
            bool predictionPropArmed)
        {
            State = state;
            Shots = shots;
            Growing = growing;
            ItemUsed = itemUsed;
            Message = message;
            RemainingBlack = remainingBlack;
            PredictionPropRemaining = predictionPropRemaining;
            PredictionPropArmed = predictionPropArmed;
        }
    }
}
