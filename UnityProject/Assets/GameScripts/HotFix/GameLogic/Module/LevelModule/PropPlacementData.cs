using System;
using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 道具的摆放及行为参数。
    /// </summary>
    public sealed class PropPlacementData
    {
        /// <summary>
        /// 道具明细 ID，供图形触发效果绑定。
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// 配置作用域内允许的触发次数。
        /// </summary>
        public readonly int UseLimit;

        /// <summary>
        /// 台面局部坐标中的道具中心。
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        /// 资源系统加载道具 Prefab 使用的地址。
        /// </summary>
        public readonly string PrefabLocation;

        /// <summary>
        /// 道具显示直径。
        /// </summary>
        public readonly float VisualDiameter;

        /// <summary>
        /// 接触触发模式使用的圆半径。
        /// </summary>
        public readonly float TriggerRadius;

        /// <summary>
        /// 道具通过接触或图形条件触发。
        /// </summary>
        public readonly TriggerMode TriggerMode;

        /// <summary>
        /// 使用次数按单杆或整关累计的作用域。
        /// </summary>
        public readonly UseScope UseScope;

        /// <summary>
        /// 道具效果策略类型，已转换为逻辑层枚举。
        /// </summary>
        public readonly PropEffectKind Kind;

        /// <summary>
        /// 效果持续的杆数；零表示只作用于触发杆。
        /// </summary>
        public readonly int DurationShots;

        /// <summary>
        /// 配置中的效果倍率或一次性数量参数。
        /// </summary>
        public readonly float RateMultiplier;

        /// <summary>
        /// 钥匙效果绑定的球洞明细 ID；非钥匙道具为零。
        /// </summary>
        public readonly int TargetPocketId;

        /// <summary>
        /// 转换道具明细及复用参数。
        /// </summary>
        /// <param name="row">本关道具位置及触发模式。</param>
        /// <param name="config">该道具引用的效果、尺寸及使用次数参数。</param>
        public PropPlacementData(LevelProp row, Prop config)
        {
            Id = row.Id;
            Position = row.Position;
            TriggerMode = row.TriggerMode;
            PrefabLocation = config.PrefabLocation;
            VisualDiameter = config.VisualDiameter;
            TriggerRadius = config.TriggerRadius;
            UseScope = config.UseScope;
            UseLimit = config.UseLimit;
            Kind = PropKindConverter.ToEffectKind(config.PropKind);

            if (config.EffectParams == null)
            {
                throw new ArgumentException($"道具 {Id} 缺少效果参数。");
            }

            DurationShots = config.EffectParams.DurationShots;
            RateMultiplier = config.EffectParams.RateMultiplier;

            if (DurationShots < 0 || RateMultiplier < 0)
            {
                throw new ArgumentException($"道具 {Id} 的持续杆数或效果倍率无效。");
            }

            if (!MatchesEffectType(config.EffectParams, Kind))
            {
                throw new ArgumentException($"道具 {Id} 类型与效果不匹配。");
            }

            TargetPocketId = Kind == PropEffectKind.Key
                ? ((KeyEffect)config.EffectParams).TargetPocketId
                : 0;
        }

        private static bool MatchesEffectType(PropEffect effect, PropEffectKind kind)
        {
            switch (kind)
            {
                case PropEffectKind.Flip:
                    return effect is FlipEffect;
                case PropEffectKind.Reverse:
                    return effect is ReverseEffect;
                case PropEffectKind.Freeze:
                    return effect is FreezeEffect;
                case PropEffectKind.Restore:
                    return effect is RestoreEffect;
                case PropEffectKind.FastChange:
                    return effect is FastChangeEffect;
                case PropEffectKind.Reveal:
                    return effect is RevealEffect;
                case PropEffectKind.Preview:
                    return effect is PreviewEffect;
                case PropEffectKind.AddShot:
                    return effect is AddShotEffect;
                case PropEffectKind.Key:
                    return effect is KeyEffect;
                default:
                    return false;
            }
        }
    }
}
