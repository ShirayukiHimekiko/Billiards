using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 按球明细加载白球及黑球，负责实例资源的回收。
    /// </summary>
    public sealed class BallManager
    {
        /// <summary>
        /// 与世界球数组保持同序的球体表现列表。
        /// </summary>
        private readonly List<BallBase> _views = new List<BallBase>();

        /// <summary>
        /// 已加载的球资源实例，包含尚未完成初始化的实例。
        /// </summary>
        private readonly List<GameObject> _objects = new List<GameObject>();

        /// <summary>
        /// 当前关卡白球。
        /// </summary>
        public WhiteBall WhiteBall
        {
            get;
            private set;
        }

        /// <summary>
        /// 依次加载本关球 Prefab。
        /// </summary>
        /// <param name="data">本关球体摆放与类型配置。</param>
        /// <param name="board">加载球体实例时使用的台面父节点。</param>
        /// <param name="token">关卡进入流程的取消令牌。</param>
        public async UniTask InitializeAsync(LevelData data, BoardView board, CancellationToken token)
        {
            foreach (var placement in data.Balls)
            {
                var instance = await GameModule.Resource.LoadGameObjectAsync(placement.Config.PrefabLocation, cancellationToken: token);

                // 先登记已返回的实例，再检查取消，保证调用方释放时能回收延迟完成的加载。
                if (instance != null)
                {
                    _objects.Add(instance);
                }

                token.ThrowIfCancellationRequested();

                if (instance == null)
                {
                    throw new InvalidOperationException($"球 {placement.Id} 加载失败。");
                }

                instance.transform.SetParent(board.transform, false);

                var view = instance.GetComponent<BallBase>();

                if (view == null)
                {
                    throw new InvalidOperationException($"球 {placement.Id} Prefab 缺少 BallBase。");
                }

                bool expectsWhite = placement.Config.Kind == GameConfig.BallKind.White;

                if (expectsWhite ? !(view is WhiteBall) : !(view is BlackBall))
                {
                    throw new InvalidOperationException($"球 {placement.Id} Prefab 类型与球种配置不符。");
                }

                view.Initialize(placement);
                _views.Add(view);

                if (view is WhiteBall white)
                {
                    WhiteBall = white;
                }
            }

            board.SetBall(WhiteBall);
        }

        /// <summary>
        /// 同步纯数据世界的表现。
        /// </summary>
        /// <param name="world">与表现列表保持同序的当前模拟世界。</param>
        public void Sync(PhysicsWorld world)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                var view = _views[i];
                var ball = world.Balls[i];
                view.gameObject.SetActive(ball.Active);

                if (ball.Active)
                {
                    view.SyncState(ball);
                }
            }
        }

        /// <summary>
        /// 解绑并销毁所有球实例，Destroy 释放资源系统实例句柄。
        /// </summary>
        public void Release()
        {
            foreach (var view in _views)
            {
                if (view != null)
                {
                    view.Release();
                }
            }

            foreach (var instance in _objects)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }

            _views.Clear();
            _objects.Clear();
            WhiteBall = null;
        }
    }
}
