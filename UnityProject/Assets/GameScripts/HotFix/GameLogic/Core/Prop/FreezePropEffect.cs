namespace GameLogic
{
    /// <summary>
    /// 暂停白球半径变化。
    /// </summary>
    public sealed class FreezePropEffect : IPropEffect
    {
        public void Apply(SimulatedBall ball, float rateMultiplier)
        {
            ball.RateMultiplier = rateMultiplier;
        }
    }
}
