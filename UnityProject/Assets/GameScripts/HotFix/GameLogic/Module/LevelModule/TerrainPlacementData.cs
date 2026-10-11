using System;
using GameConfig;
using UnityEngine;
using TerrainConfig = GameConfig.Terrain;

namespace GameLogic
{
    /// <summary>
    /// 已通过入口校验的地形摆放快照；运行时不再直接读取 Luban 行数据。
    /// </summary>
    public sealed class TerrainPlacementData
    {
        /// <summary>
        /// 地陷白球脱出所需的最小归一化力度；待策划用专属字段配置，当前为兼容默认值。
        /// </summary>
        public const float DefaultPitEscapePowerThreshold = .9f;

        /// <summary>
        /// 地陷被其他球撞出的最小有效法向冲量；零表示任意有效冲量即可脱出。
        /// </summary>
        public const float DefaultPitEscapeImpulseThreshold = 0f;

        /// <summary>
        /// 地形明细 ID。
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// 地形样式配置 ID。
        /// </summary>
        public readonly int ConfigId;

        /// <summary>
        /// 地形类型。
        /// </summary>
        public readonly TerrainKind Kind;

        /// <summary>
        /// 资源系统加载该地形 Prefab 使用的地址。
        /// </summary>
        public readonly string PrefabLocation;

        /// <summary>
        /// 台面局部坐标中的地形中心。
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        /// 地形视觉尺寸。
        /// </summary>
        public readonly Vector2 Size;

        /// <summary>
        /// 归一化后的地形朝向。
        /// </summary>
        public readonly Vector2 Direction;

        /// <summary>
        /// 触发圆半径。
        /// </summary>
        public readonly float TriggerRadius;

        /// <summary>
        /// 隧道通行口内径（半径）；兼容期取视觉尺寸较短边的一半，待配置独立内径。
        /// </summary>
        public readonly float TunnelInnerRadius;

        /// <summary>
        /// 隧道沿通道轴的半长，用于确定出口端面；兼容期取触发圆半径。
        /// </summary>
        public readonly float TunnelHalfLength;

        /// <summary>
        /// 跳台入口速度到飞行路程的换算时间；兼容期取持续时间。
        /// </summary>
        public readonly float JumpFlightTime;

        /// <summary>
        /// 跳台飞行路程的速度倍率；兼容期取样式速度倍率，仅影响路程不影响球速大小。
        /// </summary>
        public readonly float JumpSpeedMultiplier;

        /// <summary>
        /// 跳台飞行路程下限。
        /// </summary>
        public readonly float JumpMinDistance;

        /// <summary>
        /// 跳台飞行路程上限。
        /// </summary>
        public readonly float JumpMaxDistance;

        /// <summary>
        /// 地陷白球脱出的归一化力度阈值。
        /// </summary>
        public readonly float PitEscapePowerThreshold;

        /// <summary>
        /// 地陷被其他球撞出的有效法向冲量阈值。
        /// </summary>
        public readonly float PitEscapeImpulseThreshold;

        /// <summary>
        /// 旧数据兼容字段：通用持续时间，仅用于推导新类型的兼容参数。
        /// </summary>
        public readonly float Duration;

        /// <summary>
        /// 旧数据兼容字段：通用速度倍率，仅用于推导新类型的兼容参数。
        /// </summary>
        public readonly float SpeedMultiplier;

        /// <summary>
        /// 传送口配对的出口明细 ID；非传送口为零。
        /// </summary>
        public readonly int ExitId;

        /// <summary>
        /// 旧数据兼容字段：是否继承入射速度；当前实现始终保速，仅保留读取路径。
        /// </summary>
        public readonly bool InheritVelocity;

        /// <summary>
        /// 旧数据兼容字段：是否继承入射朝向；当前实现始终保向，仅保留读取路径。
        /// </summary>
        public readonly bool InheritRotation;

        /// <summary>
        /// 由摆放行与样式行构造已校验的地形快照。
        /// </summary>
        /// <param name="row">关卡地形摆放记录。</param>
        /// <param name="config">地形样式记录。</param>
        public TerrainPlacementData(LevelTerrain row, TerrainConfig config)
        {
            if (row == null || config == null)
            {
                throw new ArgumentException("地形摆放或地形样式配置缺失。");
            }

            Id = row.Id;
            ConfigId = config.Id;
            Kind = config.TerrainKind;
            PrefabLocation = config.PrefabLocation;
            Position = row.Position;
            Size = row.Size;

            if (!IsFinite(row.Direction) || row.Direction.sqrMagnitude <= .000001f)
            {
                throw new ArgumentException($"地形 {row.Id} 的方向必须是有限且非零向量。");
            }

            Direction = row.Direction.normalized;
            TriggerRadius = row.TriggerRadius;
            Duration = row.Duration > 0 ? row.Duration : config.DefaultDuration;
            SpeedMultiplier = config.DefaultSpeedMultiplier;
            ExitId = row.ExitId;
            InheritVelocity = config.InheritVelocity;
            InheritRotation = config.InheritRotation;

            // 兼容期换算：新类型字段暂由旧通用字段推导，待配表落地专属字段后替换。
            TunnelInnerRadius = Mathf.Min(Size.x, Size.y) * .5f;
            TunnelHalfLength = TriggerRadius;
            JumpFlightTime = Duration;
            JumpSpeedMultiplier = SpeedMultiplier;
            JumpMinDistance = 0;
            JumpMaxDistance = float.MaxValue;
            PitEscapePowerThreshold = DefaultPitEscapePowerThreshold;
            PitEscapeImpulseThreshold = DefaultPitEscapeImpulseThreshold;

            if (Id <= 0
                || string.IsNullOrWhiteSpace(PrefabLocation)
                || Size.x <= 0
                || Size.y <= 0
                || TriggerRadius <= 0
                || TunnelInnerRadius <= 0
                || TunnelHalfLength <= 0
                || Duration <= 0
                || SpeedMultiplier <= 0
                || JumpFlightTime <= 0
                || JumpMinDistance < 0
                || JumpMaxDistance < JumpMinDistance
                || PitEscapePowerThreshold <= 0
                || PitEscapePowerThreshold > 1
                || PitEscapeImpulseThreshold < 0
                || !IsFinite(Position)
                || !IsFinite(Size)
                || !IsFinite(Direction))
            {
                throw new ArgumentException($"地形 {Id} 的尺寸、触发范围或持续参数无效。");
            }
        }

        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x)
                && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y)
                && !float.IsInfinity(value.y);
        }
    }
}
