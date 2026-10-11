using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 地形表现基类；实例生命周期由 TerrainManager 管理。
    /// </summary>
    public sealed class Terrain : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer _renderer;

        public void Initialize(TerrainPlacementData data)
        {
            if (_renderer == null)
            {
                throw new InvalidOperationException($"地形 {data.Id} Prefab 缺少 SpriteRenderer。");
            }

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
    }
}
