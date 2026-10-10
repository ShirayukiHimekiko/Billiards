using System;
using System.Collections.Generic;
using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 显示未拾取钥匙与绑定锁洞之间的低干扰像素连接线。
    /// </summary>
    public sealed class KeyLinkView : MonoBehaviour
    {
        /// <summary>
        /// 由 Prefab 序列化绑定的 LineRenderer 模板，实例只负责线段显示。
        /// </summary>
        [SerializeField]
        private LineRenderer _lineTemplate;

        /// <summary>
        /// 连接线在台面局部坐标中的宽度。
        /// </summary>
        [SerializeField]
        private float _lineWidth = .025f;

        /// <summary>
        /// 连接线相对台面平面的偏移，避免与桌面图形完全重合。
        /// </summary>
        [SerializeField]
        private float _lineZ = -.02f;

        private readonly List<Link> _links = new List<Link>();

        /// <summary>
        /// 按当前关卡钥匙配置创建稳定的一钥一线映射。
        /// </summary>
        /// <param name="level">包含钥匙位置和六洞位置的关卡快照。</param>
        public void Initialize(LevelData level)
        {
            if (_lineTemplate == null)
            {
                throw new InvalidOperationException("钥匙连接线缺少 LineRenderer 模板绑定。");
            }

            Release();
            _lineTemplate.gameObject.SetActive(false);

            for (int propIndex = 0; propIndex < level.Props.Count; propIndex++)
            {
                PropPlacementData prop = level.Props[propIndex];

                if (prop.Kind != PropEffectKind.Key)
                {
                    continue;
                }

                int pocketIndex = FindPocketIndex(level, prop.TargetPocketId);
                var line = Instantiate(_lineTemplate, transform);
                line.name = $"KeyLink_{prop.Id}_{prop.TargetPocketId}";
                line.transform.localPosition = Vector3.zero;
                line.transform.localRotation = Quaternion.identity;
                line.transform.localScale = Vector3.one;
                line.positionCount = 2;
                line.useWorldSpace = false;
                line.startWidth = _lineWidth;
                line.endWidth = _lineWidth;
                line.startColor = GetLinkColor(level, prop.TargetPocketId);
                line.endColor = line.startColor;
                line.gameObject.SetActive(true);
                line.SetPosition(0, new Vector3(prop.Position.x, prop.Position.y, _lineZ));
                Vector2 pocketPosition = level.Pockets[pocketIndex].Position;
                line.SetPosition(1, new Vector3(pocketPosition.x, pocketPosition.y, _lineZ));
                _links.Add(new Link(line, propIndex, pocketIndex));
            }
        }

        /// <summary>
        /// 根据模拟世界隐藏已拾取钥匙及已经解锁的连接线。
        /// </summary>
        /// <param name="world">当前真实世界状态。</param>
        public void Sync(PhysicsWorld world)
        {
            foreach (Link link in _links)
            {
                bool visible = world.PropUses[link.PropIndex] == 0
                    && world.PocketStates[link.PocketIndex] == PocketState.Locked;
                link.Line.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 销毁当前关卡生成的连接线实例。
        /// </summary>
        public void Release()
        {
            foreach (Link link in _links)
            {
                if (link.Line != null)
                {
                    Destroy(link.Line.gameObject);
                }
            }

            _links.Clear();
        }

        private static int FindPocketIndex(LevelData level, int pocketId)
        {
            for (int i = 0; i < level.Pockets.Count; i++)
            {
                if (level.Pockets[i].Id == pocketId)
                {
                    return i;
                }
            }

            throw new ArgumentException($"钥匙绑定洞 {pocketId} 不存在。");
        }

        private static Color GetLinkColor(LevelData level, int pocketId)
        {
            for (int i = 0; i < level.PocketStyles.Count; i++)
            {
                PocketStyle style = level.PocketStyles[i];

                if (style.State == PocketState.Locked)
                {
                    Vector4 value = style.Color;
                    return new Color(value.x, value.y, value.z, value.w * .45f);
                }
            }

            throw new ArgumentException($"洞 {pocketId} 缺少 Locked 样式，无法生成钥匙连接线。");
        }

        private sealed class Link
        {
            public readonly LineRenderer Line;
            public readonly int PropIndex;
            public readonly int PocketIndex;

            public Link(LineRenderer line, int propIndex, int pocketIndex)
            {
                Line = line;
                PropIndex = propIndex;
                PocketIndex = pocketIndex;
            }
        }
    }
}
