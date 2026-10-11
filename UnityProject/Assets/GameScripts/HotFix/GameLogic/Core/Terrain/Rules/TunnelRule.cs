using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 隧道规则：按球径与通行口内径比较决定穿行、卡住或弹开，出口由端面几何事件触发。
    /// </summary>
    public sealed class TunnelRule : TerrainRuleBase
    {
        /// <summary>
        /// 仅接受具备速度的球，避免零速时无法定义穿行方向。
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
        /// 比较球直径与通行口内径：小则沿通道轴穿行，等则卡住，大则沿入口面法线弹开。
        /// </summary>
        /// <param name="hit">命中的稳定索引与几何信息。</param>
        /// <param name="context">受控模拟上下文。</param>
        public override void Apply(in TerrainHit hit, TerrainContext context)
        {
            SimulatedBall ball = context.Balls[hit.BallIndex];
            TerrainPlacementData terrain = context.GetTerrain(hit.TerrainIndex);
            float difference = ball.State.Radius - terrain.TunnelInnerRadius;
            float tolerance = context.ContactTolerance;

            if (difference > tolerance)
            {
                TerrainContext.ReflectAtEntrance(ball, hit.EntryNormal);
                return;
            }

            if (difference < -tolerance)
            {
                // 通道内对齐通道轴并保持速率，出口由端面几何事件触发。
                Vector2 axis = Vector2.Dot(hit.TravelDirection, terrain.Direction) >= 0
                    ? terrain.Direction
                    : -terrain.Direction;
                ball.State.Velocity = axis * ball.State.Velocity.magnitude;
                ball.Terrain.Phase = TerrainPhase.InTunnel;
                ball.Terrain.PhaseTerrainId = terrain.Id;
                ball.Terrain.PhaseDirection = axis;
                ball.Terrain.RemainingDistance = 0;
                return;
            }

            ball.State.Velocity = Vector2.zero;
            ball.Terrain.Phase = TerrainPhase.TunnelBlocked;
            ball.Terrain.PhaseTerrainId = terrain.Id;
        }
    }
}
