using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 同步所有球共用的状态、滚动、阴影和碰撞体表现，专属能力由子类负责。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public abstract class BallBase : MonoBehaviour
    {
        /// <summary>
        /// 球面直接子节点的约定名称。
        /// </summary>
        private const string SURFACE_NODE_NAME = "Surface";

        /// <summary>
        /// 球体阴影直接子节点的约定名称。
        /// </summary>
        private const string SHADOW_NODE_NAME = "BallShadow";

        /// <summary>
        /// 更新滚动表现所需的最小位移平方，过滤浮点抖动。
        /// </summary>
        private const float MIN_ROLL_TRAVEL_SQUARED = 0.00000001f;

        /// <summary>
        /// 滚动角度换算使用的半径下限，避免除以接近零的值。
        /// </summary>
        private const float MIN_ROLL_RADIUS = 0.01f;

        /// <summary>
        /// 阴影宽度相对于球体直径的比例。
        /// </summary>
        private const float SHADOW_WIDTH_SCALE = 1.08f;

        /// <summary>
        /// 阴影高度相对于球体直径的比例。
        /// </summary>
        private const float SHADOW_HEIGHT_SCALE = 0.82f;

        /// <summary>
        /// 阴影水平偏移相对于球体半径的比例。
        /// </summary>
        private const float SHADOW_OFFSET_X = 0.08f;

        /// <summary>
        /// 阴影垂直偏移相对于球体半径的比例。
        /// </summary>
        private const float SHADOW_OFFSET_Y = -0.24f;

        /// <summary>
        /// 16BIT 表现使用的离散尺寸档位数；碰撞半径仍保持连续值。
        /// </summary>
        private const int DISPLAY_RADIUS_STEPS = 24;

        /// <summary>
        /// 球面材质滚动相位属性的缓存 ID。
        /// </summary>
        private static readonly int _rollPhaseId = Shader.PropertyToID("_RollPhase");

        /// <summary>
        /// 球面材质滚动方向属性的缓存 ID。
        /// </summary>
        private static readonly int _rollDirectionId = Shader.PropertyToID("_RollDirection");

        /// <summary>
        /// 16BIT 球体尺寸档位 Sprite，数组顺序从半径下限到上限。
        /// </summary>
        [SerializeField]
        private Sprite[] _displaySprites;

        /// <summary>
        /// 表达白球状态语义的像素覆盖层渲染器。
        /// </summary>
        [SerializeField]
        private SpriteRenderer _stateRenderer;

        /// <summary>
        /// 状态覆盖层 Sprite，索引由子类状态语义决定。
        /// </summary>
        [SerializeField]
        private Sprite[] _stateSprites;

        /// <summary>
        /// 缓存的球面渲染组件。
        /// </summary>
        private SpriteRenderer _renderer;

        /// <summary>
        /// 随模拟半径缩放的球面节点。
        /// </summary>
        private Transform _surface;

        /// <summary>
        /// 随模拟半径更新尺寸和偏移的阴影节点。
        /// </summary>
        private Transform _shadow;

        /// <summary>
        /// 镜像模拟半径的碰撞体，运动由共享世界求解。
        /// </summary>
        private CircleCollider2D _collider;

        /// <summary>
        /// 球体出生状态及类型参数。
        /// </summary>
        private BallPlacementData _data;

        /// <summary>
        /// 按实例更新球面材质参数的属性块。
        /// </summary>
        private MaterialPropertyBlock _properties;

        /// <summary>
        /// 由球心位移累计得到的滚动相位。
        /// </summary>
        private float _rollPhase;

        /// <summary>
        /// 当前表现对应的球状态。
        /// </summary>
        public BallState State
        {
            get;
            private set;
        }

        /// <summary>
        /// 子类根据自身规则提供的球面颜色。
        /// </summary>
        protected abstract Color SurfaceColor
        {
            get;
        }

        /// <summary>
        /// 球体表现相对于模拟半径的显示倍率，碰撞半径不随该倍率改变。
        /// </summary>
        protected virtual float DisplayScaleMultiplier
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// 自动获取并缓存球体内部组件，初始化专属能力及出生表现。
        /// </summary>
        /// <param name="data">已经通过关卡配置校验的球体摆放数据。</param>
        public void Initialize(BallPlacementData data)
        {
            _data = data;
            _surface = GetRequiredChild(SURFACE_NODE_NAME);
            _shadow = GetRequiredChild(SHADOW_NODE_NAME);
            _renderer = GetRequiredComponent<SpriteRenderer>(_surface.gameObject);
            _collider = GetRequiredComponent<CircleCollider2D>(gameObject);

            if (_displaySprites == null || _displaySprites.Length != DISPLAY_RADIUS_STEPS)
            {
                throw new InvalidOperationException($"球 {data.Id} Prefab 必须绑定 {DISPLAY_RADIUS_STEPS} 档 16BIT 球体 Sprite。");
            }

            for (int i = 0; i < _displaySprites.Length; i++)
            {
                if (_displaySprites[i] == null)
                {
                    throw new InvalidOperationException($"球 {data.Id} Prefab 的 16BIT 球体 Sprite 档位 {i} 未绑定。");
                }
            }

            if (_stateRenderer == null && RequiresStateOverlay)
            {
                throw new InvalidOperationException($"球 {data.Id} Prefab 缺少状态覆盖层 Renderer。");
            }

            if (RequiresStateOverlay && (_stateSprites == null || _stateSprites.Length != 4))
            {
                throw new InvalidOperationException($"球 {data.Id} Prefab 必须绑定 4 档状态覆盖层 Sprite。");
            }

            _properties = new MaterialPropertyBlock();
            InitializeFeatures(data.Config);
            ResetBall();
        }

        /// <summary>
        /// 只读取世界快照，先同步专属能力，再更新共用球体表现。
        /// </summary>
        /// <param name="ball">与当前球对应的模拟快照。</param>
        public void SyncState(SimulatedBall ball)
        {
            SyncFeatures(ball);
            ApplyState(ball.State);
        }

        /// <summary>
        /// 将位置和半径应用到共用表现，不参与世界运动求解。
        /// </summary>
        /// <param name="state">世界模拟提供的球体状态。</param>
        public void ApplyState(BallState state)
        {
            Vector2 travel = state.Position - State.Position;

            if (travel.sqrMagnitude > MIN_ROLL_TRAVEL_SQUARED)
            {
                float averageRadius = (state.Radius + State.Radius) * 0.5f;
                _rollPhase = Mathf.Repeat(_rollPhase + travel.magnitude / Mathf.Max(averageRadius, MIN_ROLL_RADIUS), Mathf.PI * 2);

                Vector2 direction = travel.normalized;
                _properties.SetVector(_rollDirectionId, new Vector4(direction.x, direction.y, 0, 0));
            }

            State = state;
            transform.localPosition = state.Position;

            int displayIndex = QuantizeDisplayIndex(state.Radius);
            float displayRadius = GetDisplayRadius(displayIndex) * DisplayScaleMultiplier;
            float diameter = 2 * displayRadius;
            _renderer.sprite = _displaySprites[displayIndex];
            _surface.localScale = Vector3.one;
            _shadow.localScale = new Vector3(diameter * SHADOW_WIDTH_SCALE, diameter * SHADOW_HEIGHT_SCALE, 1);
            _shadow.localPosition = new Vector3(displayRadius * SHADOW_OFFSET_X, displayRadius * SHADOW_OFFSET_Y, 0);
            _collider.radius = state.Radius;
            _renderer.color = SurfaceColor;
            ApplyStateOverlay(diameter);
            _properties.SetFloat(_rollPhaseId, _rollPhase);
            _renderer.SetPropertyBlock(_properties);
        }

        /// <summary>
        /// 子类是否需要绑定并显示状态覆盖层。
        /// </summary>
        protected virtual bool RequiresStateOverlay
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 当前状态覆盖层索引；负值表示隐藏覆盖层。
        /// </summary>
        protected virtual int StateOverlayIndex
        {
            get
            {
                return -1;
            }
        }

        /// <summary>
        /// 应用子类提供的状态覆盖层，不参与物理状态计算。
        /// </summary>
        private void ApplyStateOverlay(float diameter)
        {
            if (_stateRenderer == null)
            {
                return;
            }

            int index = StateOverlayIndex;
            _stateRenderer.sprite = index >= 0 && index < _stateSprites.Length ? _stateSprites[index] : null;
            _stateRenderer.enabled = _stateRenderer.sprite != null;
            if (_stateRenderer.sprite != null)
            {
                _stateRenderer.transform.localScale = Vector3.one * (diameter / _stateRenderer.sprite.bounds.size.x);
            }
            _stateRenderer.color = Color.white;
        }

        /// <summary>
        /// 将连续模拟半径映射为固定数量的像素表现档位索引。
        /// </summary>
        private int QuantizeDisplayIndex(float radius)
        {
            float min = _data.Config.RadiusMin;
            float max = _data.Config.RadiusMax;

            if (max <= min)
            {
                return 0;
            }

            float normalized = Mathf.InverseLerp(min, max, Mathf.Clamp(radius, min, max));
            return Mathf.Clamp(Mathf.RoundToInt(normalized * (DISPLAY_RADIUS_STEPS - 1)), 0, DISPLAY_RADIUS_STEPS - 1);
        }

        /// <summary>
        /// 将档位索引映射回用于阴影和状态覆盖层的离散半径。
        /// </summary>
        private float GetDisplayRadius(int index)
        {
            float min = _data.Config.RadiusMin;
            float max = _data.Config.RadiusMax;

            if (max <= min)
            {
                return min;
            }

            return Mathf.Lerp(min, max, index / (float)(DISPLAY_RADIUS_STEPS - 1));
        }

        /// <summary>
        /// 重置专属能力并恢复配置出生状态。
        /// </summary>
        public void ResetBall()
        {
            ResetFeatures();
            _rollPhase = 0;
            State = new BallState
            {
                Position = _data.Position,
                Radius = _data.Radius
            };
            _properties.SetVector(_rollDirectionId, new Vector4(1, 0, 0, 0));
            ApplyState(State);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 释放专属能力并解除球体配置引用。
        /// </summary>
        public void Release()
        {
            ReleaseFeatures();
            _data = null;
        }

        /// <summary>
        /// 在初始化边界获取必需组件，缺失时报告球体及节点信息。
        /// </summary>
        /// <typeparam name="T">必需的组件类型。</typeparam>
        /// <param name="owner">组件所在的球体内部节点。</param>
        /// <returns>获取到的组件，供调用方缓存。</returns>
        protected T GetRequiredComponent<T>(GameObject owner)
            where T : Component
        {
            T component = owner.GetComponent<T>();

            if (component == null)
            {
                throw new InvalidOperationException($"球 {_data.Id} Prefab 节点 {owner.name} 缺少 {typeof(T).Name}。");
            }

            return component;
        }

        /// <summary>
        /// 获取并初始化子类专属能力；没有专属能力时无需处理。
        /// </summary>
        /// <param name="config">当前球体的配置。</param>
        protected virtual void InitializeFeatures(BallConfigData config)
        {
        }

        /// <summary>
        /// 从世界快照读取子类专属状态；不得修改快照。
        /// </summary>
        /// <param name="ball">当前球体的模拟快照。</param>
        protected virtual void SyncFeatures(SimulatedBall ball)
        {
        }

        /// <summary>
        /// 重置子类专属能力。
        /// </summary>
        protected virtual void ResetFeatures()
        {
        }

        /// <summary>
        /// 释放子类专属能力。
        /// </summary>
        protected virtual void ReleaseFeatures()
        {
        }

        /// <summary>
        /// 按球体 Prefab 结构约定获取子节点，缺失时在初始化阶段报错。
        /// </summary>
        /// <param name="nodeName">球体根节点下的直接子节点名称。</param>
        /// <returns>获取到的子节点。</returns>
        private Transform GetRequiredChild(string nodeName)
        {
            Transform child = transform.Find(nodeName);

            if (child == null)
            {
                throw new InvalidOperationException($"球 {_data.Id} Prefab 缺少子节点 {nodeName}。");
            }

            return child;
        }
    }
}
