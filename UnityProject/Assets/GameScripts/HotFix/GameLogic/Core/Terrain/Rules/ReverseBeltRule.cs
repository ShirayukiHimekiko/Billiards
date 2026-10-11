namespace GameLogic
{
    /// <summary>
    /// 反向带规则：每次由外到内经过时翻转速度，速率保持不变。
    /// </summary>
    public sealed class ReverseBeltRule : TerrainRuleBase
    {
        /// <summary>
        /// 反转当前速度矢量；是否再次触发由每球每地形的接触记录决定。
        /// </summary>
        /// <param name="hit">命中的稳定索引与几何信息。</param>
        /// <param name="context">受控模拟上下文。</param>
        public override void Apply(in TerrainHit hit, TerrainContext context)
        {
            SimulatedBall ball = context.Balls[hit.BallIndex];
            ball.State.Velocity = -ball.State.Velocity;
        }
    }
}
