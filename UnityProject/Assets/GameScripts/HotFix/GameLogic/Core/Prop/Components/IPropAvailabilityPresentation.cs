namespace GameLogic
{
    /// <summary>
    /// 可选的道具可用状态表现功能。
    /// </summary>
    public interface IPropAvailabilityPresentation : IPropComponent
    {
        /// <summary>
        /// 更新道具的可用状态表现。
        /// </summary>
        /// <param name="available">道具是否仍可触发。</param>
        void SetAvailable(bool available);
    }
}
