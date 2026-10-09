namespace GameLogic
{
    /// <summary>
    /// 反转白球半径变化方向。
    /// </summary>
    public sealed class ReversePropEffect : IPropEffect
    {
        public void Apply(SimulatedBall ball)
        {
            ball.RadiusDirection = -ball.RadiusDirection;
        }
    }
}
