namespace GameLogic
{
    /// <summary>
    /// 实际模拟和预测共用的无场景依赖道具策略。
    /// </summary>
    public interface IPropEffect
    {
        /// <summary>
        /// 将道具效果应用到传入的球体模拟状态。
        /// </summary>
        /// <param name="ball">效果修改的模拟球，调用方负责作用域及使用次数。</param>
        void Apply(SimulatedBall ball);
    }
}
