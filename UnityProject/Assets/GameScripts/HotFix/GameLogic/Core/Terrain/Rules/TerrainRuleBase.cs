namespace GameLogic
{
    /// <summary>
    /// 地形规则基类；提供触发圆进入查询的默认实现，子类只实现各自效果。
    /// </summary>
    public abstract class TerrainRuleBase : ITerrainRule
    {
        /// <summary>
        /// 默认以地形触发圆作为进入几何，返回球心首次由外向内进入触发圆的时刻。
        /// </summary>
        /// <param name="trajectory">待查询球的当前轨迹。</param>
        /// <param name="terrain">地形摆放及类型参数。</param>
        /// <param name="context">受控模拟上下文。</param>
        /// <returns>最早进入触发区的秒数；无接触时为正无穷。</returns>
        public virtual double FindEnter(in BallTrajectory trajectory, TerrainPlacementData terrain, TerrainContext context)
        {
            return context.Solver.CircleEnter(trajectory, terrain.Position, terrain.TriggerRadius);
        }

        /// <summary>
        /// 世界推进到命中时刻后执行本类型地形的效果。
        /// </summary>
        /// <param name="hit">命中的稳定索引与几何信息。</param>
        /// <param name="context">受控模拟上下文。</param>
        public abstract void Apply(in TerrainHit hit, TerrainContext context);
    }
}
