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
        /// 由真实世界推进产生、等待会话发布的一次性钥匙拾取反馈。
        /// </summary>
        private readonly List<int> _pendingKeyPockets = new List<int>();

        /// <summary>
        /// 上次同步的道具使用次数，用于识别单次拾取边沿。
        /// </summary>
        private int[] _lastUses = Array.Empty<int>();

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

            _lastUses = new int[level.Props.Count];
        }

        /// <summary>
        /// 同步道具的剩余使用次数表现。
        /// </summary>
        /// <param name="world">提供按作用域累计的道具使用次数的当前世界。</param>
        public void Sync(PhysicsWorld world)
        {
            if (_lastUses.Length != world.PropUses.Length)
            {
                _lastUses = new int[world.PropUses.Length];
            }

            for (int i = 0; i < _views.Count; i++)
            {
                int uses = world.PropUses[i];

                if (uses > _lastUses[i] && _level.Props[i].Kind == PropEffectKind.Key)
                {
                    for (int use = _lastUses[i]; use < uses; use++)
                    {
                        _pendingKeyPockets.Add(_level.Props[i].TargetPocketId);
                    }
                }

                _lastUses[i] = uses;
                _views[i].SetAvailable(world.PropUses[i] < _level.Props[i].UseLimit);
            }
        }

        /// <summary>
        /// 取出本次同步产生的钥匙拾取反馈，避免重复提示。
        /// </summary>
        /// <param name="pockets">接收已解锁洞 ID 的列表。</param>
        /// <returns>是否存在待发布反馈。</returns>
        public bool ConsumeKeyPickupFeedbacks(List<int> pockets)
        {
            if (_pendingKeyPockets.Count == 0)
            {
                return false;
            }

            pockets.AddRange(_pendingKeyPockets);
            _pendingKeyPockets.Clear();

            return true;
        }

        /// <summary>
        /// 生成 HUD 使用的钥匙绑定快照，不暴露物理数组给 UI。
        /// </summary>
        /// <param name="world">当前真实世界状态。</param>
        /// <returns>按关卡配置顺序排列的钥匙状态。</returns>
        public IReadOnlyList<KeyUISnapshot> GetKeyStates(PhysicsWorld world)
        {
            var states = new List<KeyUISnapshot>();

            for (int propIndex = 0; propIndex < _level.Props.Count; propIndex++)
            {
                PropPlacementData prop = _level.Props[propIndex];

                if (prop.Kind != PropEffectKind.Key)
                {
                    continue;
                }

                int pocketIndex = FindPocketIndex(prop.TargetPocketId);
                states.Add(new KeyUISnapshot(
                    prop.Id,
                    prop.TargetPocketId,
                    world.PropUses[propIndex] >= prop.UseLimit,
                    world.PocketStates[pocketIndex]));
            }

            return states.AsReadOnly();
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
            _pendingKeyPockets.Clear();
            _lastUses = Array.Empty<int>();
            _level = null;
        }

        private int FindPocketIndex(int pocketId)
        {
            for (int i = 0; i < _level.Pockets.Count; i++)
            {
                if (_level.Pockets[i].Id == pocketId)
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"钥匙绑定洞 {pocketId} 不存在。");
        }
    }
}
