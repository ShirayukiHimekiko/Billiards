namespace GameLogic
{
    /// <summary>
    /// 翻转尺寸趋势策略。
    /// </summary>
    public sealed class FlipPropEffect : IPropEffect
    {
        /// <summary>
        /// 翻转球体趋势。
        /// </summary>
        /// <param name="ball">需要翻转增大或缩小趋势的模拟球。</param>
        public void Apply(SimulatedBall ball)
        {
            ball.Growing = !ball.Growing;
        }
    }
}
