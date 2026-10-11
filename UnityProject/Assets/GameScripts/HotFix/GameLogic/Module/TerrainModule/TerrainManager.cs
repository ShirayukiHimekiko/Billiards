using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 管理地形资源加载、实例化、同步和释放；不参与物理求解。
    /// </summary>
    public sealed class TerrainManager : Singleton<TerrainManager>
    {
        private readonly List<TerrainObject> _views = new List<TerrainObject>();
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<TerrainRuntimeState> _states = new List<TerrainRuntimeState>();
        private LevelData _level;

        public async UniTask InitializeAsync(LevelData level, BoardView board, CancellationToken token)
        {
            _level = level;

            foreach (TerrainPlacementData data in level.Terrains)
            {
                GameObject instance = await GameModule.Resource.LoadGameObjectAsync(data.PrefabLocation, cancellationToken: token);
                if (instance != null)
                {
                    _objects.Add(instance);
                }

                token.ThrowIfCancellationRequested();

                if (instance == null)
                {
                    throw new InvalidOperationException($"地形 {data.Id} 资源加载失败：{data.PrefabLocation}。");
                }

                instance.transform.SetParent(board.transform, false);
                TerrainObject view = instance.GetComponent<TerrainObject>();
                if (view == null)
                {
                    throw new InvalidOperationException($"地形 {data.Id} Prefab 缺少 Terrain。");
                }

                view.Initialize(data);
                _views.Add(view);
                _states.Add(new TerrainRuntimeState());
            }
        }

        /// <summary>
        /// 从球阶段聚合推导每个地形实例的高亮状态并同步到表现；不读取地形模拟状态。
        /// </summary>
        /// <param name="world">当前真实世界的球体状态来源。</param>
        public void Sync(PhysicsWorld world)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                TerrainRuntimeState state = _states[i];
                state.Highlighted = IsHighlighted(world, _level.Terrains[i].Id);
                _views[i].SetState(state);
            }
        }

        /// <summary>
        /// 判断指定地形是否有球处于持续地形阶段。
        /// </summary>
        /// <param name="world">当前真实世界。</param>
        /// <param name="terrainId">地形明细 ID。</param>
        /// <returns>存在处于该地形持续阶段的球时为 true。</returns>
        private static bool IsHighlighted(PhysicsWorld world, int terrainId)
        {
            foreach (SimulatedBall ball in world.Balls)
            {
                if (ball.Active
                    && ball.Terrain.Phase != TerrainPhase.Normal
                    && ball.Terrain.PhaseTerrainId == terrainId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 销毁当前关卡的地形实例并清除引用。
        /// </summary>
        public void ClearLevel()
        {
            foreach (GameObject instance in _objects)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }

            _objects.Clear();
            _views.Clear();
            _states.Clear();
            _level = null;
        }

        /// <summary>
        /// 释放单例前清理当前关卡地形。
        /// </summary>
        protected override void OnRelease()
        {
            ClearLevel();
        }
    }
}
