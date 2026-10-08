using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 轨迹接触时间求解，覆盖变半径球对、圆障碍及平面。
    /// </summary>
    public sealed class ContactSolver
    {
        /// <summary>
        /// 复用的接触多项式实根求解器。
        /// </summary>
        private readonly PolynomialRoots _roots = new PolynomialRoots();

        /// <summary>
        /// 由低阶到高阶存储的接触多项式系数缓存。
        /// </summary>
        private readonly double[] _coefficients = new double[5];

        /// <summary>
        /// 求两条轨迹首次向内接触的时间。
        /// </summary>
        /// <param name="a">运动球的连续轨迹。</param>
        /// <param name="b">另一球或静态圆的连续轨迹。</param>
        /// <param name="capture">为 true 时求球心进入目标圆的时间，忽略运动球半径。</param>
        /// <returns>有效轨迹区间内首次向内接触的秒数；未找到时为正无穷。</returns>
        public double Circle(BallTrajectory a, BallTrajectory b, bool capture = false)
        {
            // 相对位置为二次轨迹，半径为一次函数，接触方程最高为四次。
            Vector2 p = a.Position - b.Position;
            Vector2 v = a.Velocity - b.Velocity;
            Vector2 h = (a.Acceleration - b.Acceleration) * .5f;
            double radius = capture ? b.Radius : a.Radius + b.Radius;
            double rate = capture ? 0 : a.RadiusRate + b.RadiusRate;
            double gap = p.magnitude - radius;

            if (gap <= .00001 && (capture || Vector2.Dot(p.normalized, v) - rate < -.000001))
            {
                return 0;
            }

            _coefficients[0] = p.sqrMagnitude - radius * radius;
            _coefficients[1] = 2 * (Vector2.Dot(p, v) - radius * rate);
            _coefficients[2] = v.sqrMagnitude + 2 * Vector2.Dot(p, h) - rate * rate;
            _coefficients[3] = 2 * Vector2.Dot(v, h);
            _coefficients[4] = h.sqrMagnitude;

            int count = _roots.Find(_coefficients, 4, 0, Math.Min(a.Duration, b.Duration));

            for (int i = 0; i < count; i++)
            {
                double t = _roots.Root(i);
                Vector2 delta = p + v * (float)t + h * (float)(t * t);
                Vector2 velocity = v + 2 * h * (float)t;

                // 只接受向内接触的根，排除球体离开接触面的时刻。
                if (Vector2.Dot(delta.normalized, velocity) - rate < -.000001)
                {
                    return t;
                }
            }

            return double.PositiveInfinity;
        }

        /// <summary>
        /// 求球首次接触内法线平面边界的时间。
        /// </summary>
        /// <param name="a">运动球的连续轨迹。</param>
        /// <param name="normal">指向允许活动区域的单位法线。</param>
        /// <param name="offset">边界平面在法线方向的投影偏移。</param>
        /// <returns>首次向内接触边界的秒数；未找到时为正无穷。</returns>
        public double Plane(BallTrajectory a, Vector2 normal, float offset)
        {
            // 沿内法线的球心距离减去半径，得到球面到库边的有符号间隙。
            double gap = Vector2.Dot(normal, a.Position) - offset - a.Radius;
            double v = Vector2.Dot(normal, a.Velocity) - a.RadiusRate;
            double h = .5 * Vector2.Dot(normal, a.Acceleration);

            if (gap <= .00001 && v < -.000001)
            {
                return 0;
            }

            _coefficients[0] = gap;
            _coefficients[1] = v;
            _coefficients[2] = h;

            int count = _roots.Find(_coefficients, 2, 0, a.Duration);

            for (int i = 0; i < count; i++)
            {
                double t = _roots.Root(i);

                if (v + 2 * h * t < -.000001)
                {
                    return t;
                }
            }

            return double.PositiveInfinity;
        }
    }
}
