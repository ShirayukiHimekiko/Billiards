namespace GameLogic
{
    /// <summary>
    /// 由道具 Prefab 上的功能组件提供无场景依赖的模拟效果。
    /// </summary>
    public interface IPropSimulationProvider : IPropComponent
    {
        /// <summary>
        /// 创建与该功能对应的只读模拟规格。
        /// </summary>
        /// <param name="data">道具配置及参数。</param>
        /// <returns>可供真实世界和预测世界共享的规格。</returns>
        PropEffectSpec CreateEffect(PropPlacementData data);
    }
}
