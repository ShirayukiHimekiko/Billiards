namespace GameLogic
{
    /// <summary>
    /// 产生额外击球次数，由会话从模拟世界消费。
    /// </summary>
    public sealed class AddShotPropEffect : IPropEffect
    {
        /// <summary>
        /// 可供真实世界和预测副本共享的无状态规格。
        /// </summary>
        public static readonly PropEffectSpec Spec = new PropEffectSpec(
            PropEffectKind.AddShot, new AddShotPropEffect());

        /// <inheritdoc />
        public PropEffectResult Trigger(SimulatedBall ball, PropPlacementData data)
        {
            return new PropEffectResult(shotBonus: UnityEngine.Mathf.Max(
                1, UnityEngine.Mathf.RoundToInt(data.RateMultiplier)));
        }
    }
}
