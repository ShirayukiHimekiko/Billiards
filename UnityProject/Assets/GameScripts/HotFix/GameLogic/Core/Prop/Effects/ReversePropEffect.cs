namespace GameLogic
{
    /// <summary>
    /// 反转白球半径变化方向。
    /// </summary>
    public sealed class ReversePropEffect : IPropEffect, IPropShotEffect
    {
        /// <summary>
        /// 可供真实世界和预测副本共享的无状态规格。
        /// </summary>
        public static readonly PropEffectSpec Spec = new PropEffectSpec(
            PropEffectKind.Reverse, new ReversePropEffect(), PropEffectDurationMode.PersistentInLevel);

        /// <inheritdoc />
        public PropEffectResult Trigger(SimulatedBall ball, PropPlacementData data)
        {
            ApplyToShot(ball, data);
            return default;
        }

        /// <inheritdoc />
        public void ApplyToShot(SimulatedBall ball, PropPlacementData data)
        {
            ball.RadiusDirection = -ball.RadiusDirection;
        }
    }
}
