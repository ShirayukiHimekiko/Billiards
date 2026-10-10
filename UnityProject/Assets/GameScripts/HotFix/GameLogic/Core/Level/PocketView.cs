using System.Collections.Generic;
using System;
using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 球洞表现，状态样式由台面表统一配置。
    /// </summary>
    public sealed class PocketView : MonoBehaviour
    {
        /// <summary>
        /// 应用球洞状态颜色的渲染组件。
        /// </summary>
        [SerializeField]
        private MeshRenderer _renderer;

        /// <summary>
        /// 球洞关闭时显示的盖板节点。
        /// </summary>
        [SerializeField]
        private Transform _cap;

        /// <summary>
        /// 四种洞态共用的像素图案渲染器。
        /// </summary>
        [SerializeField]
        private SpriteRenderer _statePattern;

        /// <summary>
        /// 洞态图案按 Locked、Unlocked、Occupied、Disabled 顺序绑定。
        /// </summary>
        [SerializeField]
        private Sprite[] _stateSprites;

        /// <summary>
        /// 按实例更新材质参数的可复用属性块。
        /// </summary>
        private MaterialPropertyBlock _properties;

        /// <summary>
        /// 配置中的四状态显示样式。
        /// </summary>
        private IReadOnlyList<PocketStyle> _styles;

        /// <summary>
        /// 上次应用的状态，避免重复刷新表现。
        /// </summary>
        private PocketState? _last;

        /// <summary>
        /// 配置洞位置、尺寸及状态样式。
        /// </summary>
        /// <param name="data">球洞局部位置、捕获半径及初始状态。</param>
        /// <param name="styles">四种球洞状态对应的颜色与盖板样式。</param>
        public void Initialize(PocketPlacementData data, IReadOnlyList<PocketStyle> styles)
        {
            _properties = new MaterialPropertyBlock();
            _styles = styles;

            if (_renderer == null || _cap == null || _statePattern == null || _stateSprites == null || _stateSprites.Length != 4)
            {
                throw new InvalidOperationException("球洞 Prefab 必须绑定颜色 Renderer、盖板、四态像素图案和对应 Sprite 数组。");
            }

            transform.localPosition = data.Position;
            transform.localScale = Vector3.one * (data.CaptureRadius * 2);
            SetState(data.InitialState);
        }

        /// <summary>
        /// 更新四种状态表现。
        /// </summary>
        /// <param name="state">要显示的当前模拟球洞状态。</param>
        public void SetState(PocketState state)
        {
            if (_last == state)
            {
                return;
            }

            _last = state;

            foreach (var style in _styles)
            {
                if (style.State == state)
                {
                    Vector4 c = style.Color;
                    _properties.SetColor("_Color", new Color(c.x, c.y, c.z, c.w));
                    break;
                }
            }

            ApplyStatePattern(state);
            _statePattern.sprite = _stateSprites[GetStatePatternIndex(state)];
            _statePattern.enabled = true;
            _renderer.SetPropertyBlock(_properties);
        }

        /// <summary>
        /// 将配置枚举映射到序列化数组，避免依赖枚举声明顺序。
        /// </summary>
        private int GetStatePatternIndex(PocketState state)
        {
            switch (state)
            {
                case PocketState.Locked:
                    return 0;
                case PocketState.Unlocked:
                    return 1;
                case PocketState.Occupied:
                    return 2;
                case PocketState.Disabled:
                    return 3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "未知球洞状态。");
            }
        }

        /// <summary>
        /// 通过盖板几何状态表达洞态，避免仅依赖颜色区分。
        /// </summary>
        private void ApplyStatePattern(PocketState state)
        {
            bool enabled = state != PocketState.Disabled;
            _renderer.enabled = enabled;
            _cap.gameObject.SetActive(state == PocketState.Locked || state == PocketState.Occupied || state == PocketState.Disabled);

            switch (state)
            {
                case PocketState.Disabled:
                    _cap.localScale = Vector3.one * 0.72f;
                    _cap.localRotation = Quaternion.identity;
                    break;
                case PocketState.Locked:
                    _cap.localScale = Vector3.one;
                    _cap.localRotation = Quaternion.identity;
                    break;
                case PocketState.Unlocked:
                    _cap.localScale = Vector3.one;
                    _cap.localRotation = Quaternion.identity;
                    break;
                case PocketState.Occupied:
                    _cap.localScale = Vector3.one * 0.84f;
                    _cap.localRotation = Quaternion.Euler(0, 0, 45);
                    break;
            }
        }
    }
}
