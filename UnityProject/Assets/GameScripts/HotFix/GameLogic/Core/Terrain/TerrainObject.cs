using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 地形表现基类；实例生命周期由 TerrainManager 管理。
    /// </summary>
    public sealed class TerrainObject : MonoBehaviour
    {
        /// <summary>
        /// 地形主体显示子节点的约定名称。
        /// </summary>
        private const string VISUAL_NODE_NAME = "Visual";

        /// <summary>
        /// 缓存的显示层渲染器，初始化时按约定子节点获取。
        /// </summary>
        private SpriteRenderer _renderer;

        public void Initialize(TerrainPlacementData data)
        {
            _renderer = GetRequiredRenderer(data);
            transform.localPosition = data.Position;
            transform.localScale = new Vector3(data.Size.x, data.Size.y, 1);
            transform.localRotation = Quaternion.FromToRotation(Vector3.right, data.Direction);
        }

        /// <summary>
        /// 应用由球阶段聚合得到的派生表现状态。
        /// </summary>
        /// <param name="state">本帧该地形是否有球处于持续阶段。</param>
        public void SetState(TerrainRuntimeState state)
        {
            _renderer.color = state.Highlighted ? new Color(1, 1, 1, .9f) : Color.white;
        }

        /// <summary>
        /// 按 Prefab 结构约定获取主体显示层渲染器，缺失时在初始化边界报错。
        /// </summary>
        /// <param name="data">当前地形摆放配置。</param>
        /// <returns>地形主体的 SpriteRenderer。</returns>
        private SpriteRenderer GetRequiredRenderer(TerrainPlacementData data)
        {
            Transform visual = transform.Find(VISUAL_NODE_NAME);

            if (visual == null)
            {
                throw new InvalidOperationException($"地形 {data.Id} Prefab 缺少子节点 {VISUAL_NODE_NAME}。");
            }

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();

            if (renderer == null)
            {
                throw new InvalidOperationException($"地形 {data.Id} Prefab 节点 {VISUAL_NODE_NAME} 缺少 SpriteRenderer。");
            }

            return renderer;
        }
    }
}
