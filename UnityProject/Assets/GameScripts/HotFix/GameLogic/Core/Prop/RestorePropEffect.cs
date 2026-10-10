namespace GameLogic
{
    /// <summary>
    /// 将白球半径恢复到本杆基准值。
    /// </summary>
    public sealed class RestorePropEffect : IPropEffect
    {
        public void Apply(SimulatedBall ball, float rateMultiplier)
        {
            ball.State.Radius = ball.BaseRadius;
        }
    }
}
