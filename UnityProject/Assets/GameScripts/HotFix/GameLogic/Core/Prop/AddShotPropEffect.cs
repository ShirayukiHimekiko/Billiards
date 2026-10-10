namespace GameLogic
{
    /// <summary>
    /// 加杆由会话统一修改剩余杆数，物理层仅记录触发事件。
    /// </summary>
    public sealed class AddShotPropEffect : IPropEffect
    {
        public void Apply(SimulatedBall ball, float rateMultiplier)
        {
        }
    }
}
