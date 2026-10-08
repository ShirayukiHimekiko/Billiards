using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 球体摆放快照。
    /// </summary>
    public sealed class BallPlacementData
    {
        /// <summary>
        /// 球体明细 ID，用于关联和错误提示。
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// 球体引用的类型参数快照。
        /// </summary>
        public readonly BallConfigData Config;

        /// <summary>
        /// 台面局部坐标中的出生球心位置。
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        /// 出生及犯规复位时使用的球半径。
        /// </summary>
        public readonly float Radius;

        /// <summary>
        /// 转换球体明细。
        /// </summary>
        /// <param name="row">本关球体明细，提供出生位置及半径。</param>
        /// <param name="config">该球引用的类型参数。</param>
        public BallPlacementData(LevelBall row, Ball config)
        {
            Id = row.Id;
            Config = new BallConfigData(config);
            Position = row.SpawnPosition;
            Radius = row.SpawnRadius;
        }
    }
}
