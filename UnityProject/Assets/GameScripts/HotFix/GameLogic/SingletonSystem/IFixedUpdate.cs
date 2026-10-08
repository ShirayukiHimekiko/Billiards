namespace GameLogic
{
    /// <summary>
    /// 固定帧更新接口。
    /// </summary>
    public interface IFixedUpdate
    {
        /// <summary>
        /// 游戏框架模块轮询。
        /// </summary>
        void OnFixedUpdate();
    }
}
