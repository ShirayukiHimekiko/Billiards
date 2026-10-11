using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 跳台规则：沿进入瞬间的速度方向起飞，以入口速度换算目标飞行路程。
    /// </summary>
    public sealed class JumpPadRule : TerrainRuleBase
    {
        /// <summary>
        /// 仅接受具备速度的球，避免零速进入时凭地形朝向凭空生成速度。
        /// </summary>
        /// <param name="trajectory">待查询球的当前轨迹。</param>
        /// <param name="terrain">地形摆放及类型参数。</param>
        /// <param name="context">受控模拟上下文。</param>
        /// <returns>最早进入触发区的秒数；零速或无接触时为正无穷。</returns>
        public override double FindEnter(in BallTrajectory trajectory, TerrainPlacementData terrain, TerrainContext context)
        {
            if (trajectory.Velocity.magnitude <= context.StopSpeed)
            {
                return double.PositiveInfinity;
            }

            return base.FindEnter(trajectory, terrain, context);
        }

        /// <summary>
        /// 按入口速度换算飞行路程并进入腾空阶段；速度大小保持不变。
        /// </summary>
        /// <param name="hit">命中的稳定索引与几何信息。</param>
        /// <param name="context">受控模拟上下文。</param>
        public override void Apply(in TerrainHit hit, TerrainContext context)
        {
            SimulatedBall ball = context.Balls[hit.BallIndex];
            TerrainPlacementData terrain = context.GetTerrain(hit.TerrainIndex);
            float speed = ball.State.Velocity.magnitude;
            float flightDistance = Mathf.Clamp(
                speed * terrain.JumpFlightTime * terrain.JumpSpeedMultiplier,
                terrain.JumpMinDistance,
                terrain.JumpMaxDistance);

            ball.Terrain.Phase = TerrainPhase.Airborne;
            ball.Terrain.PhaseTerrainId = terrain.Id;
            ball.Terrain.PhaseDirection = hit.TravelDirection;
            ball.Terrain.RemainingDistance = flightDistance;
        }
    }
}
