using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 地陷规则：进入后球心定位到陷区并停住，仍需参与球-球碰撞解算。
    /// </summary>
    public sealed class PitRule : TerrainRuleBase
    {
        /// <summary>
        /// 把球停在陷区中心并进入陷住阶段；不改变活动标记，也不标记入洞。
        /// </summary>
        /// <param name="hit">命中的稳定索引与几何信息。</param>
        /// <param name="context">受控模拟上下文。</param>
        public override void Apply(in TerrainHit hit, TerrainContext context)
        {
            SimulatedBall ball = context.Balls[hit.BallIndex];
            TerrainPlacementData terrain = context.GetTerrain(hit.TerrainIndex);

            ball.State.Position = terrain.Position;
            ball.State.Velocity = Vector2.zero;
            ball.Terrain.Phase = TerrainPhase.TrappedInPit;
            ball.Terrain.PhaseTerrainId = terrain.Id;
            ball.Terrain.PhaseDirection = Vector2.zero;
            ball.Terrain.RemainingDistance = 0;
        }
    }
}
