using System;

namespace GameLogic
{
    /// <summary>
    /// 集中创建道具策略，新增效果时不依赖具体表现组件。
    /// </summary>
    public static class PropEffects
    {
        /// <summary>
        /// 实际模拟与预测可复用的无状态翻转策略。
        /// </summary>
        private static readonly IPropEffect Flip = new FlipPropEffect();
        private static readonly IPropEffect Reverse = new ReversePropEffect();
        private static readonly IPropEffect Freeze = new FreezePropEffect();
        private static readonly IPropEffect Restore = new RestorePropEffect();
        private static readonly IPropEffect FastChange = new FastChangePropEffect();
        private static readonly IPropEffect Unsupported = new UnsupportedPropEffect();

        /// <summary>
        /// 获取道具类型对应的策略。
        /// </summary>
        /// <param name="kind">请求的道具策略类型。</param>
        /// <returns>可复用的无状态策略；不支持的类型抛出参数异常。</returns>
        public static IPropEffect Get(PropEffectKind kind)
        {
            switch (kind)
            {
                case PropEffectKind.Flip:
                    return Flip;
                case PropEffectKind.Reverse:
                    return Reverse;
                case PropEffectKind.Freeze:
                    return Freeze;
                case PropEffectKind.Restore:
                    return Restore;
                case PropEffectKind.FastChange:
                    return FastChange;
                case PropEffectKind.Reveal:
                case PropEffectKind.Preview:
                case PropEffectKind.AddShot:
                case PropEffectKind.Key:
                    return Unsupported;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>
        /// 应用模拟效果。
        /// </summary>
        /// <param name="kind">请求的道具策略类型。</param>
        /// <param name="ball">接受效果的模拟球。</param>
        public static void Apply(PropEffectKind kind, SimulatedBall ball)
        {
            Get(kind).Apply(ball);
        }
    }
}
