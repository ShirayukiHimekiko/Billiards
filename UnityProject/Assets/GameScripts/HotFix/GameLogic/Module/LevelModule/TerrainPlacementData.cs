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
        public readonly int Id;
        public readonly int ConfigId;
        public readonly TerrainKind Kind;
        public readonly string PrefabLocation;
        public readonly Vector2 Position;
        public readonly Vector2 Size;
        public readonly Vector2 Direction;
        public readonly float TriggerRadius;
        public readonly float Duration;
        public readonly float SpeedMultiplier;
        public readonly int ExitId;
        public readonly bool InheritVelocity;
        public readonly bool InheritRotation;

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

            if (Id <= 0
                || string.IsNullOrWhiteSpace(PrefabLocation)
                || Size.x <= 0
                || Size.y <= 0
                || TriggerRadius <= 0
                || Duration <= 0
                || SpeedMultiplier <= 0
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
