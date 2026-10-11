namespace GameLogic
{
    /// <summary>
    /// 翻转尺寸趋势策略。
    /// </summary>
    public sealed class FlipPropEffect : IPropEffect, IPropShotEffect
    {
        /// <summary>
        /// 可供真实世界和预测副本共享的无状态规格。
        /// </summary>
        public static readonly PropEffectSpec Spec = new PropEffectSpec(
            PropEffectKind.Flip, new FlipPropEffect(), PropEffectDurationMode.PersistentInLevel);

        /// <inheritdoc />
        public PropEffectResult Trigger(SimulatedBall ball, PropPlacementData data)
        {
            ApplyToShot(ball, data);
            return default;
        }

        /// <inheritdoc />
        public void ApplyToShot(SimulatedBall ball, PropPlacementData data)
        {
            ball.Growing = !ball.Growing;
        }
    }
}
