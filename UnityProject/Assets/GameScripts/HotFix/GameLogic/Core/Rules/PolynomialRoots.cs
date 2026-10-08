using System;

namespace GameLogic
{
    /// <summary>
    /// 零至四次多项式区间求根：导数分割单调区间，并检查重根。
    /// </summary>
    public sealed class PolynomialRoots
    {
        /// <summary>
        /// 按导数递归层级复用的多项式系数缓存。
        /// </summary>
        private readonly double[][] _coefficients = new double[5][];

        /// <summary>
        /// 按递归层级保存的区间实根缓存。
        /// </summary>
        private readonly double[][] _roots = new double[5][];

        /// <summary>
        /// 各递归层级的有效实根数量。
        /// </summary>
        private readonly int[] _counts = new int[5];

        /// <summary>
        /// 获取最近一次查询的根。
        /// </summary>
        /// <param name="index">根的索引。</param>
        /// <returns>对应的根。</returns>
        public double Root(int index)
        {
            return _roots[0][index];
        }

        /// <summary>
        /// 创建可复用的求根缓存。
        /// </summary>
        public PolynomialRoots()
        {
            for (int i = 0; i < 5; i++)
            {
                _coefficients[i] = new double[5];
                _roots[i] = new double[8];
            }
        }

        /// <summary>
        /// 查询指定区间内的实根。
        /// </summary>
        /// <param name="coefficients">从常数项开始的系数。</param>
        /// <param name="degree">多项式次数，范围为 0 至 4。</param>
        /// <param name="min">区间起点。</param>
        /// <param name="max">区间终点。</param>
        /// <returns>有效根数量。</returns>
        public int Find(
            double[] coefficients,
            int degree,
            double min,
            double max)
        {
            Array.Copy(coefficients, _coefficients[0], degree + 1);
            Solve(0, degree, min, max);

            return _counts[0];
        }

        /// <summary>
        /// 使用 Horner 法计算多项式值。
        /// </summary>
        /// <param name="c">从常数项开始的系数。</param>
        /// <param name="degree">多项式次数。</param>
        /// <param name="t">自变量值。</param>
        /// <returns>多项式的值。</returns>
        public static double Evaluate(double[] c, int degree, double t)
        {
            double value = c[degree];

            for (int i = degree - 1; i >= 0; i--)
            {
                value = value * t + c[i];
            }

            return value;
        }

        /// <summary>
        /// 使用导数根分割单调区间，递归求解实根。
        /// </summary>
        /// <param name="level">导数递归层级，同时索引复用缓存。</param>
        /// <param name="degree">本层多项式次数。</param>
        /// <param name="min">实根查询区间起点。</param>
        /// <param name="max">实根查询区间终点。</param>
        private void Solve(
            int level,
            int degree,
            double min,
            double max)
        {
            _counts[level] = 0;

            double[] c = _coefficients[level];

            while (degree > 0 && Math.Abs(c[degree]) < 1e-14)
            {
                degree--;
            }

            if (degree == 0)
            {
                return;
            }

            if (degree == 1)
            {
                double root = -c[0] / c[1];

                if (root >= min - 1e-10 && root <= max + 1e-10)
                {
                    Add(level, Math.Max(min, Math.Min(max, root)));
                }

                return;
            }

            for (int i = 1; i <= degree; i++)
            {
                _coefficients[level + 1][i - 1] = i * c[i];
            }

            Solve(level + 1, degree - 1, min, max);

            double left = min;
            Check(level, c, degree, left);

            for (int i = 0; i <= _counts[level + 1]; i++)
            {
                double right = i == _counts[level + 1] ? max : _roots[level + 1][i];
                double lv = Evaluate(c, degree, left);
                double rv = Evaluate(c, degree, right);

                if (lv * rv < 0)
                {
                    double a = left;
                    double b = right;

                    for (int j = 0; j < 64; j++)
                    {
                        double mid = (a + b) * 0.5;
                        double mv = Evaluate(c, degree, mid);

                        if (lv * mv <= 0)
                        {
                            b = mid;
                        }
                        else
                        {
                            a = mid;
                            lv = mv;
                        }
                    }

                    Add(level, (a + b) * 0.5);
                }

                Check(level, c, degree, right);
                left = right;
            }

            Array.Sort(_roots[level], 0, _counts[level]);
        }

        /// <summary>
        /// 依据系数规模判断端点是否为根。
        /// </summary>
        /// <param name="level">写入实根的缓存层级。</param>
        /// <param name="c">本层由低阶到高阶排列的系数。</param>
        /// <param name="degree">本层多项式次数。</param>
        /// <param name="time">需要检查的区间端点或导数根。</param>
        private void Check(
            int level,
            double[] c,
            int degree,
            double time)
        {
            double scale = 1;

            for (int i = 0; i <= degree; i++)
            {
                scale += Math.Abs(c[i]) * Math.Pow(Math.Max(1, Math.Abs(time)), i);
            }

            if (Math.Abs(Evaluate(c, degree, time)) <= 1e-10 * scale)
            {
                Add(level, time);
            }
        }

        /// <summary>
        /// 去除重复根后写入当前层级的缓存。
        /// </summary>
        /// <param name="level">写入实根的缓存层级。</param>
        /// <param name="time">需要去重后写入的候选根。</param>
        private void Add(int level, double time)
        {
            for (int i = 0; i < _counts[level]; i++)
            {
                if (Math.Abs(_roots[level][i] - time) < 1e-9)
                {
                    return;
                }
            }

            if (_counts[level] >= _roots[level].Length)
            {
                throw new InvalidOperationException("求根数量超出多项式次数。");
            }

            _roots[level][_counts[level]++] = time;
        }
    }
}
