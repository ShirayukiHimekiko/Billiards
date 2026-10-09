using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 管理独立图形实例的表现及进度，碰撞判定统一在模拟世界。
    /// </summary>
    public sealed class GeometryManager
    {
        /// <summary>
        /// 与关卡图形配置保持同序的模板实例。
        /// </summary>
        private readonly List<GeometryView> _views = new List<GeometryView>();

        /// <summary>
        /// 使用编辑器制作的模板创建本关图形。
        /// </summary>
        /// <param name="level">本关图形数据及显示样式。</param>
        /// <param name="board">包含图形模板和实例父节点的台面。</param>
        public void Initialize(LevelData level, BoardView board)
        {
            foreach (var data in level.Geometries)
            {
                var view = Object.Instantiate(board.GeometryTemplate, board.transform);
                view.name = "Geometry_" + data.Id;
                view.Initialize(data, level.GeometryStyle);
                view.SetHints(level.ShowHints);
                view.gameObject.SetActive(true);
                _views.Add(view);
            }
        }

        /// <summary>
        /// 同步所有图形进度。
        /// </summary>
        /// <param name="world">提供图形效果完成标记的当前世界。</param>
        public void Sync(PhysicsWorld world)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                _views[i].SetCompleted(world.GeometryCompleted[i]);
            }

            if (world.TryConsumeGoalFeedback(out GoalFeedback feedback))
            {
                foreach (var view in _views)
                {
                    if (view.GeometryId == feedback.GeometryId)
                    {
                        view.ShowFailureFeedback(feedback);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 切换所有图形提示。
        /// </summary>
        /// <param name="visible">是否显示尚未完成图形的条件圆。</param>
        public void SetHints(bool visible)
        {
            foreach (var view in _views)
            {
                view.SetHints(visible);
            }
        }

        /// <summary>
        /// 释放本关图形。
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
