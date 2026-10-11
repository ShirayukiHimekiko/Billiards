namespace GameLogic
{
    /// <summary>
    /// 暂停白球半径变化。
    /// </summary>
    public sealed class FreezePropEffect : IPropEffect, IPropShotEffect
    {
        /// <summary>
        /// 可供真实世界和预测副本共享的无状态规格。
        /// </summary>
        public static readonly PropEffectSpec Spec = new PropEffectSpec(PropEffectKind.Freeze, new FreezePropEffect());

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
