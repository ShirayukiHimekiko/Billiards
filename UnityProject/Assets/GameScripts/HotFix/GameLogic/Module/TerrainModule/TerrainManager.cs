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
    public sealed class TerrainManager
    {
        private readonly List<TerrainBase> _views = new List<TerrainBase>();
        private readonly List<GameObject> _objects = new List<GameObject>();
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
                TerrainBase view = instance.GetComponent<TerrainBase>();
                if (view == null)
                {
                    throw new InvalidOperationException($"地形 {data.Id} Prefab 缺少 TerrainBase。");
                }

                view.Initialize(data);
                _views.Add(view);
            }
        }

        public void Sync(PhysicsWorld world)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                _views[i].SetState(world.TerrainStates[i]);
            }
        }

        public void Release()
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
            _level = null;
        }
    }
}
