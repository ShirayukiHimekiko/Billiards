using System;

namespace GameLogic
{
    /// <summary>
    /// 需要会话或表现层消费的道具效果，在接入对应模块前显式暴露未完成状态。
    /// </summary>
    public sealed class UnsupportedPropEffect : IPropEffect
    {
        public void Apply(SimulatedBall ball)
        {
            throw new NotSupportedException("当前道具效果需要会话或表现层接入。");
        }
    }
}
