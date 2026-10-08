using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 管理配置道具实例及表现，不持有几何判定职责。
    /// </summary>
    public sealed class PropManager
    {
        /// <summary>
        /// 与关卡道具配置保持同序的表现组件。
        /// </summary>
        private readonly List<PropBase> _views = new List<PropBase>();

        /// <summary>
        /// 已加载的道具资源实例，供失败或取消时统一回收。
        /// </summary>
        private readonly List<GameObject> _objects = new List<GameObject>();

        /// <summary>
        /// 由配置数据生成、供世界模拟及预测使用的效果策略。
        /// </summary>
        private readonly List<IPropEffect> _effects = new List<IPropEffect>();

        /// <summary>
        /// 用于查询道具使用次数上限的关卡配置。
        /// </summary>
        private LevelData _level;

        /// <summary>
        /// 具体道具基类提供的无场景依赖效果策略。
        /// </summary>
        public IReadOnlyList<IPropEffect> Effects => _effects;

        /// <summary>
        /// 加载并配置本关道具。
        /// </summary>
        /// <param name="level">本关道具摆放、效果及作用域配置。</param>
        /// <param name="board">道具实例的台面父节点。</param>
        /// <param name="token">关卡进入流程的取消令牌。</param>
        public async UniTask InitializeAsync(LevelData level, BoardView board, CancellationToken token)
        {
            _level = level;

            foreach (var data in level.Props)
            {
                var instance = await GameModule.Resource.LoadGameObjectAsync(data.PrefabLocation, cancellationToken: token);

                // 先登记实例再检查取消，确保未完成初始化的资源也由 Release 回收。
                if (instance != null)
                {
                    _objects.Add(instance);
                }

                token.ThrowIfCancellationRequested();

                if (instance == null)
                {
                    throw new InvalidOperationException($"道具 {data.Id} 加载失败。");
                }

                instance.transform.SetParent(board.transform, false);

                var view = instance.GetComponent<PropBase>();

                if (view == null)
                {
                    throw new InvalidOperationException($"道具 {data.Id} 缺少 PropBase。");
                }

                view.Initialize(data);
                _views.Add(view);
                _effects.Add(PropEffects.Get(data.Kind));
            }
        }

        /// <summary>
        /// 同步道具的剩余使用次数表现。
        /// </summary>
        /// <param name="world">提供按作用域累计的道具使用次数的当前世界。</param>
        public void Sync(PhysicsWorld world)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                _views[i].SetAvailable(world.PropUses[i] < _level.Props[i].UseLimit);
            }
        }

        /// <summary>
        /// 销毁资源实例。
        /// </summary>
        public void Release()
        {
            foreach (var instance in _objects)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }

            _objects.Clear();
            _views.Clear();
            _effects.Clear();
            _level = null;
        }
    }
}
