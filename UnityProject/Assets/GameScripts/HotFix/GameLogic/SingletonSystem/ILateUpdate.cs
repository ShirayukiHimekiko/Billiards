namespace GameLogic
{
    /// <summary>
    /// 延迟帧更新接口。
    /// </summary>
    public interface ILateUpdate
    {
        /// <summary>
        /// 游戏框架模块轮询。
        /// </summary>
        void OnLateUpdate();
    }
}
