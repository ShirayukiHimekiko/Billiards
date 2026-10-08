using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 白球预测中的首次球体接触，用于显示 Ghost Ball 与被撞球出射方向。
    /// </summary>
    public readonly struct PredictionBallContact
    {
        /// <summary>
        /// 碰撞瞬间的白球球心。
        /// </summary>
        public readonly Vector2 Center;

        /// <summary>
        /// 碰撞瞬间的白球半径。
        /// </summary>
        public readonly float Radius;

        /// <summary>
        /// 碰撞瞬间的被撞球球心。
        /// </summary>
        public readonly Vector2 TargetCenter;

        /// <summary>
        /// 碰撞瞬间的被撞球半径。
        /// </summary>
        public readonly float TargetRadius;

        /// <summary>
        /// 被撞球经过冲量响应后的出射单位方向；无有效速度时为零向量。
        /// </summary>
        public readonly Vector2 TargetDirection;

        /// <summary>
        /// 产生本次球体接触的白球预测分段索引。
        /// </summary>
        public readonly int SegmentIndex;

        /// <summary>
        /// 创建一次预测球体接触记录。
        /// </summary>
        /// <param name="center">碰撞瞬间的白球球心。</param>
        /// <param name="radius">碰撞瞬间的白球半径。</param>
        /// <param name="targetCenter">碰撞瞬间的被撞球球心。</param>
        /// <param name="targetRadius">碰撞瞬间的被撞球半径。</param>
        /// <param name="targetVelocity">冲量响应后的被撞球速度。</param>
        /// <param name="segmentIndex">产生本次接触的白球预测分段索引。</param>
        public PredictionBallContact(
            Vector2 center,
            float radius,
            Vector2 targetCenter,
            float targetRadius,
            Vector2 targetVelocity,
            int segmentIndex)
        {
            Center = center;
            Radius = radius;
            TargetCenter = targetCenter;
            TargetRadius = targetRadius;
            TargetDirection = targetVelocity.sqrMagnitude > 1e-12f ? targetVelocity.normalized : Vector2.zero;
            SegmentIndex = segmentIndex;
        }
    }

    /// <summary>
    /// 白球预测中的一次库边接触，用于显示撞库瞬间的球体轮廓。
    /// </summary>
    public readonly struct PredictionRailContact
    {
        /// <summary>
        /// 撞库瞬间的球心位置。
        /// </summary>
        public readonly Vector2 Center;

        /// <summary>
        /// 撞库瞬间的白球半径。
        /// </summary>
        public readonly float Radius;

        /// <summary>
        /// 从库边指向台面内部的单位法线。
        /// </summary>
        public readonly Vector2 Normal;

        /// <summary>
        /// 产生本次撞库接触的预测分段索引。
        /// </summary>
        public readonly int SegmentIndex;

        /// <summary>
        /// 撞库瞬间球面与库边相切的位置。
        /// </summary>
        public Vector2 ContactPoint => Center - Normal * Radius;

        /// <summary>
        /// 创建一次预测库边接触记录。
        /// </summary>
        /// <param name="center">撞库瞬间的球心位置。</param>
        /// <param name="radius">撞库瞬间的白球半径。</param>
        /// <param name="normal">从库边指向台面内部的单位法线。</param>
        /// <param name="segmentIndex">产生本次接触的预测分段索引。</param>
        public PredictionRailContact(Vector2 center, float radius, Vector2 normal, int segmentIndex)
        {
            Center = center;
            Radius = radius;
            Normal = normal;
            SegmentIndex = segmentIndex;
        }
    }

    /// <summary>
    /// 一段连续的白球预测显示路径；不同段之间不生成连接线。
    /// </summary>
    public sealed class PredictionPathSegment
    {
        /// <summary>
        /// 当前分段中的有序局部坐标点。
        /// </summary>
        private readonly List<Vector2> _points = new List<Vector2>(256);

        /// <summary>
        /// 当前分段中的有序局部坐标点。
        /// </summary>
        public IReadOnlyList<Vector2> Points => _points;

        /// <summary>
        /// 起点是否为反射接缝，需要保证虚线从该点可见。
        /// </summary>
        public bool EnsureStartVisible
        {
            get;
            private set;
        }

        /// <summary>
        /// 末端是否为预测接触点，需要保证虚线在该点可见。
        /// </summary>
        public bool EnsureEndVisible
        {
            get;
            private set;
        }

        /// <summary>
        /// 清空并复用当前分段。
        /// </summary>
        /// <param name="start">新分段起点。</param>
        /// <param name="ensureStartVisible">是否保证虚线从分段起点可见。</param>
        internal void Reset(Vector2 start, bool ensureStartVisible)
        {
            _points.Clear();
            _points.Add(start);
            EnsureStartVisible = ensureStartVisible;
            EnsureEndVisible = false;
        }

        /// <summary>
        /// 追加非重复路径点。
        /// </summary>
        /// <param name="point">待追加的局部坐标点。</param>
        internal void Add(Vector2 point)
        {
            if (_points.Count == 0 || (_points[_points.Count - 1] - point).sqrMagnitude > 1e-12f)
            {
                _points.Add(point);
            }
        }

        /// <summary>
        /// 标记当前分段以需要保证可见的预测接触点结束。
        /// </summary>
        internal void MarkEndVisible()
        {
            EnsureEndVisible = true;
        }

        /// <summary>
        /// 仅沿当前分段裁掉起点距离，不跨越碰撞后的下一分段。
        /// </summary>
        /// <param name="distance">需要裁掉的路径长度。</param>
        internal void TrimStart(float distance)
        {
            float remaining = distance;

            while (remaining > 0 && _points.Count > 1)
            {
                Vector2 delta = _points[1] - _points[0];
                float length = delta.magnitude;

                if (length <= .000001f)
                {
                    _points.RemoveAt(1);

                    continue;
                }

                if (length >= remaining)
                {
                    _points[0] += delta / length * remaining;

                    return;
                }

                remaining -= length;
                _points.RemoveAt(0);
            }

            if (remaining > 0)
            {
                _points.Clear();
            }
        }
    }

    /// <summary>
    /// 可复用的白球预测结果，保存互不连接的显示分段及真实球心终点。
    /// </summary>
    public sealed class PredictionPath
    {
        /// <summary>
        /// 已创建的分段缓存，重复预测时复用。
        /// </summary>
        private readonly List<PredictionPathSegment> _segments = new List<PredictionPathSegment>(8);

        /// <summary>
        /// 本次预测中的库边接触记录，重复预测时复用集合。
        /// </summary>
        private readonly List<PredictionRailContact> _railContacts = new List<PredictionRailContact>(8);

        /// <summary>
        /// 本次预测中的首次球体接触记录，重复预测时复用集合。
        /// </summary>
        private readonly List<PredictionBallContact> _ballContacts = new List<PredictionBallContact>(1);

        /// <summary>
        /// 本次预测实际使用的分段数量。
        /// </summary>
        private int _segmentCount;

        /// <summary>
        /// 当前接受新采样点的分段；结束后为空。
        /// </summary>
        private PredictionPathSegment _current;

        /// <summary>
        /// 本次预测实际使用的分段数量。
        /// </summary>
        public int SegmentCount => _segmentCount;

        /// <summary>
        /// 本次预测中的所有库边接触记录。
        /// </summary>
        public IReadOnlyList<PredictionRailContact> RailContacts => _railContacts;

        /// <summary>
        /// 本次预测中可显示的球体接触记录。
        /// </summary>
        public IReadOnlyList<PredictionBallContact> BallContacts => _ballContacts;

        /// <summary>
        /// 白球可见轨迹是否已在首次球体碰撞处终止。
        /// </summary>
        public bool EndedByBallContact
        {
            get;
            private set;
        }

        /// <summary>
        /// 预测结束时白球的真实球心位置，不受接触点显示延伸影响。
        /// </summary>
        public Vector2 EndPosition
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否至少存在一段可以绘制的路径。
        /// </summary>
        public bool HasDrawableSegments
        {
            get
            {
                for (int i = 0; i < _segmentCount; i++)
                {
                    if (_segments[i].Points.Count > 1)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 获取本次预测中的指定显示分段。
        /// </summary>
        /// <param name="index">从零开始的分段索引。</param>
        /// <returns>指定预测显示分段。</returns>
        public PredictionPathSegment GetSegment(int index)
        {
            return _segments[index];
        }

        /// <summary>
        /// 清空上次结果并以白球当前球心开始第一段。
        /// </summary>
        /// <param name="start">白球当前球心位置。</param>
        internal void Reset(Vector2 start)
        {
            _segmentCount = 0;
            _current = null;
            _railContacts.Clear();
            _ballContacts.Clear();
            EndedByBallContact = false;
            EndPosition = start;
            BeginSegment(start);
        }

        /// <summary>
        /// 记录撞库瞬间的球体状态，供表现层绘制接触预览圆。
        /// </summary>
        /// <param name="center">撞库瞬间的球心位置。</param>
        /// <param name="radius">撞库瞬间的白球半径。</param>
        /// <param name="normal">从库边指向台面内部的单位法线。</param>
        internal void AddRailContact(Vector2 center, float radius, Vector2 normal)
        {
            _railContacts.Add(new PredictionRailContact(center, radius, normal, _segmentCount - 1));
        }

        /// <summary>
        /// 记录首次球体碰撞的 Ghost Ball 与被撞球出射方向。
        /// </summary>
        /// <param name="center">碰撞瞬间的白球球心。</param>
        /// <param name="radius">碰撞瞬间的白球半径。</param>
        /// <param name="targetCenter">碰撞瞬间的被撞球球心。</param>
        /// <param name="targetRadius">碰撞瞬间的被撞球半径。</param>
        /// <param name="targetVelocity">冲量响应后的被撞球速度。</param>
        internal void AddBallContact(
            Vector2 center,
            float radius,
            Vector2 targetCenter,
            float targetRadius,
            Vector2 targetVelocity)
        {
            _ballContacts.Add(new PredictionBallContact(
                center,
                radius,
                targetCenter,
                targetRadius,
                targetVelocity,
                _segmentCount - 1));
            EndedByBallContact = true;
        }

        /// <summary>
        /// 开始一段不与上一段连接的新路径。
        /// </summary>
        /// <param name="start">新分段起点。</param>
        /// <param name="ensureStartVisible">是否保证虚线从分段起点可见。</param>
        internal void BeginSegment(Vector2 start, bool ensureStartVisible = false)
        {
            if (_segmentCount == _segments.Count)
            {
                _segments.Add(new PredictionPathSegment());
            }

            _current = _segments[_segmentCount++];
            _current.Reset(start, ensureStartVisible);
        }

        /// <summary>
        /// 向当前显示分段追加采样点。
        /// </summary>
        /// <param name="point">待追加的白球球心或接触点。</param>
        internal void AddPoint(Vector2 point)
        {
            _current?.Add(point);
        }

        /// <summary>
        /// 结束当前分段。
        /// </summary>
        /// <param name="ensureEndVisible">是否保证末端接触点可见。</param>
        internal void FinishSegment(bool ensureEndVisible)
        {
            if (_current == null)
            {
                return;
            }

            if (ensureEndVisible)
            {
                _current.MarkEndVisible();
            }

            _current = null;
        }

        /// <summary>
        /// 更新预测结束时白球的真实球心位置。
        /// </summary>
        /// <param name="position">当前预测球心位置。</param>
        internal void SetEndPosition(Vector2 position)
        {
            EndPosition = position;
        }

        /// <summary>
        /// 把第一段起点沿轨迹裁到当前白球表面，不跨越碰撞后的分段。
        /// </summary>
        /// <param name="radius">白球当前半径。</param>
        public void TrimStart(float radius)
        {
            if (_segmentCount > 0)
            {
                _segments[0].TrimStart(radius);
            }
        }
    }
}
