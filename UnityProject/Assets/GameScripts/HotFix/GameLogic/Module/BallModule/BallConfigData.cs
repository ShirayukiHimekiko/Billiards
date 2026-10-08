using System;
using GameConfig;

namespace GameLogic
{
    /// <summary>
    /// 球类型参数，世界步长和碰撞参数归物理表。
    /// </summary>
    public sealed class BallConfigData
    {
        /// <summary>
        /// 球种，决定是否具有击球输入和尺寸变化参数。
        /// </summary>
        public readonly BallKind Kind;

        /// <summary>
        /// 资源系统加载球体 Prefab 使用的地址。
        /// </summary>
        public readonly string PrefabLocation;

        /// <summary>
        /// 球间碰撞计算冲量时使用的质量，不随半径变化。
        /// </summary>
        public readonly float Mass;

        /// <summary>
        /// 运动过程中允许的最小球半径。
        /// </summary>
        public readonly float RadiusMin;

        /// <summary>
        /// 运动过程中允许的最大球半径；黑球与最小半径相同。
        /// </summary>
        public readonly float RadiusMax;

        /// <summary>
        /// 白球达到满力度所需的蓄力秒数。
        /// </summary>
        public readonly float ChargeDuration;

        /// <summary>
        /// 白球零力度时的初始速度。
        /// </summary>
        public readonly float SpeedMin;

        /// <summary>
        /// 白球满力度时的初始速度。
        /// </summary>
        public readonly float SpeedMax;

        /// <summary>
        /// 白球零力度时应用的击球半径。
        /// </summary>
        public readonly float ShotRadiusMin;

        /// <summary>
        /// 白球满力度时应用的击球半径。
        /// </summary>
        public readonly float ShotRadiusMax;

        /// <summary>
        /// 白球零力度时用于推导减速度的无碰撞基准路程。
        /// </summary>
        public readonly float DistanceMin;

        /// <summary>
        /// 白球满力度时用于推导减速度的无碰撞基准路程。
        /// </summary>
        public readonly float DistanceMax;

        /// <summary>
        /// 白球零力度时的每秒半径增量。
        /// </summary>
        public readonly float GrowRateMin;

        /// <summary>
        /// 白球满力度时的每秒半径增量。
        /// </summary>
        public readonly float GrowRateMax;

        /// <summary>
        /// 白球处于缩小趋势时每秒减少的半径，配置保存正值。
        /// </summary>
        public readonly float ShrinkRate;

        /// <summary>
        /// 转换球参数，黑球没有输入及尺寸变化能力。
        /// </summary>
        /// <param name="row">球类型配置；黑球必须固定尺寸，白球必须包含击球及尺寸变化参数。</param>
        public BallConfigData(Ball row)
        {
            Kind = row.BallKind;
            PrefabLocation = row.PrefabLocation;
            Mass = row.Mass;
            RadiusMin = row.RadiusMin;
            RadiusMax = row.RadiusMax;

            if (Mass <= 0 || RadiusMin <= 0 || RadiusMax < RadiusMin)
            {
                throw new ArgumentException($"球配置 {row.Id} 无效。");
            }

            if (Kind == BallKind.Black)
            {
                if (row.ShotParams != null || row.SizeChangeParams != null || RadiusMin != RadiusMax)
                {
                    throw new ArgumentException($"黑球 {row.Id} 必须固定尺寸且没有击球输入。");
                }

                return;
            }

            var shot = row.ShotParams ?? throw new ArgumentException($"白球 {row.Id} 缺少击球参数。");
            var size = row.SizeChangeParams ?? throw new ArgumentException($"白球 {row.Id} 缺少尺寸变化参数。");
            ChargeDuration = shot.ChargeDuration;
            SpeedMin = shot.SpeedMin;
            SpeedMax = shot.SpeedMax;
            ShotRadiusMin = shot.RadiusMin;
            ShotRadiusMax = shot.RadiusMax;
            DistanceMin = shot.DistanceMin;
            DistanceMax = shot.DistanceMax;
            GrowRateMin = size.GrowRateMin;
            GrowRateMax = size.GrowRateMax;
            ShrinkRate = size.ShrinkRate;

            if (ChargeDuration <= 0
                || SpeedMin <= 0
                || SpeedMax < SpeedMin
                || DistanceMin <= 0
                || DistanceMax < DistanceMin
                || ShotRadiusMin < RadiusMin
                || ShotRadiusMax > RadiusMax
                || ShotRadiusMax < ShotRadiusMin
                || GrowRateMax <= 0
                || GrowRateMin < 0
                || GrowRateMax < GrowRateMin
                || ShrinkRate <= 0)
            {
                throw new ArgumentException($"白球 {row.Id} 参数范围无效。");
            }
        }
    }
}
