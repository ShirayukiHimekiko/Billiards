using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 在组件局部坐标中构建台面线条网格，通过 Transform 显示到世界空间。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class GeometryGraphic : MonoBehaviour
    {
        /// <summary>
        /// 承载运行时绘制网格的组件。
        /// </summary>
        [SerializeField]
        private MeshFilter _filter;

        /// <summary>
        /// 当前绘制的线条颜色。
        /// </summary>
        [SerializeField]
        private Color _color = Color.white;

        /// <summary>
        /// 线条在组件局部坐标中的宽度。
        /// </summary>
        [SerializeField]
        private float _lineWidth = .035f;

        /// <summary>
        /// 组件自行创建并负责销毁的运行时网格。
        /// </summary>
        private Mesh _mesh;

        /// <summary>
        /// 复用的线条及填充顶点缓存。
        /// </summary>
        private readonly List<Vector3> _vertices = new List<Vector3>(192);

        /// <summary>
        /// 与顶点对应的颜色缓存。
        /// </summary>
        private readonly List<Color> _colors = new List<Color>(192);

        /// <summary>
        /// 将线段四边形及填充区域组成三角形的索引缓存。
        /// </summary>
        private readonly List<int> _indices = new List<int>(288);

        /// <summary>
        /// 应用配置中的线条颜色和宽度。
        /// </summary>
        /// <param name="color">线条及填充使用的颜色。</param>
        /// <param name="width">组件局部坐标中的线条宽度。</param>
        public void SetStyle(Color color, float width)
        {
            _color = color;
            _lineWidth = width;
        }

        /// <summary>
        /// 沿指定数量的预测分段连续分配虚线，分段之间不连线且不重置虚线间距。
        /// </summary>
        /// <param name="path">包含多个独立显示分段的预测路径。</param>
        /// <param name="dash">每段实线长度。</param>
        /// <param name="gap">相邻实线之间的间隙长度。</param>
        /// <param name="maxSegments">最多绘制的预测分段数量。</param>
        public void SetDashedPath(PredictionPath path, float dash, float gap, int maxSegments)
        {
            Clear();

            float phase = 0;
            float period = dash + gap;
            int visibleSegmentCount = Mathf.Min(path.SegmentCount, maxSegments);

            for (int segmentIndex = 0; segmentIndex < visibleSegmentCount; segmentIndex++)
            {
                PredictionPathSegment segment = path.GetSegment(segmentIndex);
                IReadOnlyList<Vector2> points = segment.Points;

                for (int i = 1; i < points.Count; i++)
                {
                    Vector2 delta = points[i] - points[i - 1];
                    float length = delta.magnitude;

                    if (length < .000001f)
                    {
                        continue;
                    }

                    Vector2 direction = delta / length;
                    float travelled = 0;

                    while (travelled < length - .000001f)
                    {
                        bool draw = phase < dash;
                        float span = Mathf.Min(length - travelled, (draw ? dash : period) - phase);

                        if (draw)
                        {
                            Line(points[i - 1] + direction * travelled, points[i - 1] + direction * (travelled + span));
                        }

                        travelled += span;
                        phase += span;

                        if (phase >= period - .000001f)
                        {
                            phase = 0;
                        }
                        else if (phase >= dash - .000001f && draw)
                        {
                            phase = dash;
                        }
                    }
                }

                if (segment.EnsureStartVisible)
                {
                    DrawVisibleStart(points, dash);
                }

                if (segment.EnsureEndVisible)
                {
                    DrawVisibleEnd(points, dash);
                }
            }

            IReadOnlyList<PredictionRailContact> railContacts = path.RailContacts;

            for (int i = 0; i < railContacts.Count; i++)
            {
                PredictionRailContact contact = railContacts[i];

                if (contact.SegmentIndex >= visibleSegmentCount)
                {
                    continue;
                }

                DrawOutlineCircle(contact.Center, contact.Radius);
            }

            IReadOnlyList<PredictionBallContact> ballContacts = path.BallContacts;

            for (int i = 0; i < ballContacts.Count; i++)
            {
                PredictionBallContact contact = ballContacts[i];

                if (contact.SegmentIndex >= visibleSegmentCount)
                {
                    continue;
                }

                DrawOutlineCircle(contact.Center, contact.Radius);

                if (contact.TargetDirection.sqrMagnitude > 1e-12f)
                {
                    Line(
                        contact.TargetCenter,
                        contact.TargetCenter + contact.TargetDirection * (contact.TargetRadius * 2));
                }
            }

            Commit();
        }

        /// <summary>
        /// 绘制预测接触瞬间的空心球体轮廓。
        /// </summary>
        /// <param name="center">预测球心位置。</param>
        /// <param name="radius">预测球体半径。</param>
        private void DrawOutlineCircle(Vector2 center, float radius)
        {
            const int SEGMENT_COUNT = 32;
            float step = Mathf.PI * 2 / SEGMENT_COUNT;
            Vector2 previous = center + Vector2.right * radius;

            for (int i = 1; i <= SEGMENT_COUNT; i++)
            {
                float angle = step * i;
                Vector2 current = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Line(previous, current);
                previous = current;
            }
        }

        /// <summary>
        /// 在库边反射后的分段起点补一小段实线，避免接缝恰好落在虚线间隙中。
        /// </summary>
        /// <param name="points">以反射接缝开始的路径点。</param>
        /// <param name="dash">配置的单段实线长度。</param>
        private void DrawVisibleStart(IReadOnlyList<Vector2> points, float dash)
        {
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 delta = points[i] - points[i - 1];
                float length = delta.magnitude;

                if (length <= .000001f)
                {
                    continue;
                }

                Vector2 start = points[i - 1];
                Vector2 direction = delta / length;
                Line(start, start + direction * Mathf.Min(dash, length));

                return;
            }
        }

        /// <summary>
        /// 在预测接触点前补一小段实线，避免接触点恰好落在虚线间隙中。
        /// </summary>
        /// <param name="points">以预测接触点结束的路径点。</param>
        /// <param name="dash">配置的单段实线长度。</param>
        private void DrawVisibleEnd(IReadOnlyList<Vector2> points, float dash)
        {
            for (int i = points.Count - 1; i > 0; i--)
            {
                Vector2 delta = points[i] - points[i - 1];
                float length = delta.magnitude;

                if (length <= .000001f)
                {
                    continue;
                }

                Vector2 end = points[i];
                Vector2 direction = delta / length;
                Line(end - direction * Mathf.Min(dash, length), end);

                return;
            }
        }

        /// <summary>
        /// 绘制目标三角形及淡色填充。
        /// </summary>
        /// <param name="a">顶点 A。</param>
        /// <param name="b">顶点 B。</param>
        /// <param name="c">顶点 C。</param>
        public void SetTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            Clear();

            Color fill = _color;
            fill.a *= .07f;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _colors.Add(fill);
            _colors.Add(fill);
            _colors.Add(fill);
            _indices.Add(0);
            _indices.Add(1);
            _indices.Add(2);
            Line(a, b);
            Line(b, c);
            Line(c, a);
            Commit();
        }

        /// <summary>
        /// 绘制目标圆的虚线。
        /// </summary>
        /// <param name="radius">圆半径。</param>
        public void SetCircle(float radius)
        {
            Clear();

            for (int i = 0; i < 96; i += 2)
            {
                float a = i * Mathf.PI * 2 / 96;
                float b = (i + 1) * Mathf.PI * 2 / 96;
                Line(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius);
            }

            Commit();
        }

        /// <summary>
        /// 清空下一次绘制使用的顶点缓存。
        /// </summary>
        private void Clear()
        {
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();
        }

        /// <summary>
        /// 将具有宽度的线段写入三角形网格缓存。
        /// </summary>
        /// <param name="a">组件局部坐标中的线段起点。</param>
        /// <param name="b">组件局部坐标中的线段终点。</param>
        private void Line(Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            Vector2 n = new Vector2(-d.y, d.x).normalized * (_lineWidth / 2);
            int start = _vertices.Count;
            _vertices.Add(a - n);
            _vertices.Add(a + n);
            _vertices.Add(b + n);
            _vertices.Add(b - n);

            for (int i = 0; i < 4; i++)
            {
                _colors.Add(_color);
            }

            _indices.Add(start);
            _indices.Add(start + 1);
            _indices.Add(start + 2);
            _indices.Add(start);
            _indices.Add(start + 2);
            _indices.Add(start + 3);
        }

        /// <summary>
        /// 将绘制缓存提交给台面网格。
        /// </summary>
        private void Commit()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh
                {
                    name = "BilliardsTargetGeometry"
                };
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_indices, 0);
            _mesh.RecalculateBounds();
            _filter.sharedMesh = _mesh;
        }

        /// <summary>
        /// 销毁组件持有的运行时网格。
        /// </summary>
        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }
}
