using System;
using System.Collections.Generic;
using System.Threading;

namespace GameLogic
{
    /// <summary>
    /// 统一读取九表并组成关卡快照。
    /// </summary>
    public sealed class LevelManager
    {
        /// <summary>
        /// 首次查询后缓存的只读关卡目录，按 Order 和 Id 排序。
        /// </summary>
        private IReadOnlyList<GameConfig.Level> _catalogue;

        /// <summary>
        /// 按关卡排序提供只读目录，不创建关卡世界或加载台面。
        /// </summary>
        /// <returns>按 Order、Id 排序的关卡记录。</returns>
        public IReadOnlyList<GameConfig.Level> GetCatalogue()
        {
            if (_catalogue == null)
            {
                var levels = new List<GameConfig.Level>(ConfigSystem.Instance.Tables.TbLevel.DataList);
                levels.Sort((left, right) =>
                {
                    int order = left.Order.CompareTo(right.Order);

                    return order != 0 ? order : left.Id.CompareTo(right.Id);
                });
                _catalogue = levels.AsReadOnly();
            }

            return _catalogue;
        }

        /// <summary>
        /// 本关配置快照。
        /// </summary>
        public LevelData Current
        {
            get;
            private set;
        }

        /// <summary>
        /// 通过 levelId 聚合明细。
        /// </summary>
        /// <param name="id">配置目录中存在的关卡 ID。</param>
        /// <param name="token">进入流程的取消令牌，聚合前检查取消。</param>
        /// <returns>聚合九表并完成布局校验的关卡快照。</returns>
        public LevelData LoadLevel(int id, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var tables = ConfigSystem.Instance.Tables;
            var row = tables.TbLevel.Get(id);
            var board = tables.TbBoard.Get(row.BoardConfigId);
            var balls = new List<BallPlacementData>();
            var pockets = new List<PocketPlacementData>();
            var props = new List<PropPlacementData>();
            var geometries = new List<GeometryData>();
            var terrains = new List<TerrainPlacementData>();

            foreach (var child in tables.TbLevelBall.DataList)
            {
                if (child.LevelId == id)
                {
                    balls.Add(new BallPlacementData(child, tables.TbBall.Get(child.BallConfigId)));
                }
            }

            if (board.PocketLayouts.Count != LevelData.FixedPocketCount
                || board.PocketStyles.Count != 4)
            {
                throw new ArgumentException($"台面 {board.Id} 必须配置固定六洞及四状态样式。");
            }

            var styleStates = new HashSet<GameConfig.PocketState>();

            foreach (var style in board.PocketStyles)
            {
                if (!styleStates.Add(style.State))
                {
                    throw new ArgumentException($"台面 {board.Id} 球洞样式状态重复。");
                }
            }

            foreach (GameConfig.PocketState state in Enum.GetValues(typeof(GameConfig.PocketState)))
            {
                if (!styleStates.Contains(state))
                {
                    throw new ArgumentException($"台面 {board.Id} 缺少球洞状态 {state} 的样式。");
                }
            }

            var prediction = board.PredictionStyle;

            if (prediction.DashLength <= 0
                || prediction.GapLength <= 0
                || prediction.LineWidth <= 0
                || prediction.DotDiameter <= 0
                || board.GeometryStyle.LineWidth <= 0
                || board.GeometryStyle.PreviewLineWidth <= 0
                || board.GeometryStyle.PreviewFailureThreshold <= 0
                || board.GeometryStyle.PreviewFailureInterval <= 0
                || board.GeometryStyle.PreviewDuration <= 0)
            {
                throw new ArgumentException($"台面 {board.Id} 预测或图形样式尺寸必须为正值。");
            }

            var layouts = new Dictionary<GameConfig.PocketSlot, GameConfig.PocketLayout>();

            foreach (var layout in board.PocketLayouts)
            {
                layouts.Add(layout.Slot, layout);
            }

            foreach (var child in tables.TbLevelPocket.DataList)
            {
                if (child.LevelId == id)
                {
                    pockets.Add(new PocketPlacementData(child, layouts[child.Slot], UnityEngine.Rect.MinMaxRect(
                        board.Min.x,
                        board.Min.y,
                        board.Max.x,
                        board.Max.y)));
                }
            }

            foreach (var child in tables.TbLevelProp.DataList)
            {
                if (child.LevelId == id)
                {
                    props.Add(new PropPlacementData(child, tables.TbProp.Get(child.PropConfigId)));
                }
            }

            foreach (var child in tables.TbLevelGeometry.DataList)
            {
                if (child.LevelId == id)
                {
                    geometries.Add(new GeometryData(child));
                }
            }

            foreach (var child in tables.TbLevelTerrain.DataList)
            {
                if (child.LevelId == id)
                {
                    terrains.Add(new TerrainPlacementData(child, tables.TbTerrain.Get(child.TerrainConfigId)));
                }
            }

            Current = new LevelData(
                row,
                board,
                new PhysicsConfigData(tables.TbPhysics.Get(row.PhysicsConfigId)),
                balls,
                pockets,
                props,
                geometries,
                terrains);

            return Current;
        }

        /// <summary>
        /// 解除台面及关卡数据引用。
        /// </summary>
        /// <param name="view">需要解除引用的台面，未创建台面时可传空。</param>
        public void UnloadLevel(BoardView view)
        {
            if (view != null)
            {
                view.Release();
            }

            Current = null;
        }

        /// <summary>
        /// 查询下一关；最后一关返回零。
        /// </summary>
        /// <param name="id">当前关卡 ID。</param>
        /// <returns>Order 大于当前关卡的最小 Order 记录 ID；没有下一关时为零。</returns>
        public int NextLevelId(int id)
        {
            var rows = ConfigSystem.Instance.Tables.TbLevel;
            int order = rows.Get(id).Order;
            int best = int.MaxValue;
            int result = 0;

            foreach (var row in rows.DataList)
            {
                if (row.Order > order && row.Order < best)
                {
                    best = row.Order;
                    result = row.Id;
                }
            }

            return result;
        }
    }
}
