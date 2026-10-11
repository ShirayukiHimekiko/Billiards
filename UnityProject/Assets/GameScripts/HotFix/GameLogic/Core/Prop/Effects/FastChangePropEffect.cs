namespace GameLogic
{
    /// <summary>
    /// 将白球当前半径变化速率提升为两倍。
    /// </summary>
    public sealed class FastChangePropEffect : IPropEffect, IPropShotEffect
    {
        /// <summary>
        /// 可供真实世界和预测副本共享的无状态规格。
        /// </summary>
        public static readonly PropEffectSpec Spec = new PropEffectSpec(
            PropEffectKind.FastChange, new FastChangePropEffect());

        /// <inheritdoc />
        public PropEffectResult Trigger(SimulatedBall ball, PropPlacementData data)
        {
            ApplyToShot(ball, data);
            return default;
        }

        /// <inheritdoc />
        public void ApplyToShot(SimulatedBall ball, PropPlacementData data)
        {
            ball.RateMultiplier = data.RateMultiplier;
        }
    }
}
