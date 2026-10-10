namespace GameLogic
{
    /// <summary>
    /// 将白球当前半径变化速率提升为两倍。
    /// </summary>
    public sealed class FastChangePropEffect : IPropEffect
    {
        public void Apply(SimulatedBall ball, float rateMultiplier)
        {
            ball.RateMultiplier = rateMultiplier;
        }
    }
}
