using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 道具实例的统一入口，负责通用表现和同物体功能组件查询。
    /// </summary>
    public class PropObject : MonoBehaviour
    {
        /// <summary>
        /// 道具主体显示子节点的约定名称。
        /// </summary>
        private const string VISUAL_NODE_NAME = "Visual";

        /// <summary>
        /// 缓存的显示层渲染器，初始化时按约定子节点获取。
        /// </summary>
        private SpriteRenderer _renderer;

        private IPropComponent[] _features;
        private IPropAvailabilityPresentation _availabilityPresentation;

        /// <summary>
        /// 应用道具的台面位置与显示尺寸，并缓存功能组件。
        /// </summary>
        /// <param name="data">道具摆放配置。</param>
        public void Initialize(PropPlacementData data)
        {
            _renderer = GetRequiredRenderer(data);
            _features = GetComponents<IPropComponent>();
            _availabilityPresentation = GetFeature<IPropAvailabilityPresentation>();
            transform.localPosition = data.Position;
            transform.localScale = Vector3.one * (data.VisualDiameter / _renderer.sprite.bounds.size.x);
            SetAvailable(true);
        }

        /// <summary>
        /// 从道具同物体的组件中取得唯一的指定功能。
        /// </summary>
        /// <typeparam name="T">功能接口类型。</typeparam>
        /// <returns>对应功能；不存在时返回 null。</returns>
        public T GetFeature<T>() where T : class, IPropComponent
        {
            T result = null;

            foreach (IPropComponent feature in _features)
            {
                if (feature is T match)
                {
                    if (result != null)
                    {
                        throw new InvalidOperationException($"道具 {name} 存在多个 {typeof(T).Name} 功能组件。");
                    }

                    result = match;
                }
            }

            return result;
        }

        /// <summary>
        /// 显示道具是否仍可触发，并通知可选的表现功能。
        /// </summary>
        /// <param name="available">是否仍有可用次数。</param>
        public virtual void SetAvailable(bool available)
        {
            _renderer.color = available ? Color.white : new Color(.5f, .5f, .5f, .35f);
            _availabilityPresentation?.SetAvailable(available);
        }

        /// <summary>
        /// 按 Prefab 结构约定获取主体显示层渲染器，缺失时在初始化边界报错。
        /// </summary>
        /// <param name="data">当前道具摆放配置。</param>
        /// <returns>道具主体的 SpriteRenderer。</returns>
        private SpriteRenderer GetRequiredRenderer(PropPlacementData data)
        {
            Transform visual = transform.Find(VISUAL_NODE_NAME);

            if (visual == null)
            {
                throw new InvalidOperationException($"道具 {data.Id} Prefab 缺少子节点 {VISUAL_NODE_NAME}。");
            }

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();

            if (renderer == null)
            {
                throw new InvalidOperationException($"道具 {data.Id} Prefab 节点 {VISUAL_NODE_NAME} 缺少 SpriteRenderer。");
            }

            if (renderer.sprite == null)
            {
                throw new InvalidOperationException($"道具 {data.Id} Prefab 节点 {VISUAL_NODE_NAME} 缺少 Sprite 绑定。");
            }

            return renderer;
        }
    }
}
