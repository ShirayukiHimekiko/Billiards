using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 在白球前方绘制跟随瞄准方向的分段蓄力指示器。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ChargeGaugeView : MonoBehaviour
    {
        /// <summary>
        /// 力度显示的分段数量。
        /// </summary>
        [SerializeField]
        private int _segmentCount = 10;

        /// <summary>
        /// 单个分段沿瞄准方向的长度。
        /// </summary>
        [SerializeField]
        private float _segmentLength = .12f;

        /// <summary>
        /// 相邻分段之间的间隔。
        /// </summary>
        [SerializeField]
        private float _segmentGap = .04f;

        /// <summary>
        /// 单个分段垂直于瞄准方向的高度。
        /// </summary>
        [SerializeField]
        private float _segmentHeight = .18f;

        /// <summary>
        /// 指示器起点与白球表面的前向间距。
        /// </summary>
        [SerializeField]
        private float _forwardGap = .28f;

        /// <summary>
        /// 指示器与预测中心线之间的侧向距离。
        /// </summary>
        [SerializeField]
        private float _sideOffset = .28f;

        /// <summary>
        /// 承载运行时分段网格的组件。
        /// </summary>
        private MeshFilter _filter;

        /// <summary>
        /// 组件自行创建并负责销毁的运行时网格。
        /// </summary>
        private Mesh _mesh;

        /// <summary>
        /// 复用的分段顶点缓存。
        /// </summary>
        private readonly List<Vector3> _vertices = new List<Vector3>(40);

        /// <summary>
        /// 复用的分段顶点颜色缓存。
        /// </summary>
        private readonly List<Color> _colors = new List<Color>(40);

        /// <summary>
        /// 复用的分段三角形索引缓存。
        /// </summary>
        private readonly List<int> _indices = new List<int>(60);

        /// <summary>
        /// 已点亮分段使用的颜色。
        /// </summary>
        private Color _activeColor = Color.white;

        /// <summary>
        /// 未点亮分段使用的低透明度颜色。
        /// </summary>
        private Color _inactiveColor = new Color(1, 1, 1, .16f);

        /// <summary>
        /// 上次已经提交到网格的点亮段数。
        /// </summary>
        private int _lastActiveSegments = -1;

        /// <summary>
        /// 应用分段颜色并准备可复用网格。
        /// </summary>
        /// <param name="activeColor">已点亮分段的颜色。</param>
        public void SetStyle(Color activeColor)
        {
            _activeColor = activeColor;
            _inactiveColor = new Color(activeColor.r, activeColor.g, activeColor.b, activeColor.a * .22f);
            EnsureMesh();
            UpdateSegments(0, true);
        }

        /// <summary>
        /// 显示分段蓄力指示器并更新位置、方向和力度。
        /// </summary>
        /// <param name="position">白球在台面局部坐标中的中心。</param>
        /// <param name="direction">归一化瞄准方向。</param>
        /// <param name="radius">白球当前半径。</param>
        /// <param name="power">零到一的连续力度。</param>
        /// <param name="bounds">台面可玩区域。</param>
        public void Show(Vector2 position, Vector2 direction, float radius, float power, Rect bounds)
        {
            EnsureMesh();
            Place(position, direction, radius, bounds);
            UpdateSegments(Mathf.Clamp01(power));
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏分段蓄力指示器。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 创建固定拓扑的十段网格；后续只更新顶点颜色。
        /// </summary>
        private void EnsureMesh()
        {
            if (_mesh != null)
            {
                return;
            }

            _filter = GetComponent<MeshFilter>();
            _mesh = new Mesh
            {
                name = "BilliardsChargeGauge"
            };

            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();

            float halfHeight = _segmentHeight * .5f;

            for (int i = 0; i < _segmentCount; i++)
            {
                float start = i * (_segmentLength + _segmentGap);
                float end = start + _segmentLength;
                int vertexStart = _vertices.Count;
                _vertices.Add(new Vector3(start, -halfHeight));
                _vertices.Add(new Vector3(start, halfHeight));
                _vertices.Add(new Vector3(end, halfHeight));
                _vertices.Add(new Vector3(end, -halfHeight));

                for (int vertex = 0; vertex < 4; vertex++)
                {
                    _colors.Add(_inactiveColor);
                }

                _indices.Add(vertexStart);
                _indices.Add(vertexStart + 1);
                _indices.Add(vertexStart + 2);
                _indices.Add(vertexStart);
                _indices.Add(vertexStart + 2);
                _indices.Add(vertexStart + 3);
            }

            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_indices, 0);
            _mesh.RecalculateBounds();
            _filter.sharedMesh = _mesh;
        }

        /// <summary>
        /// 将指示器放到白球前方，并在靠近桌边时切换到预测线另一侧。
        /// </summary>
        /// <param name="position">白球中心。</param>
        /// <param name="direction">归一化瞄准方向。</param>
        /// <param name="radius">白球当前半径。</param>
        /// <param name="bounds">台面可玩区域。</param>
        private void Place(Vector2 position, Vector2 direction, float radius, Rect bounds)
        {
            Vector2 start = position + direction * (radius + _forwardGap);
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Vector2 preferredOffset = -normal * _sideOffset;
            Vector2 oppositeOffset = normal * _sideOffset;
            Vector2 offset = Fits(start, direction, preferredOffset, bounds)
                ? preferredOffset
                : Fits(start, direction, oppositeOffset, bounds)
                    ? oppositeOffset
                    : Vector2.zero;

            Vector2 anchor = start + offset;
            transform.localPosition = new Vector3(anchor.x, anchor.y, 0);
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        /// <summary>
        /// 判断指定侧向偏移下的指示器两端是否仍位于台面范围内。
        /// </summary>
        /// <param name="start">没有侧向偏移时的指示器起点。</param>
        /// <param name="direction">归一化瞄准方向。</param>
        /// <param name="offset">候选侧向偏移。</param>
        /// <param name="bounds">台面可玩区域。</param>
        /// <returns>指示器是否能完整容纳在台面内。</returns>
        private bool Fits(Vector2 start, Vector2 direction, Vector2 offset, Rect bounds)
        {
            float margin = _segmentHeight * .5f;
            Rect safeBounds = new Rect(
                bounds.xMin + margin,
                bounds.yMin + margin,
                bounds.width - margin * 2,
                bounds.height - margin * 2);
            Vector2 placedStart = start + offset;
            Vector2 placedEnd = placedStart + direction * TotalLength();

            return safeBounds.Contains(placedStart) && safeBounds.Contains(placedEnd);
        }

        /// <summary>
        /// 根据连续力度更新点亮段数，仅在跨段时提交颜色。
        /// </summary>
        /// <param name="power">零到一的连续力度。</param>
        /// <param name="force">是否忽略缓存并强制刷新。</param>
        private void UpdateSegments(float power, bool force = false)
        {
            int activeSegments = power <= 0
                ? 0
                : Mathf.Clamp(Mathf.CeilToInt(power * _segmentCount - .0001f), 1, _segmentCount);

            if (!force && activeSegments == _lastActiveSegments)
            {
                return;
            }

            _lastActiveSegments = activeSegments;

            for (int segment = 0; segment < _segmentCount; segment++)
            {
                Color color = segment < activeSegments ? _activeColor : _inactiveColor;
                int vertexStart = segment * 4;

                for (int vertex = 0; vertex < 4; vertex++)
                {
                    _colors[vertexStart + vertex] = color;
                }
            }

            _mesh.SetColors(_colors);
        }

        /// <summary>
        /// 获取全部分段在瞄准方向上的总长度。
        /// </summary>
        /// <returns>总长度。</returns>
        private float TotalLength()
        {
            return _segmentCount * _segmentLength + (_segmentCount - 1) * _segmentGap;
        }

        /// <summary>
        /// 释放组件在运行时创建的网格。
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
