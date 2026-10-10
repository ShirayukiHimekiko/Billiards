namespace GameLogic
{
    /// <summary>
    /// 钥匙本身不改变球体；绑定洞由物理世界在同一拾取事件中解锁。
    /// </summary>
    public sealed class KeyPropEffect : IPropEffect
    {
        public void Apply(SimulatedBall ball, float rateMultiplier)
        {
        }
    }
}
