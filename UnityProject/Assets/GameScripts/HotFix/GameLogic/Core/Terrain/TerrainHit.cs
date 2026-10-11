using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 一次地形命中所需的稳定索引与几何信息。
    /// </summary>
    public readonly struct TerrainHit
    {
        /// <summary>
        /// 球在世界数组中的索引。
        /// </summary>
        public readonly int BallIndex;

        /// <summary>
        /// 地形在关卡数组中的索引。
        /// </summary>
        public readonly int TerrainIndex;

        /// <summary>
        /// 命中相对当前轨迹起点的秒数。
        /// </summary>
        public readonly double Time;

        /// <summary>
        /// 入口面指向球来向的单位法线。
        /// </summary>
        public readonly Vector2 EntryNormal;

        /// <summary>
        /// 进入瞬间的球速单位方向。
        /// </summary>
        public readonly Vector2 TravelDirection;

        /// <summary>
        /// 创建一次地形命中。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        /// <param name="terrainIndex">地形在关卡数组中的索引。</param>
        /// <param name="time">命中相对轨迹起点的秒数。</param>
        /// <param name="entryNormal">入口面指向球来向的单位法线。</param>
        /// <param name="travelDirection">进入瞬间的球速单位方向。</param>
        public TerrainHit(
            int ballIndex,
            int terrainIndex,
            double time,
            Vector2 entryNormal,
            Vector2 travelDirection)
        {
            BallIndex = ballIndex;
            TerrainIndex = terrainIndex;
            Time = time;
            EntryNormal = entryNormal;
            TravelDirection = travelDirection;
        }
    }
}
