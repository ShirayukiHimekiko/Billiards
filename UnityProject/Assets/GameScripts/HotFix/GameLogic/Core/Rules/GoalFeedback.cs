using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 由物理层提交、表现层只消费一次的几何失败反馈。
    /// </summary>
    public readonly struct GoalFeedback
    {
        /// <summary>
        /// 图形在关卡配置中的明细 ID。
        /// </summary>
        public readonly int GeometryId;

        /// <summary>
        /// 发生失败反弹时的球心位置。
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        /// 失败反弹时的白球半径。
        /// </summary>
        public readonly float CurrentRadius;

        /// <summary>
        /// 与当前半径误差最小的目标半径。
        /// </summary>
        public readonly float TargetRadius;

        /// <summary>
        /// 目标半径减当前半径；正值表示目标在球外，负值表示目标在球内。
        /// </summary>
        public readonly float RadiusDelta;

        /// <summary>
        /// 物理世界提交反馈时的状态版本。
        /// </summary>
        public readonly int Revision;

        public GoalFeedback(
            int geometryId,
            Vector2 position,
            float currentRadius,
            float targetRadius,
            int revision)
        {
            GeometryId = geometryId;
            Position = position;
            CurrentRadius = currentRadius;
            TargetRadius = targetRadius;
            RadiusDelta = targetRadius - currentRadius;
            Revision = revision;
        }
    }
}
