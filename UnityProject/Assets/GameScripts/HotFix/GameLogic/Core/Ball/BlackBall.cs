using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 固定尺寸的黑球表现，不依赖白球的击球输入和尺寸趋势组件。
    /// </summary>
    public sealed class BlackBall : BallBase
    {
        /// <summary>
        /// 黑球固定的球面颜色。
        /// </summary>
        private static readonly Color _blackColor = new Color(0.075f, 0.085f, 0.095f);

        /// <summary>
        /// 黑球固定的球面颜色。
        /// </summary>
        protected override Color SurfaceColor => _blackColor;
    }
}
