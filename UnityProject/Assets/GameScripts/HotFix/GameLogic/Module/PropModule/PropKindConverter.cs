using System;

namespace GameLogic
{
    /// <summary>
    /// 把 Luban 配置枚举转换为逻辑层枚举的唯一入口。
    /// </summary>
    public static class PropKindConverter
    {
        /// <summary>
        /// 将配置道具类型映射为逻辑层策略类型。
        /// </summary>
        /// <param name="kind">配置表声明的道具类型。</param>
        /// <returns>对应的逻辑层策略类型；不支持的配置类型抛出参数异常。</returns>
        public static PropEffectKind ToEffectKind(GameConfig.PropKind kind)
        {
            switch (kind)
            {
                case GameConfig.PropKind.Flip:
                    return PropEffectKind.Flip;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
