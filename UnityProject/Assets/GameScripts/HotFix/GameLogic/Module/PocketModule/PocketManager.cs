using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 管理本关固定六洞表现，物理状态保存在模拟世界。
    /// </summary>
    public sealed class PocketManager
    {
        /// <summary>
        /// 与关卡球洞配置保持同序的模板实例。
        /// </summary>
        private readonly List<PocketView> _views = new List<PocketView>();

        /// <summary>
        /// 按配置摆放固定六洞模板实例。
        /// </summary>
        /// <param name="level">本关六洞摆放与状态样式。</param>
        /// <param name="board">包含球洞模板和实例父节点的台面。</param>
        public void Initialize(LevelData level, BoardView board)
        {
            foreach (var data in level.Pockets)
            {
                var view = Object.Instantiate(board.PocketTemplate, board.transform);
                view.name = "Pocket_" + data.Id;
                view.Initialize(data, level.PocketStyles);
                view.gameObject.SetActive(true);
                _views.Add(view);
            }
        }

        /// <summary>
        /// 同步每洞四状态。
        /// </summary>
        /// <param name="world">提供当前球洞状态的模拟世界。</param>
        public void Sync(PhysicsWorld world)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                _views[i].SetState(world.PocketStates[i]);
            }
        }

        /// <summary>
        /// 释放所有模板实例。
        /// </summary>
        public void Release()
        {
            foreach (var view in _views)
            {
                if (view != null)
                {
                    Object.Destroy(view.gameObject);
                }
            }

            _views.Clear();
        }
    }
}
