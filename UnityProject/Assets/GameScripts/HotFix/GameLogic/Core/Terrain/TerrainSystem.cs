using System;
using System.Collections.Generic;
using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 地形模拟协调器：候选接触、规则分发、阶段事件、每球接触记录与快照复制。
    /// </summary>
    public sealed class TerrainSystem
    {
        /// <summary>
        /// 按地形类型注册的无状态规则表，构造世界时初始化一次。
        /// </summary>
        private static readonly IReadOnlyDictionary<TerrainKind, ITerrainRule> Rules =
            new Dictionary<TerrainKind, ITerrainRule>
            {
                { TerrainKind.JumpPad, new JumpPadRule() },
                { TerrainKind.Tunnel, new TunnelRule() },
                { TerrainKind.Pit, new PitRule() },
                { TerrainKind.ReverseBelt, new ReverseBeltRule() },
                { TerrainKind.Teleporter, new TeleporterRule() }
            };

        /// <summary>
        /// 本关布局及类型参数。
        /// </summary>
        private readonly LevelData _level;

        /// <summary>
        /// 与本世界共享的球体数组。
        /// </summary>
        private readonly SimulatedBall[] _balls;

        /// <summary>
        /// 本关物理参数。
        /// </summary>
        private readonly PhysicsConfigData _physics;

        /// <summary>
        /// 每球每地形的接触记录，属于必须随预测副本复制的世界状态。
        /// </summary>
        private readonly TerrainContactBook _contacts;

        /// <summary>
        /// 传给规则的受控模拟上下文。
        /// </summary>
        private readonly TerrainContext _context;

        /// <summary>
        /// 创建地形协调器并按出生位置初始化接触记录。
        /// </summary>
        /// <param name="level">已完成布局校验的关卡快照。</param>
        /// <param name="balls">与本世界共享的球体数组。</param>
        /// <param name="physics">本关物理参数。</param>
        /// <param name="canPlace">候选位置可用性查询，由世界提供。</param>
        public TerrainSystem(LevelData level, SimulatedBall[] balls, PhysicsConfigData physics, Func<Vector2, float, int, bool> canPlace)
        {
            _level = level;
            _balls = balls;
            _physics = physics;
            _contacts = new TerrainContactBook(balls.Length, level.Terrains.Count);
            _context = new TerrainContext(balls, level.Terrains, _contacts, new ContactSolver(), physics, canPlace);

            for (int t = 0; t < level.Terrains.Count; t++)
            {
                if (!Rules.ContainsKey(level.Terrains[t].Kind))
                {
                    throw new ArgumentException($"地形 {level.Terrains[t].Id} 的类型没有注册规则。");
                }
            }

            ResetSpawnContacts();
        }

        /// <summary>
        /// 查找指定球当前轨迹内最早的地形事件。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        /// <param name="trajectory">该球的当前轨迹。</param>
        /// <returns>最早地形事件；无事件时类型为 None。</returns>
        public TerrainEvent FindNext(int ballIndex, in BallTrajectory trajectory)
        {
            if (!_balls[ballIndex].Active)
            {
                return TerrainEvent.None;
            }

            TerrainEvent best = TerrainEvent.None;
            Choose(ref best, FindPhaseEvent(ballIndex, trajectory));

            for (int t = 0; t < _level.Terrains.Count; t++)
            {
                TerrainPlacementData terrain = _level.Terrains[t];

                if (_contacts.IsInside(ballIndex, t))
                {
                    double exit = _context.Solver.CircleExit(trajectory, terrain.Position, terrain.TriggerRadius);

                    if (!double.IsPositiveInfinity(exit))
                    {
                        Choose(ref best, TerrainEvent.RegionExit(ballIndex, t, exit));
                    }

                    continue;
                }

                if (_balls[ballIndex].Terrain.SuppressesTerrainEntry)
                {
                    continue;
                }

                double enter = Rules[terrain.Kind].FindEnter(trajectory, terrain, _context);

                if (!double.IsPositiveInfinity(enter))
                {
                    Choose(ref best, TerrainEvent.Enter(ballIndex, t, enter));
                }
            }

            return best;
        }

        /// <summary>
        /// 提交一次进入事件：先落接触记录，再交给类型规则执行效果。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        /// <param name="terrainIndex">地形在关卡数组中的索引。</param>
        public void ApplyEnter(int ballIndex, int terrainIndex)
        {
            _contacts.SetInside(ballIndex, terrainIndex, true);

            TerrainPlacementData terrain = _level.Terrains[terrainIndex];
            Vector2 velocity = _balls[ballIndex].State.Velocity;
            Vector2 travel = velocity.sqrMagnitude > .000001f ? velocity.normalized : terrain.Direction;
            Rules[terrain.Kind].Apply(
                new TerrainHit(ballIndex, terrainIndex, 0, -travel, travel),
                _context);
        }

        /// <summary>
        /// 提交一次离开事件，解锁该球与该地形的接触记录。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        /// <param name="terrainIndex">地形在关卡数组中的索引。</param>
        public void ApplyRegionExit(int ballIndex, int terrainIndex)
        {
            _contacts.SetInside(ballIndex, terrainIndex, false);
        }

        /// <summary>
        /// 提交一次阶段结束事件，按原因退出或切换阶段。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        /// <param name="terrainIndex">地形在关卡数组中的索引。</param>
        /// <param name="reason">阶段结束原因。</param>
        public void ApplyPhaseEnd(int ballIndex, int terrainIndex, TerrainPhaseEndReason reason)
        {
            SimulatedBall ball = _balls[ballIndex];

            switch (reason)
            {
                case TerrainPhaseEndReason.Landing:
                case TerrainPhaseEndReason.TunnelExit:
                    ball.Terrain.EndPhase();
                    break;
                case TerrainPhaseEndReason.TunnelBlocked:
                    ball.State.Velocity = Vector2.zero;
                    ball.Terrain.Phase = TerrainPhase.TunnelBlocked;
                    ball.Terrain.PhaseTerrainId = _level.Terrains[terrainIndex].Id;
                    break;
            }
        }

        /// <summary>
        /// 把一次球球碰撞冲量回传给参与球，用于解除陷住或通道卡住状态。
        /// </summary>
        /// <param name="ballIndex">参与碰撞的球索引。</param>
        /// <param name="impulse">本次法向冲量大小。</param>
        public void OnBallImpulse(int ballIndex, float impulse)
        {
            SimulatedBall ball = _balls[ballIndex];
            TerrainPhase phase = ball.Terrain.Phase;

            if (phase != TerrainPhase.TrappedInPit && phase != TerrainPhase.TunnelBlocked)
            {
                return;
            }

            TerrainPlacementData terrain = _level.Terrains[TerrainIndex(ball.Terrain.PhaseTerrainId)];
            float threshold = phase == TerrainPhase.TrappedInPit ? terrain.PitEscapeImpulseThreshold : 0;

            if (impulse > threshold
                && _context.CanPlaceAt(ball.State.Position, ball.State.Radius, ballIndex))
            {
                ball.Terrain.EndPhase();
                return;
            }

            // 未达阈值或没有合法脱出位置：吸收本次速度，保持陷住或卡住。
            ball.State.Velocity = Vector2.zero;
        }

        /// <summary>
        /// 按本段实际位移扣除腾空剩余路程；被撞改向后仍以实际走过的路径长度结算。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        /// <param name="traveled">本段前进的实际位移长度。</param>
        public void OnBallAdvanced(int ballIndex, float traveled)
        {
            TerrainBallState state = _balls[ballIndex].Terrain;

            if (state.Phase != TerrainPhase.Airborne)
            {
                return;
            }

            state.RemainingDistance = Mathf.Max(0, state.RemainingDistance - traveled);
        }

        /// <summary>
        /// 查询指定球所在地陷的脱出力度阈值。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        /// <returns>归一化力度阈值；球未陷住时返回零。</returns>
        public float PitEscapePower(int ballIndex)
        {
            TerrainBallState state = _balls[ballIndex].Terrain;

            if (state.Phase != TerrainPhase.TrappedInPit)
            {
                return 0;
            }

            return _level.Terrains[TerrainIndex(state.PhaseTerrainId)].PitEscapePowerThreshold;
        }

        /// <summary>
        /// 白球达到脱出阈值后解除陷住，位置与尺寸保持，随后按常规出杆赋予速度。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        public void ReleaseFromPit(int ballIndex)
        {
            _balls[ballIndex].Terrain.EndPhase();
        }

        /// <summary>
        /// 把另一世界的地形接触记录复制到当前世界，供预测与重置使用。
        /// </summary>
        /// <param name="source">与当前世界球数、地形数一致的源协调器。</param>
        public void CopyFrom(TerrainSystem source)
        {
            source._contacts.CopyTo(_contacts);
        }

        /// <summary>
        /// 按出生位置初始化接触记录；出生即在触发区内的球标记为已进入，不当作进入事件。
        /// </summary>
        private void ResetSpawnContacts()
        {
            for (int t = 0; t < _level.Terrains.Count; t++)
            {
                TerrainPlacementData terrain = _level.Terrains[t];

                for (int b = 0; b < _balls.Length; b++)
                {
                    if (Vector2.Distance(_balls[b].State.Position, terrain.Position) <= terrain.TriggerRadius)
                    {
                        _contacts.SetInside(b, t, true);
                    }
                }
            }
        }

        /// <summary>
        /// 计算当前阶段可能在本轨迹片段内结束的时刻。
        /// </summary>
        /// <param name="ballIndex">球在世界数组中的索引。</param>
        /// <param name="trajectory">该球的当前轨迹。</param>
        /// <returns>阶段结束事件；本段不结束或无持续阶段时为 None。</returns>
        private TerrainEvent FindPhaseEvent(int ballIndex, in BallTrajectory trajectory)
        {
            TerrainBallState state = _balls[ballIndex].Terrain;

            if (state.Phase == TerrainPhase.Airborne)
            {
                int index = TerrainIndex(state.PhaseTerrainId);

                if (index >= 0)
                {
                    return TerrainEvent.PhaseEnd(
                        ballIndex,
                        index,
                        TerrainPhaseEndReason.Landing,
                        FindLanding(trajectory, state.RemainingDistance));
                }
            }
            else if (state.Phase == TerrainPhase.InTunnel)
            {
                int index = TerrainIndex(state.PhaseTerrainId);

                if (index >= 0)
                {
                    TerrainPlacementData terrain = _level.Terrains[index];

                    // 通道内半径跨出容差：立即进入卡住阶段。
                    if (Mathf.Abs(_balls[ballIndex].State.Radius - terrain.TunnelInnerRadius) <= _physics.ContactTolerance)
                    {
                        return TerrainEvent.PhaseEnd(ballIndex, index, TerrainPhaseEndReason.TunnelBlocked, 0);
                    }

                    float offset = Vector2.Dot(state.PhaseDirection, terrain.Position) + terrain.TunnelHalfLength;
                    return TerrainEvent.PhaseEnd(
                        ballIndex,
                        index,
                        TerrainPhaseEndReason.TunnelExit,
                        _context.Solver.PlaneExit(trajectory, state.PhaseDirection, offset));
                }
            }

            return TerrainEvent.None;
        }

        /// <summary>
        /// 求本段轨迹内腾空剩余路程走完或先停球的时刻。
        /// </summary>
        /// <param name="trajectory">腾空球的当前轨迹。</param>
        /// <param name="remaining">剩余飞行路程。</param>
        /// <returns>落地秒数；本段内不落地时为正无穷。</returns>
        private double FindLanding(in BallTrajectory trajectory, float remaining)
        {
            if (remaining <= 0)
            {
                return 0;
            }

            double speed = trajectory.Velocity.magnitude;
            double deceleration = trajectory.Acceleration.magnitude;

            if (speed <= _physics.StopSpeed)
            {
                return 0;
            }

            if (deceleration <= 1e-9)
            {
                double constant = remaining / speed;
                return constant <= trajectory.Duration + 1e-9 ? constant : double.PositiveInfinity;
            }

            double discriminant = speed * speed - 2 * deceleration * remaining;
            double time = discriminant <= 0
                ? speed / deceleration
                : (speed - Math.Sqrt(discriminant)) / deceleration;

            return time <= trajectory.Duration + 1e-9 ? time : double.PositiveInfinity;
        }

        /// <summary>
        /// 按地形 ID 查询关卡数组索引。
        /// </summary>
        /// <param name="id">地形明细 ID。</param>
        /// <returns>关卡数组索引；不存在时返回负值。</returns>
        private int TerrainIndex(int id)
        {
            for (int i = 0; i < _level.Terrains.Count; i++)
            {
                if (_level.Terrains[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 只保留更早的地形事件，同刻按进入、离开、阶段结束的固定次序。
        /// </summary>
        /// <param name="best">当前最早事件。</param>
        /// <param name="candidate">候选事件。</param>
        private static void Choose(ref TerrainEvent best, in TerrainEvent candidate)
        {
            if (candidate.Kind == TerrainEventKind.None || double.IsPositiveInfinity(candidate.Time))
            {
                return;
            }

            if (best.Kind == TerrainEventKind.None
                || candidate.Time < best.Time - 1e-8
                || Math.Abs(candidate.Time - best.Time) <= 1e-8
                && Priority(candidate.Kind) < Priority(best.Kind))
            {
                best = candidate;
            }
        }

        /// <summary>
        /// 地形事件内部的同刻优先级。
        /// </summary>
        /// <param name="kind">事件类型。</param>
        /// <returns>数值越小越先处理。</returns>
        private static int Priority(TerrainEventKind kind)
        {
            return kind == TerrainEventKind.Enter ? 0 : kind == TerrainEventKind.RegionExit ? 1 : 2;
        }
    }
}
