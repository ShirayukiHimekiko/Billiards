using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 台面输入投影与预测表现，实体由专职管理器维护。
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        /// <summary>
        /// 按配置边界定位和缩放的台面节点。
        /// </summary>
        [SerializeField]
        private Transform _felt;

        /// <summary>
        /// 显示出杆方向与蓄力后移量的球杆线段。
        /// </summary>
        [SerializeField]
        private LineRenderer _cue;

        /// <summary>
        /// 绘制模拟预测轨迹的虚线组件。
        /// </summary>
        [SerializeField]
        private GeometryGraphic _prediction;

        /// <summary>
        /// 显示完整预测终点的圆点节点。
        /// </summary>
        [SerializeField]
        private Transform _predictionDot;

        /// <summary>
        /// 应用预测终点颜色的渲染组件。
        /// </summary>
        [SerializeField]
        private MeshRenderer _predictionDotRenderer;

        /// <summary>
        /// 由图形管理器复制的编辑器制作模板。
        /// </summary>
        [SerializeField]
        private GeometryView _geometryTemplate;

        /// <summary>
        /// 由球洞管理器复制的编辑器制作模板。
        /// </summary>
        [SerializeField]
        private PocketView _pocketTemplate;

        /// <summary>
        /// 由钥匙管理器复制的绑定洞连接线表现。
        /// </summary>
        [SerializeField]
        private KeyLinkView _keyLinks;

        /// <summary>
        /// 将 Unity 帧更新转发给会话的驱动组件。
        /// </summary>
        [SerializeField]
        private SessionRunner _runner;

        /// <summary>
        /// 本关边界、球参数及预测表现配置。
        /// </summary>
        private LevelData _data;

        /// <summary>
        /// 将屏幕鼠标位置投影到台面的正交主相机。
        /// </summary>
        private Camera _camera;

        /// <summary>
        /// 当前绑定的桌球会话，释放或解绑后为空。
        /// </summary>
        private Session _session;

        /// <summary>
        /// 负责图形进度表现与提示显示的管理器。
        /// </summary>
        private GeometryManager _geometries;

        /// <summary>
        /// 复用的预测轨迹分段及接触点缓存，坐标相对于台面。
        /// </summary>
        private readonly PredictionPath _predictionPath = new PredictionPath();

        /// <summary>
        /// 上次预测使用的瞄准方向。
        /// </summary>
        private Vector2 _lastDirection;

        /// <summary>
        /// 上次预测使用的归一化力度，负值表示缓存无效。
        /// </summary>
        private float _lastPower = -1;

        /// <summary>
        /// 上次预测对应的世界状态版本。
        /// </summary>
        private int _lastRevision = -1;

        /// <summary>
        /// 缓存预测是否完成，用于控制终点圆点显示。
        /// </summary>
        private bool _predictionComplete;

        /// <summary>
        /// 是否显示第一次撞库后的单次反射预测分段。
        /// </summary>
        private bool _predictionReflectionsVisible;

        /// <summary>
        /// 本关白球。
        /// </summary>
        public WhiteBall Ball
        {
            get;
            private set;
        }

        /// <summary>
        /// 会话帧更新驱动。
        /// </summary>
        public SessionRunner Runner => _runner;

        /// <summary>
        /// 世界相机。
        /// </summary>
        public Camera WorldCamera => _camera;

        /// <summary>
        /// 编辑器制作的图形模板。
        /// </summary>
        public GeometryView GeometryTemplate => _geometryTemplate;

        /// <summary>
        /// 编辑器制作的球洞模板。
        /// </summary>
        public PocketView PocketTemplate => _pocketTemplate;

        /// <summary>
        /// 准备台面及配置预测样式。
        /// </summary>
        /// <param name="data">本关台面边界及预测显示配置。</param>
        public void Prepare(LevelData data)
        {
            if (_felt == null
                || _cue == null
                || _prediction == null
                || _predictionDot == null
                || _predictionDotRenderer == null
                || _geometryTemplate == null
                || _pocketTemplate == null
                    || _keyLinks == null
                || _runner == null)
            {
                throw new InvalidOperationException("桌球 Prefab 缺少新版序列化绑定。");
            }

            _data = data;
            _camera = Camera.main;

            if (_camera == null || !_camera.orthographic)
            {
                throw new InvalidOperationException("桌球场景需要正交 Main Camera。");
            }

            _felt.localPosition = data.Bounds.center;
            _felt.localScale = new Vector3(data.Bounds.width, data.Bounds.height, 1);

            Vector4 c = data.PredictionStyle.Color;
            var color = new Color(c.x, c.y, c.z, c.w);
            _prediction.SetStyle(color, data.PredictionStyle.LineWidth);

            var properties = new MaterialPropertyBlock();
            properties.SetColor("_Color", color);
            _predictionDotRenderer.SetPropertyBlock(properties);
            _predictionDot.localScale = Vector3.one * data.PredictionStyle.DotDiameter;
            _keyLinks.Initialize(data);
            HideAim();
        }

        /// <summary>
        /// 由球管理器绑定加载的白球。
        /// </summary>
        /// <param name="ball">已由球管理器加载并初始化的唯一白球。</param>
        public void SetBall(WhiteBall ball)
        {
            Ball = ball;
        }

        /// <summary>
        /// 绑定本关会话及图形管理器。
        /// </summary>
        /// <param name="session">当前会话，解绑时传空。</param>
        /// <param name="geometries">本关图形管理器，解绑时传空。</param>
        public void Bind(Session session, GeometryManager geometries)
        {
            _session = session;
            _geometries = geometries;
            _predictionReflectionsVisible = false;
            _lastPower = -1;
        }

        /// <summary>
        /// 切换第一次撞库后的单次反射预测，并使当前瞄准缓存失效。
        /// </summary>
        /// <param name="visible">是否额外显示第一次撞库后的分段。</param>
        public void SetPredictionReflectionsVisible(bool visible)
        {
            _predictionReflectionsVisible = visible;
            _lastPower = -1;
        }

        /// <summary>
        /// 台面局部坐标转世界坐标。
        /// </summary>
        /// <param name="position">台面局部坐标。</param>
        /// <returns>变换后的 Unity 世界坐标。</returns>
        public Vector3 ToWorld(Vector2 position)
        {
            return transform.TransformPoint(position);
        }

        /// <summary>
        /// 将游戏画面内的鼠标投影到台面平面，允许坐标位于桌面外。
        /// </summary>
        /// <param name="screen">鼠标屏幕坐标。</param>
        /// <param name="position">投影得到的台面局部坐标。</param>
        /// <returns>鼠标是否位于相机视口内且成功投影。</returns>
        public bool TryProjectScreenPoint(Vector2 screen, out Vector2 position)
        {
            position = default;

            if (!_camera.pixelRect.Contains(screen))
            {
                return false;
            }

            Ray ray = _camera.ScreenPointToRay(screen);
            var plane = new Plane(transform.forward, transform.position);

            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            position = transform.InverseTransformPoint(ray.GetPoint(distance));

            return true;
        }

        /// <summary>
        /// 判断已投影的局部坐标是否在允许出杆的桌面内。
        /// </summary>
        /// <param name="position">台面局部坐标。</param>
        /// <returns>是否位于桌面配置边界内。</returns>
        public bool IsOnTable(Vector2 position)
        {
            return _data.Bounds.Contains(position);
        }

        /// <summary>
        /// 绘制随力度改变的反射虚线、停点圆点及球杆。
        /// </summary>
        /// <param name="position">台面局部坐标中的白球中心。</param>
        /// <param name="direction">台面局部坐标中的单位瞄准方向。</param>
        /// <param name="radius">白球当前半径，用于把球杆尖端、预测虚线起点与终点都外移到球面之外。</param>
        /// <param name="visible">是否显示球杆及预测；没有会话时隐藏。</param>
        /// <param name="power">零到一的归一化击球力度。</param>
        public void SetAim(
            Vector2 position,
            Vector2 direction,
            float radius,
            bool visible,
            float power = 0)
        {
            if (!visible || _session == null)
            {
                HideAim();

                return;
            }

            _prediction.gameObject.SetActive(true);
            _cue.gameObject.SetActive(true);

            int revision = _session.World.Revision;

            if (_lastPower != power || _lastDirection != direction || _lastRevision != revision)
            {
                _lastPower = power;
                _lastDirection = direction;
                _lastRevision = revision;

                bool complete = _session.World.Predict(direction, power, _predictionPath);
                _predictionComplete = complete;
                // 第一段记录白球圆心轨迹，起点需裁到当前球面；球体碰撞会在对应分段末端结束显示。
                _predictionPath.TrimStart(radius);
                int visibleSegments = GetVisiblePredictionSegmentCount();
                _prediction.SetDashedPath(
                    _predictionPath,
                    _data.PredictionStyle.DashLength,
                    _data.PredictionStyle.GapLength,
                    visibleSegments);
                _predictionDot.gameObject.SetActive(ShouldShowPredictionDot());
                _predictionDot.localPosition = _predictionPath.EndPosition;
            }
            else
            {
                _predictionDot.gameObject.SetActive(ShouldShowPredictionDot());
            }

            Vector2 tip = position - direction * (radius + .12f + .7f * power);
            _cue.SetPosition(0, tip - direction * 2.4f);
            _cue.SetPosition(1, tip);
        }

        /// <summary>
        /// 只在预测终点属于当前可见分段时显示终点圆点。
        /// </summary>
        /// <returns>是否显示完整预测终点。</returns>
        private bool ShouldShowPredictionDot()
        {
            return !_predictionPath.EndedByBallContact
                && _predictionComplete
                && _predictionPath.HasDrawableSegments
                && _predictionPath.SegmentCount <= GetVisiblePredictionSegmentCount();
        }

        /// <summary>
        /// 获取当前允许显示的白球预测分段数量。
        /// </summary>
        /// <returns>未启用道具时为一段，启用后为入射段加一段反射。</returns>
        private int GetVisiblePredictionSegmentCount()
        {
            return _predictionReflectionsVisible ? 2 : 1;
        }

        /// <summary>
        /// 隐藏瞄准表现。
        /// </summary>
        private void HideAim()
        {
            _prediction.gameObject.SetActive(false);
            _predictionDot.gameObject.SetActive(false);
            _cue.gameObject.SetActive(false);
        }

        /// <summary>
        /// 切换图形提示。
        /// </summary>
        /// <param name="visible">是否显示本关所有图形的条件圆。</param>
        public void SetHints(bool visible)
        {
            _geometries?.SetHints(visible);
        }

        /// <summary>
        /// 将钥匙连接线同步到真实世界的拾取与洞状态。
        /// </summary>
        /// <param name="world">当前真实物理世界。</param>
        public void SyncKeyLinks(PhysicsWorld world)
        {
            _keyLinks.Sync(world);
        }

        /// <summary>
        /// 切换显影道具的图形条件圆显示。
        /// </summary>
        public void SetReveal(bool visible)
        {
            _geometries?.SetReveal(visible);
        }

        /// <summary>
        /// 释放会话、表现及配置引用。
        /// </summary>
        public void Release()
        {
            _runner?.Bind(null);
            _keyLinks?.Release();
            _session = null;
            _geometries = null;
            _predictionReflectionsVisible = false;
            _data = null;
            _camera = null;
            Ball = null;
        }
    }
}
