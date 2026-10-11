using System;

namespace GameLogic
{
    /// <summary>
    /// 每球每地形实例的触发区内外记录；只由边界穿越事件更新，不是整地形共享的冷却。
    /// </summary>
    public sealed class TerrainContactBook
    {
        /// <summary>
        /// 按球索引乘地形数再加地形索引存储的区内标记。
        /// </summary>
        private readonly bool[] _inside;

        /// <summary>
        /// 本关地形实例数量，用于二维索引换算。
        /// </summary>
        private readonly int _terrainCount;

        /// <summary>
        /// 按球数与地形数创建空的接触记录。
        /// </summary>
        /// <param name="ballCount">本关球体数量。</param>
        /// <param name="terrainCount">本关地形实例数量。</param>
        public TerrainContactBook(int ballCount, int terrainCount)
        {
            _terrainCount = terrainCount;
            _inside = new bool[ballCount * terrainCount];
        }

        /// <summary>
        /// 查询指定球是否位于指定地形触发区内。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        /// <param name="terrainIndex">地形在关卡数组中的索引。</param>
        /// <returns>球心当前是否在触发区内。</returns>
        public bool IsInside(int ballIndex, int terrainIndex)
        {
            return _inside[ballIndex * _terrainCount + terrainIndex];
        }

        /// <summary>
        /// 写入指定球与地形的区内标记。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        /// <param name="terrainIndex">地形在关卡数组中的索引。</param>
        /// <param name="value">进入触发区为 true，离开为 false。</param>
        public void SetInside(int ballIndex, int terrainIndex, bool value)
        {
            _inside[ballIndex * _terrainCount + terrainIndex] = value;
        }

        /// <summary>
        /// 把当前记录整体复制到同尺寸的目标记录。
        /// </summary>
        /// <param name="target">与当前球数、地形数一致的目标记录。</param>
        public void CopyTo(TerrainContactBook target)
        {
            Array.Copy(_inside, target._inside, _inside.Length);
        }
    }
}
