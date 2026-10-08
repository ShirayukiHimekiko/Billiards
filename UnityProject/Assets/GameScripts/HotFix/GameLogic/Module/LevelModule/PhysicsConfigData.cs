using System;
using GameConfig;

namespace GameLogic
{
    /// <summary>
    /// 世界共用的物理参数。
    /// </summary>
    public sealed class PhysicsConfigData
    {
        /// <summary>
        /// 真实模拟与预测共用的固定步长，单位为秒。
        /// </summary>
        public readonly float Step;

        /// <summary>
        /// 球间碰撞的恢复系数，范围为零到一。
        /// </summary>
        public readonly float BallRestitution;

        /// <summary>
        /// 库边反弹的恢复系数，范围为零到一。
        /// </summary>
        public readonly float RailRestitution;

        /// <summary>
        /// 几何门反弹的恢复系数，范围为零到一。
        /// </summary>
        public readonly float GeometryRestitution;

        /// <summary>
        /// 接触分离与半径增长限制使用的距离容差。
        /// </summary>
        public readonly float ContactTolerance;

        /// <summary>
        /// 球体停止判定的速度阈值。
        /// </summary>
        public readonly float StopSpeed;

        /// <summary>
        /// 单次 Step 处理连续接触事件的迭代上限。
        /// </summary>
        public readonly int MaxEventIterations;

        /// <summary>
        /// 完整轨迹预测及几何门接近查询的最大模拟步数。
        /// </summary>
        public readonly int PredictionBudget;

        /// <summary>
        /// 转换物理参数。
        /// </summary>
        /// <param name="row">物理参数记录，当前只支持按击球路程推导减速度。</param>
        public PhysicsConfigData(GameConfig.Physics row)
        {
            Step = row.SimulationStep;
            BallRestitution = row.BallRestitution;
            RailRestitution = row.RailRestitution;
            GeometryRestitution = row.GeometryRestitution;
            ContactTolerance = row.ContactTolerance;
            StopSpeed = row.StopSpeed;
            MaxEventIterations = row.MaxEventIterations;
            PredictionBudget = row.PredictionBudget;

            if (Step <= 0
                || Step > .05f
                || ContactTolerance <= 0
                || StopSpeed <= 0
                || MaxEventIterations < 8
                || PredictionBudget <= 0
                || BallRestitution < 0
                || BallRestitution > 1
                || RailRestitution < 0
                || RailRestitution > 1
                || GeometryRestitution < 0
                || GeometryRestitution > 1
                || row.DecelerationModel != DecelerationModel.ShotDistance)
            {
                throw new ArgumentException($"物理配置 {row.Id} 无效。");
            }
        }
    }
}
