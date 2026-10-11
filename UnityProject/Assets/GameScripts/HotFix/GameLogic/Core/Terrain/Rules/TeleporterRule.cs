using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 传送口规则：把球瞬移到已校验的配对出口，世界速度矢量逐分量保留。
    /// </summary>
    public sealed class TeleporterRule : TerrainRuleBase
    {
        /// <summary>
        /// 仅接受具备速度的球；零速无法定义出口保向方向。
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
        /// 把球移到出口安全区并保留速度；出口被占用或越界时取消本次传送。
        /// </summary>
        /// <param name="hit">命中的稳定索引与几何信息。</param>
        /// <param name="context">受控模拟上下文。</param>
        public override void Apply(in TerrainHit hit, TerrainContext context)
        {
            SimulatedBall ball = context.Balls[hit.BallIndex];
            TerrainPlacementData entry = context.GetTerrain(hit.TerrainIndex);
            TerrainPlacementData exit = context.TerrainById(entry.ExitId);
            Vector2 velocity = ball.State.Velocity;
            Vector2 travel = velocity.normalized;
            Vector2 destination = exit.Position
                + travel * (exit.TriggerRadius + ball.State.Radius + context.ContactTolerance);

            if (!context.CanPlaceAt(destination, ball.State.Radius, hit.BallIndex))
            {
                return;
            }

            // 传送使球物理离开原位置，先清空它对全部地形的接触记录，避免记录长期不同步。
            for (int t = 0; t < context.Terrains.Count; t++)
            {
                context.Contacts.SetInside(hit.BallIndex, t, false);
            }

            ball.State.Position = destination;
            ball.State.Velocity = velocity;

            // 落点若仍在出口触发区内，标记为已进入，避免同一时刻回跳。
            if (Vector2.Distance(destination, exit.Position) <= exit.TriggerRadius)
            {
                context.Contacts.SetInside(hit.BallIndex, context.TerrainIndex(exit.Id), true);
            }
        }
    }
}
