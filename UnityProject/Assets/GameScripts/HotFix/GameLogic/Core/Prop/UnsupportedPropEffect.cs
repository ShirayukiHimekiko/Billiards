namespace GameLogic
{
    /// <summary>
    /// 兼容旧配置的空效果。正式配置不会映射到此类型。
    /// </summary>
    public sealed class UnsupportedPropEffect : IPropEffect
    {
        public void Apply(SimulatedBall ball, float rateMultiplier)
        {
        }
    }
}
