namespace GameLogic
{
    /// <summary>
    /// 请求模拟世界解锁配置绑定的球洞。
    /// </summary>
    public sealed class KeyPropEffect : IPropEffect
    {
        /// <summary>
        /// 可供真实世界和预测副本共享的无状态规格。
        /// </summary>
        public static readonly PropEffectSpec Spec = new PropEffectSpec(
            PropEffectKind.Key, new KeyPropEffect(), PropEffectDurationMode.None);

        /// <inheritdoc />
        public PropEffectResult Trigger(SimulatedBall ball, PropPlacementData data)
        {
            return new PropEffectResult(unlockPocketId: data.TargetPocketId);
        }
    }
}
