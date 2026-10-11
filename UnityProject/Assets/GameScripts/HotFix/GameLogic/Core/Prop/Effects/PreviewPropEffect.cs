namespace GameLogic
{
    /// <summary>
    /// 启用预演能力，由真实世界与预测世界分别记录。
    /// </summary>
    public sealed class PreviewPropEffect : IPropEffect
    {
        /// <summary>
        /// 可供真实世界和预测副本共享的无状态规格。
        /// </summary>
        public static readonly PropEffectSpec Spec = new PropEffectSpec(
            PropEffectKind.Preview, new PreviewPropEffect(),
            PropEffectDurationMode.PreviewNextShot, PropVisibility.Preview);

        /// <inheritdoc />
        public PropEffectResult Trigger(SimulatedBall ball, PropPlacementData data)
        {
            return new PropEffectResult(visibility: PropVisibility.Preview);
        }
    }
}
