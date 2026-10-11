using System;

namespace GameLogic
{
    /// <summary>
    /// 持续效果的计算规则。
    /// </summary>
    public enum PropEffectDurationMode
    {
        Configured,
        PersistentInLevel,
        PreviewNextShot,
        None
    }

    /// <summary>
    /// 由模拟世界维护的道具可视能力。
    /// </summary>
    [Flags]
    public enum PropVisibility
    {
        None = 0,
        Reveal = 1,
        Preview = 2
    }

    /// <summary>
    /// 与场景对象无关、可在真实世界和预测副本间共享的效果规格。
    /// </summary>
    public sealed class PropEffectSpec
    {
        /// <summary>
        /// 供配置兼容和界面识别的效果类型。
        /// </summary>
        public PropEffectKind Kind { get; }

        /// <summary>
        /// 无可变状态的效果执行器。
        /// </summary>
        public IPropEffect Effect { get; }

        /// <summary>
        /// 使用次数对应的持续规则。
        /// </summary>
        public PropEffectDurationMode DurationMode { get; }

        /// <summary>
        /// 持续期间需要维持的可视能力。
        /// </summary>
        public PropVisibility Visibility { get; }

        /// <summary>
        /// 创建只读效果规格。
        /// </summary>
        /// <param name="kind">配置兼容类型。</param>
        /// <param name="effect">无状态效果执行器。</param>
        /// <param name="durationMode">持续规则。</param>
        /// <param name="visibility">持续期间的可视能力。</param>
        public PropEffectSpec(PropEffectKind kind, IPropEffect effect,
            PropEffectDurationMode durationMode = PropEffectDurationMode.Configured,
            PropVisibility visibility = PropVisibility.None)
        {
            Kind = kind;
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
            DurationMode = durationMode;
            Visibility = visibility;
        }
    }
}
