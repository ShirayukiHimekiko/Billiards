namespace GameLogic
{
    /// <summary>
    /// 将白球半径恢复到本杆基准值。
    /// </summary>
    public sealed class RestorePropEffect : IPropEffect, IPropShotEffect
    {
        /// <summary>
        /// 可供真实世界和预测副本共享的无状态规格。
        /// </summary>
        public static readonly PropEffectSpec Spec = new PropEffectSpec(PropEffectKind.Restore, new RestorePropEffect());

        /// <inheritdoc />
        public PropEffectResult Trigger(SimulatedBall ball, PropPlacementData data)
        {
            ApplyToShot(ball, data);
            return default;
        }

        /// <inheritdoc />
        public void ApplyToShot(SimulatedBall ball, PropPlacementData data)
        {
            ball.State.Radius = ball.BaseRadius;
        }
    }
}
