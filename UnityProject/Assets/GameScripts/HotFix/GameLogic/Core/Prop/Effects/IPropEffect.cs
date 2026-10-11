namespace GameLogic
{
    /// <summary>
    /// 无场景依赖、无实例可变状态的道具模拟效果。
    /// </summary>
    public interface IPropEffect
    {
        /// <summary>
        /// 在真实世界或预测世界中执行本次触发。
        /// </summary>
        /// <param name="ball">接触道具的模拟球。</param>
        /// <param name="data">效果的配置参数。</param>
        /// <returns>交由世界写入的触发结果。</returns>
        PropEffectResult Trigger(SimulatedBall ball, PropPlacementData data);
    }

    /// <summary>
    /// 在持续效果有效时对新杆白球重新应用的球体效果。
    /// </summary>
    public interface IPropShotEffect
    {
        /// <summary>
        /// 将仍有效的效果应用到新杆白球。
        /// </summary>
        /// <param name="ball">新杆白球的模拟状态。</param>
        /// <param name="data">效果配置参数。</param>
        void ApplyToShot(SimulatedBall ball, PropPlacementData data);
    }
}
