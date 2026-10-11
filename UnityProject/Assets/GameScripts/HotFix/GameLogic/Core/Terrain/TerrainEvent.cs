namespace GameLogic
{
    /// <summary>
    /// 地形候选事件类型。
    /// </summary>
    public enum TerrainEventKind
    {
        /// <summary>
        /// 当前轨迹片段内没有地形事件。
        /// </summary>
        None,

        /// <summary>
        /// 从触发区外进入，交由对应规则处理。
        /// </summary>
        Enter,

        /// <summary>
        /// 从触发区内向外离开，解锁该球与该地形的接触记录。
        /// </summary>
        RegionExit,

        /// <summary>
        /// 持续阶段结束，按原因切换或退出阶段。
        /// </summary>
        PhaseEnd
    }

    /// <summary>
    /// 持续阶段的结束原因。
    /// </summary>
    public enum TerrainPhaseEndReason
    {
        /// <summary>
        /// 无结束原因。
        /// </summary>
        None,

        /// <summary>
        /// 跳台腾空走完剩余路程或先停球，落地恢复地面接触。
        /// </summary>
        Landing,

        /// <summary>
        /// 球心越过隧道出口端面。
        /// </summary>
        TunnelExit,

        /// <summary>
        /// 通道内球径跨出容差，进入卡住阶段。
        /// </summary>
        TunnelBlocked
    }

    /// <summary>
    /// 地形候选事件；只携带稳定索引、时刻与结束原因。
    /// </summary>
    public readonly struct TerrainEvent
    {
        /// <summary>
        /// 无事件常量，时间取正无穷便于直接比较。
        /// </summary>
        public static readonly TerrainEvent None = new TerrainEvent(
            TerrainEventKind.None,
            TerrainPhaseEndReason.None,
            0,
            0,
            double.PositiveInfinity);

        /// <summary>
        /// 事件类型。
        /// </summary>
        public readonly TerrainEventKind Kind;

        /// <summary>
        /// 阶段结束原因；非 PhaseEnd 时为零。
        /// </summary>
        public readonly TerrainPhaseEndReason Reason;

        /// <summary>
        /// 球在世界数组中的索引。
        /// </summary>
        public readonly int BallIndex;

        /// <summary>
        /// 地形在关卡数组中的索引。
        /// </summary>
        public readonly int TerrainIndex;

        /// <summary>
        /// 事件相对当前轨迹起点的秒数。
        /// </summary>
        public readonly double Time;

        /// <summary>
        /// 创建地形候选事件。
        /// </summary>
        /// <param name="kind">事件类型。</param>
        /// <param name="reason">阶段结束原因。</param>
        /// <param name="ballIndex">球索引。</param>
        /// <param name="terrainIndex">地形索引。</param>
        /// <param name="time">事件时刻。</param>
        public TerrainEvent(
            TerrainEventKind kind,
            TerrainPhaseEndReason reason,
            int ballIndex,
            int terrainIndex,
            double time)
        {
            Kind = kind;
            Reason = reason;
            BallIndex = ballIndex;
            TerrainIndex = terrainIndex;
            Time = time;
        }

        /// <summary>
        /// 创建进入候选。
        /// </summary>
        /// <param name="ballIndex">球索引。</param>
        /// <param name="terrainIndex">地形索引。</param>
        /// <param name="time">进入时刻。</param>
        /// <returns>进入事件。</returns>
        public static TerrainEvent Enter(int ballIndex, int terrainIndex, double time)
        {
            return new TerrainEvent(TerrainEventKind.Enter, TerrainPhaseEndReason.None, ballIndex, terrainIndex, time);
        }

        /// <summary>
        /// 创建离开候选。
        /// </summary>
        /// <param name="ballIndex">球索引。</param>
        /// <param name="terrainIndex">地形索引。</param>
        /// <param name="time">离开时刻。</param>
        /// <returns>离开事件。</returns>
        public static TerrainEvent RegionExit(int ballIndex, int terrainIndex, double time)
        {
            return new TerrainEvent(TerrainEventKind.RegionExit, TerrainPhaseEndReason.None, ballIndex, terrainIndex, time);
        }

        /// <summary>
        /// 创建阶段结束候选。
        /// </summary>
        /// <param name="ballIndex">球索引。</param>
        /// <param name="terrainIndex">地形索引。</param>
        /// <param name="reason">结束原因。</param>
        /// <param name="time">结束时刻。</param>
        /// <returns>阶段结束事件。</returns>
        public static TerrainEvent PhaseEnd(int ballIndex, int terrainIndex, TerrainPhaseEndReason reason, double time)
        {
            return new TerrainEvent(TerrainEventKind.PhaseEnd, reason, ballIndex, terrainIndex, time);
        }
    }
}
