using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 局部时间轨迹，查询不推进球状态。
    /// </summary>
    public readonly struct BallTrajectory
    {
        /// <summary>
        /// 轨迹起点的球心位置。
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        /// 轨迹起点的速度。
        /// </summary>
        public readonly Vector2 Velocity;

        /// <summary>
        /// 轨迹内的恒定加速度。
        /// </summary>
        public readonly Vector2 Acceleration;

        /// <summary>
        /// 轨迹起点的半径。
        /// </summary>
        public readonly double Radius;

        /// <summary>
        /// 轨迹内的半径变化率。
        /// </summary>
        public readonly double RadiusRate;

        /// <summary>
        /// 轨迹有效时长。
        /// </summary>
        public readonly double Duration;

        /// <summary>
        /// 创建可查询的局部时间轨迹。
        /// </summary>
        /// <param name="state">起始状态。</param>
        /// <param name="acceleration">恒定加速度。</param>
        /// <param name="radiusRate">半径变化率。</param>
        /// <param name="duration">有效轨迹时长。</param>
        public BallTrajectory(
            BallState state,
            Vector2 acceleration,
            double radiusRate,
            double duration)
        {
            Position = state.Position;
            Velocity = state.Velocity;
            Acceleration = acceleration;
            Radius = state.Radius;
            RadiusRate = radiusRate;
            Duration = duration;
        }

        /// <summary>
        /// 计算轨迹中指定时间的球状态。
        /// </summary>
        /// <param name="time">距轨迹起点的秒数。</param>
        /// <returns>对应的球状态。</returns>
        public BallState At(double time)
        {
            float t = (float)time;

            return new BallState
            {
                Position = Position + Velocity * t + Acceleration * (0.5f * t * t),
                Velocity = Velocity + Acceleration * t,
                Radius = (float)(Radius + RadiusRate * time)
            };
        }
    }
}
