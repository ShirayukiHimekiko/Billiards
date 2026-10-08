namespace GameLogic
{
    /// <summary>
    /// 白球与三角形目标的达成类型。
    /// </summary>
    public enum GoalKind
    {
        /// <summary>
        /// 当前状态未满足配置中的几何条件。
        /// </summary>
        None,

        /// <summary>
        /// 球体满足三角形内切条件。
        /// </summary>
        Inscribed,

        /// <summary>
        /// 球体满足三角形外接条件。
        /// </summary>
        Circumscribed
    }
}
