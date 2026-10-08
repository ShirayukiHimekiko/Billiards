using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 桌球玩法与界面之间的事件定义。
    /// </summary>
    public static class SessionEvents
    {
        /// <summary>
        /// 桌球会话状态变化的通知事件。
        /// </summary>
        public static readonly int StateChanged = RuntimeId.ToRuntimeId("Billiards.StateChanged");
    }
}
