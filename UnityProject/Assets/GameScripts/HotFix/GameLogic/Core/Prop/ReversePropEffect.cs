namespace GameLogic
{
    /// <summary>
    /// 反转白球半径变化方向。
    /// </summary>
    public sealed class ReversePropEffect : IPropEffect
    {
        public void Apply(SimulatedBall ball, float rateMultiplier)
        {
            ball.RadiusDirection = -ball.RadiusDirection;
        }
    }
}
