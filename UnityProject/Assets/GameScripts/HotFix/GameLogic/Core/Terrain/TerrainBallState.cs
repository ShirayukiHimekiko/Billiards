using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 球当前所处的地形互斥阶段；同一时刻最多只有一个持续阶段。
    /// </summary>
    public enum TerrainPhase
    {
        /// <summary>
        /// 未处于任何持续地形效果。
        /// </summary>
        Normal,

        /// <summary>
        /// 跳台腾空，走完飞行路程或先停球后落地。
        /// </summary>
        Airborne,

        /// <summary>
        /// 正在沿隧道通道穿行。
        /// </summary>
        InTunnel,

        /// <summary>
        /// 球径与通道内径相等，卡在通道内，需外部撞击或整关重置解除。
        /// </summary>
        TunnelBlocked,

        /// <summary>
        /// 陷入地陷并停在陷区，需出杆力度或外部撞击解除。
        /// </summary>
        TrappedInPit
    }

    /// <summary>
    /// 球的互斥地形阶段；纯数据，可由预测世界深复制。
    /// </summary>
    public sealed class TerrainBallState
    {
        /// <summary>
        /// 当前阶段。
        /// </summary>
        public TerrainPhase Phase;

        /// <summary>
        /// 当前持续阶段所属地形 ID；无阶段时为零。
        /// </summary>
        public int PhaseTerrainId;

        /// <summary>
        /// 腾空或通道阶段的世界方向单位向量。
        /// </summary>
        public Vector2 PhaseDirection;

        /// <summary>
        /// 腾空阶段剩余飞行路程，单位为世界单位。
        /// </summary>
        public float RemainingDistance;

        /// <summary>
        /// 当前是否处于腾空阶段。
        /// </summary>
        public bool Airborne => Phase == TerrainPhase.Airborne;

        /// <summary>
        /// 当前阶段是否阻止球再次进入其他地形触发区。
        /// </summary>
        public bool SuppressesTerrainEntry => Phase != TerrainPhase.Normal;

        /// <summary>
        /// 当前阶段是否跳过球、道具、球洞与几何门接触，只保留库边与图形条件判定。
        /// </summary>
        public bool SkipsGroundObjects => Phase == TerrainPhase.Airborne;

        /// <summary>
        /// 结束持续阶段；地面与地形的接触记录由 TerrainContactBook 独立维护，不受影响。
        /// </summary>
        public void EndPhase()
        {
            Phase = TerrainPhase.Normal;
            PhaseTerrainId = 0;
            PhaseDirection = Vector2.zero;
            RemainingDistance = 0;
        }

        /// <summary>
        /// 复制可变状态，供预测世界隔离使用。
        /// </summary>
        /// <returns>与当前球地形状态等值的新对象。</returns>
        public TerrainBallState Copy()
        {
            return new TerrainBallState
            {
                Phase = Phase,
                PhaseTerrainId = PhaseTerrainId,
                PhaseDirection = PhaseDirection,
                RemainingDistance = RemainingDistance
            };
        }
    }
}
