namespace GameLogic
{
    /// <summary>
    /// 帧更新接口。
    /// </summary>
    public interface IUpdate
    {
        /// <summary>
        /// 游戏框架模块轮询。
        /// </summary>
        void OnUpdate();
    }
}
