using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 三角形双目标判定结果。
    /// </summary>
    public readonly struct GoalMatch
    {
        public readonly GoalKind Kind;
        public readonly Vector2 Center;
        public readonly float Radius;
        public readonly float Error;

        public GoalMatch(GoalKind kind, Vector2 center, float radius, float error)
        {
            Kind = kind;
            Center = center;
            Radius = radius;
            Error = error;
        }

        public bool Success
        {
            get
            {
                return Kind != GoalKind.None;
            }
        }
    }

    /// <summary>
    /// 按图形双目标查询轨迹中的最早触发时刻。
    /// </summary>
    public sealed class GoalJudge
    {
        /// <summary>
        /// 复用的零至四次多项式区间求根器。
        /// </summary>
        private readonly PolynomialRoots _solver = new PolynomialRoots();

        /// <summary>
        /// 当前条件边界的多项式系数，按常数项到高阶项排列。
        /// </summary>
        private readonly double[] _polynomial = new double[5];

        /// <summary>
        /// 轨迹端点与条件边界实根组成的时间分割点缓存。
        /// </summary>
        private readonly double[] _cuts = new double[128];

        /// <summary>
        /// 本次查询已写入的有效分割点数量。
        /// </summary>
        private int _cutCount;

        /// <summary>
        /// 当前查询的球体连续轨迹。
        /// </summary>
        private BallTrajectory _trajectory;

        /// <summary>
        /// 当前查询的图形条件与容差。
        /// </summary>
        private GeometryData _level;

        /// <summary>
        /// 条件查询类型，当前实现以零表示图形目标查询。
        /// </summary>
        private int _query;

        /// <summary>
        /// 判断当前球状态满足的目标类型。
        /// </summary>
        /// <param name="level">待判定图形的唯一条件及容差。</param>
        /// <param name="state">球状态。</param>
        /// <returns>内切、外接或未达成。</returns>
        public static GoalKind Check(GeometryData level, BallState state)
        {
            return Evaluate(level, state).Kind;
        }

        /// <summary>
        /// 比较当前半径与双目标半径，返回误差更小的有效目标。
        /// </summary>
        public static GoalMatch Evaluate(GeometryData level, BallState state)
        {
            bool inside = true;
            bool inner = true;
            bool outer = true;
            float orientation = Mathf.Sign(LevelData.Cross(level.B - level.A, level.C - level.A));

            for (int i = 0; i < 3; i++)
            {
                Vector2 vertex = level.Vertex(i);
                Vector2 edge = level.Vertex((i + 1) % 3) - vertex;
                float distance = orientation * LevelData.Cross(edge, state.Position - vertex) / edge.magnitude;
                inside &= distance >= -0.000001f;
                inner &= Mathf.Abs(distance - state.Radius) <= level.Tolerance + 0.000001f;
                outer &= Mathf.Abs(Vector2.Distance(state.Position, vertex) - state.Radius) <= level.Tolerance + 0.000001f;
            }

            float tolerance = level.Tolerance + 0.000001f;
            bool inscribed = inside && inner && Mathf.Abs(state.Radius - level.InscribedRadius) <= tolerance;
            bool circumscribed = outer && Mathf.Abs(state.Radius - level.CircumscribedRadius) <= tolerance;

            if (!inscribed && !circumscribed)
            {
                return new GoalMatch(GoalKind.None, Vector2.zero, 0, float.PositiveInfinity);
            }

            float inscribedError = Mathf.Abs(state.Radius - level.InscribedRadius);
            float circumscribedError = Mathf.Abs(state.Radius - level.CircumscribedRadius);

            if (inscribed && (!circumscribed || inscribedError <= circumscribedError))
            {
                return new GoalMatch(GoalKind.Inscribed, level.InscribedCenter, level.InscribedRadius, inscribedError);
            }

            return new GoalMatch(GoalKind.Circumscribed, level.CircumscribedCenter, level.CircumscribedRadius, circumscribedError);
        }

        /// <summary>
        /// 查询与当前半径误差最小的目标半径，供失败反馈显示建议尺寸。
        /// </summary>
        /// <param name="level">待显示反馈的图形。</param>
        /// <param name="radius">失败时的白球半径。</param>
        /// <returns>内切或外接目标中更接近当前半径的半径。</returns>
        public static float NearestTargetRadius(GeometryData level, float radius)
        {
            float inscribedError = Mathf.Abs(radius - level.InscribedRadius);
            float circumscribedError = Mathf.Abs(radius - level.CircumscribedRadius);

            return inscribedError <= circumscribedError ? level.InscribedRadius : level.CircumscribedRadius;
        }

        /// <summary>
        /// 查询轨迹中最早满足目标的时间。
        /// </summary>
        /// <param name="level">待查询图形的唯一条件及容差。</param>
        /// <param name="trajectory">待查询轨迹。</param>
        /// <returns>相对时间，未找到时为正无穷。</returns>
        public double FindGoal(GeometryData level, BallTrajectory trajectory)
        {
            Begin(level, trajectory, 0);

            float orientation = Mathf.Sign(LevelData.Cross(level.B - level.A, level.C - level.A));

            for (int i = 0; i < 3; i++)
            {
                Vector2 vertex = level.Vertex(i);
                Vector2 edge = level.Vertex((i + 1) % 3) - vertex;
                Vector2 normal = orientation * new Vector2(-edge.y, edge.x).normalized;
                SetQuadratic(Vector2.Dot(normal, trajectory.Position - vertex), Vector2.Dot(normal, trajectory.Velocity), 0.5 * Vector2.Dot(
                    normal,
                    trajectory.Acceleration));
                AddRoots(2);
                _polynomial[0] -= trajectory.Radius + level.Tolerance;
                _polynomial[1] -= trajectory.RadiusRate;
                AddRoots(2);
                _polynomial[0] += 2 * level.Tolerance;
                AddRoots(2);
                CirclePolynomial(vertex, trajectory.Radius + level.Tolerance, trajectory.RadiusRate);
                AddRoots(4);
                CirclePolynomial(vertex, trajectory.Radius - level.Tolerance, trajectory.RadiusRate);
                AddRoots(4);
            }

            SetQuadratic(trajectory.Radius - level.Tolerance, trajectory.RadiusRate, 0);
            AddRoots(1);

            return Earliest();
        }

        /// <summary>
        /// 初始化本次轨迹查询的类型与分割区间。
        /// </summary>
        /// <param name="level">本次查询的图形条件。</param>
        /// <param name="trajectory">待查询的连续轨迹。</param>
        /// <param name="query">查询类型，零表示图形目标条件。</param>
        private void Begin(GeometryData level, BallTrajectory trajectory, int query)
        {
            if (trajectory.Duration < 0 || double.IsNaN(trajectory.Duration))
            {
                throw new ArgumentException("无效查询区间。");
            }

            _level = level;
            _trajectory = trajectory;
            _query = query;
            _cutCount = 2;
            _cuts[0] = 0;
            _cuts[1] = trajectory.Duration;
        }

        /// <summary>
        /// 写入二次多项式系数并清空高阶项。
        /// </summary>
        /// <param name="c0">常数项系数。</param>
        /// <param name="c1">一次项系数。</param>
        /// <param name="c2">二次项系数。</param>
        private void SetQuadratic(double c0, double c1, double c2)
        {
            _polynomial[0] = c0;
            _polynomial[1] = c1;
            _polynomial[2] = c2;
            _polynomial[3] = 0;
            _polynomial[4] = 0;
        }

        /// <summary>
        /// 构造球体与指定圆的接触多项式。
        /// </summary>
        /// <param name="center">台面局部坐标中的目标圆心。</param>
        /// <param name="radius">轨迹起点处的比较半径。</param>
        /// <param name="rate">比较半径每秒变化量。</param>
        private void CirclePolynomial(Vector2 center, double radius, double rate)
        {
            double x = _trajectory.Position.x - center.x;
            double y = _trajectory.Position.y - center.y;
            double vx = _trajectory.Velocity.x;
            double vy = _trajectory.Velocity.y;
            double ax = 0.5 * _trajectory.Acceleration.x;
            double ay = 0.5 * _trajectory.Acceleration.y;
            _polynomial[0] = x * x + y * y - radius * radius;
            _polynomial[1] = 2 * (x * vx + y * vy - radius * rate);
            _polynomial[2] = vx * vx + vy * vy + 2 * (x * ax + y * ay) - rate * rate;
            _polynomial[3] = 2 * (vx * ax + vy * ay);
            _polynomial[4] = ax * ax + ay * ay;
        }

        /// <summary>
        /// 将有效实根加入轨迹分割点。
        /// </summary>
        /// <param name="degree">当前多项式次数，范围为零到四。</param>
        private void AddRoots(int degree)
        {
            int count = _solver.Find(_polynomial, degree, 0, _trajectory.Duration);

            for (int i = 0; i < count; i++)
            {
                if (_cutCount >= _cuts.Length)
                {
                    throw new InvalidOperationException("区间分割数量超出限制。");
                }

                _cuts[_cutCount++] = _solver.Root(i);
            }
        }

        /// <summary>
        /// 复核指定时刻是否满足当前查询条件。
        /// </summary>
        /// <param name="time">距当前轨迹起点的秒数。</param>
        /// <returns>该时刻的球状态是否满足当前图形条件。</returns>
        private bool Valid(double time)
        {
            BallState state = _trajectory.At(time);

            if (_query == 0)
            {
                return Check(_level, state) != GoalKind.None;
            }

            return false;
        }

        /// <summary>
        /// 按分割点查询最早有效时刻并细化边界。
        /// </summary>
        /// <returns>轨迹区间内的最早有效秒数；未找到时为正无穷。</returns>
        private double Earliest()
        {
            Array.Sort(_cuts, 0, _cutCount);

            for (int i = 0; i < _cutCount; i++)
            {
                double left = _cuts[i];

                if (Valid(left))
                {
                    return left;
                }

                if (i == _cutCount - 1 || _cuts[i + 1] - left < 1e-10)
                {
                    continue;
                }

                double right = (_cuts[i + 1] + left) * 0.5;

                if (!Valid(right))
                {
                    continue;
                }

                // 根分割后的有效开区间：细化因浮点复核产生的边界偏移。
                for (int j = 0; j < 40; j++)
                {
                    double mid = (left + right) * 0.5;

                    if (Valid(mid))
                    {
                        right = mid;
                    }
                    else
                    {
                        left = mid;
                    }
                }

                return right;
            }

            return double.PositiveInfinity;
        }
    }
}
