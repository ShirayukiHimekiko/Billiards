namespace GameLogic
{
    /// <summary>
    /// 一类地形的接触查询与命中响应；实现类无状态，运行态只存在于球状态与接触记录。
    /// </summary>
    public interface ITerrainRule
    {
        /// <summary>
        /// 返回该球轨迹内最早的有效进入时刻。
        /// </summary>
        /// <param name="trajectory">待查询球的当前轨迹。</param>
        /// <param name="terrain">地形摆放及类型参数。</param>
        /// <param name="context">受控模拟上下文。</param>
        /// <returns>最早进入触发区的秒数；无有效接触时为正无穷。</returns>
        double FindEnter(in BallTrajectory trajectory, TerrainPlacementData terrain, TerrainContext context);

        /// <summary>
        /// 世界推进到命中时刻后，对该球与地形状态执行本类型效果。
        /// </summary>
        /// <param name="hit">命中的稳定索引与几何信息。</param>
        /// <param name="context">受控模拟上下文。</param>
        void Apply(in TerrainHit hit, TerrainContext context);
    }
}
