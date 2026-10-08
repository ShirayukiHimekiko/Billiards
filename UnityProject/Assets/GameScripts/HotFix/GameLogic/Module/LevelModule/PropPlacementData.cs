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

            if (!(config.EffectParams is FlipEffect) || Kind != PropEffectKind.Flip)
            {
                throw new ArgumentException($"道具 {Id} 类型与效果不匹配。");
            }
        }
    }
}
