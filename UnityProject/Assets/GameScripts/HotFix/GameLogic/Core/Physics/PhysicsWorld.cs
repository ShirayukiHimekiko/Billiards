using System;
using System.Collections.Generic;
using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 确定性桌球世界；表现、输入和资源加载不参与物理求解。
    /// </summary>
    public sealed class PhysicsWorld
    {
        /// <summary>
        /// 连续接触事件类型，枚举顺序决定同刻事件的稳定优先级。
        /// </summary>
        private enum EventKind
        {
            /// <summary>
            /// 当前时间片段没有接触事件。
            /// </summary>
            None,

            /// <summary>
            /// 图形条件达成，同刻事件优先处理。
            /// </summary>
            Goal,

            /// <summary>
            /// 球接触可用道具。
            /// </summary>
            Prop,

            /// <summary>
            /// 球心进入开放球洞的捕获范围。
            /// </summary>
            Pocket,

            /// <summary>
            /// 球进入地形触发范围。
            /// </summary>
            Terrain,

            /// <summary>
            /// 两个活动球首次向内接触。
            /// </summary>
            Ball,

            /// <summary>
            /// 球体接触未开放洞口的库边。
            /// </summary>
            Rail,

            /// <summary>
            /// 球体接触尚未放行的几何门。
            /// </summary>
            Gate
        }

        /// <summary>
        /// 当前时间片段中最早接触事件的数据。
        /// </summary>
        private struct Contact
        {
            /// <summary>
            /// 相对于当前连续轨迹起点的接触秒数。
            /// </summary>
            public double Time;

            /// <summary>
            /// 事件类型，同时用于同刻事件的优先级比较。
            /// </summary>
            public EventKind Kind;

            /// <summary>
            /// 发生接触的球在世界数组中的索引。
            /// </summary>
            public int Ball;

            /// <summary>
            /// 另一球、图形、道具、球洞或库边的索引。
            /// </summary>
            public int Other;

            /// <summary>
            /// 库边事件使用的内法线。
            /// </summary>
            public Vector2 Normal;
        }

        /// <summary>
        /// 世界共用的关卡布局及物理配置。
        /// </summary>
        private readonly LevelData _level;

        /// <summary>
        /// 与道具配置顺序对应的无场景依赖效果策略。
        /// </summary>
        private readonly IReadOnlyList<IPropEffect> _propEffects;

        /// <summary>
        /// 计算球、库边及圆形障碍接触时间的求解器。
        /// </summary>
        private readonly ContactSolver _contacts = new ContactSolver();

        /// <summary>
        /// 查询图形条件最早达成时间的判定器。
        /// </summary>
        private readonly GoalJudge _goals = new GoalJudge();

        /// <summary>
        /// 本次连续时间片段内所有球的轨迹缓存。
        /// </summary>
        private readonly BallTrajectory[] _trajectories;

        /// <summary>
        /// 图形索引乘球数再加球索引得到的本次接近或出杆初始重叠放行标记。
        /// </summary>
        private readonly bool[] _admitted;

        /// <summary>
        /// 按图形和球索引记录已触发条件，离开条件区后解除。
        /// </summary>
        private readonly bool[] _conditionLatched;

        /// <summary>
        /// 由当前杆初速和基准路程推导的共用减速度。
        /// </summary>
        private float _deceleration;

        /// <summary>
        /// 接近预测中忽略的当前几何门索引，负值表示不忽略。
        /// </summary>
        private int _ignoredGeometry = -1;

        /// <summary>
        /// 为真时忽略全部几何门阻挡，供预测线按"图形不参与碰撞"绘制。
        /// </summary>
        private bool _ignoreGates;

        /// <summary>
        /// 是否为几何门的单次接近查询，防止递归预测其他门。
        /// </summary>
        private bool _dryApproach;

        /// <summary>
        /// 接近查询是否已被其他几何门截断。
        /// </summary>
        private bool _approachEnded;

        /// <summary>
        /// 白球是否已在本杆图形目标处停止；停止后保持位置，但仍作为实体参与其他球的反弹。
        /// </summary>
        private bool _whiteStoppedAtGoal;

        /// <summary>
        /// 按关卡摆放顺序保存的可变球体模拟状态。
        /// </summary>
        public readonly SimulatedBall[] Balls;

        /// <summary>
        /// 与关卡球洞顺序对应的当前洞状态。
        /// </summary>
        public readonly PocketState[] PocketStates;

        /// <summary>
        /// 与关卡道具顺序对应的使用次数，按各自作用域重置。
        /// </summary>
        public readonly int[] PropUses;

        /// <summary>
        /// 与关卡地形顺序对应的运行状态。
        /// </summary>
        public readonly TerrainRuntimeState[] TerrainStates;

        /// <summary>
        /// 与关卡图形顺序对应的效果完成标记。
        /// </summary>
        public readonly bool[] GeometryCompleted;

        /// <summary>
        /// 各道具效果剩余作用杆数；预测副本与真实世界一起复制。
        /// </summary>
        public readonly int[] EffectRemaining;

        /// <summary>
        /// 当前会话是否处于显影效果中。
        /// </summary>
        public bool RevealActive
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前会话是否处于预演效果中。
        /// </summary>
        public bool PreviewActive
        {
            get;
            private set;
        }

        /// <summary>
        /// 本次模拟待由会话结算的额外杆数。
        /// </summary>
        public int ShotBonus
        {
            get;
            private set;
        }

        /// <summary>
        /// 与关卡图形顺序对应的几何门失败次数。
        /// </summary>
        public readonly int[] GeometryFailureCounts;

        /// <summary>
        /// 最近一次几何失败反馈；由表现层消费后清除。
        /// </summary>
        private GoalFeedback _pendingGoalFeedback;

        /// <summary>
        /// 是否存在待消费的几何失败反馈。
        /// </summary>
        private bool _hasPendingGoalFeedback;

        /// <summary>
        /// 唯一白球在球体数组中的索引。
        /// </summary>
        public readonly int WhiteIndex;

        /// <summary>
        /// 本杆是否白球落袋。
        /// </summary>
        public bool WhiteFoul
        {
            get;
            private set;
        }

        /// <summary>
        /// 消费最近一次几何失败反馈，避免表现层重复显示同一事件。
        /// </summary>
        /// <param name="feedback">读取到的失败反馈。</param>
        /// <returns>存在待消费反馈时为 true。</returns>
        public bool TryConsumeGoalFeedback(out GoalFeedback feedback)
        {
            if (!_hasPendingGoalFeedback)
            {
                feedback = default;

                return false;
            }

            feedback = _pendingGoalFeedback;
            _hasPendingGoalFeedback = false;

            return true;
        }

        /// <summary>
        /// 状态版本用于按事件刷新界面。
        /// </summary>
        public int Revision
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前是否还有运动球。
        /// </summary>
        public bool Moving
        {
            get
            {
                foreach (var ball in Balls)
                {
                    if (ball.Active && ball.State.Velocity.sqrMagnitude > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 仍在台面的黑球数。
        /// </summary>
        public int RemainingBlack
        {
            get
            {
                int count = 0;

                foreach (var ball in Balls)
                {
                    if (ball.Active && ball.Data.Config.Kind == BallKind.Black)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// 创建本关模拟世界。
        /// </summary>
        /// <param name="level">已完成布局校验的关卡快照。</param>
        /// <param name="propEffects">与道具配置顺序对应的策略；为空时按本关道具类型创建。</param>
        public PhysicsWorld(LevelData level, IReadOnlyList<IPropEffect> propEffects = null)
        {
            _level = level;

            if (propEffects == null)
            {
                var effects = new IPropEffect[level.Props.Count];

                for (int i = 0; i < effects.Length; i++)
                {
                    effects[i] = PropEffects.Get(level.Props[i].Kind);
                }

                _propEffects = effects;
            }
            else
            {
                _propEffects = propEffects;
            }

            Balls = new SimulatedBall[level.Balls.Count];

            for (int i = 0; i < Balls.Length; i++)
            {
                Balls[i] = new SimulatedBall(level.Balls[i]);

                if (Balls[i].Data.Config.Kind == BallKind.White)
                {
                    WhiteIndex = i;
                }
            }

            PocketStates = new PocketState[level.Pockets.Count];
            PropUses = new int[level.Props.Count];
            TerrainStates = new TerrainRuntimeState[level.Terrains.Count];
            GeometryCompleted = new bool[level.Geometries.Count];
            GeometryFailureCounts = new int[level.Geometries.Count];
            _admitted = new bool[level.Geometries.Count * Balls.Length];
            _conditionLatched = new bool[_admitted.Length];
            _trajectories = new BallTrajectory[Balls.Length];

            for (int i = 0; i < PocketStates.Length; i++)
            {
                PocketStates[i] = level.Pockets[i].InitialState;
            }

            for (int i = 0; i < TerrainStates.Length; i++)
            {
                TerrainStates[i] = new TerrainRuntimeState();
            }

            EffectRemaining = new int[level.Props.Count];
        }

        /// <summary>
        /// 复制进度和运动状态，预测不能修改真实世界。
        /// </summary>
        /// <returns>球状态及进度数组独立、共享配置和无状态策略的世界副本。</returns>
        public PhysicsWorld Copy()
        {
            var copy = new PhysicsWorld(_level, _propEffects)
            {
                _deceleration = _deceleration,
                _whiteStoppedAtGoal = _whiteStoppedAtGoal,
                WhiteFoul = WhiteFoul,
                Revision = Revision,
                RevealActive = RevealActive,
                PreviewActive = PreviewActive,
                ShotBonus = ShotBonus
            };

            for (int i = 0; i < Balls.Length; i++)
            {
                copy.Balls[i] = Balls[i].Copy();
            }

            Array.Copy(PocketStates, copy.PocketStates, PocketStates.Length);
            Array.Copy(PropUses, copy.PropUses, PropUses.Length);
            for (int i = 0; i < TerrainStates.Length; i++)
            {
                copy.TerrainStates[i] = TerrainStates[i].Copy();
            }
            Array.Copy(EffectRemaining, copy.EffectRemaining, EffectRemaining.Length);
            Array.Copy(GeometryCompleted, copy.GeometryCompleted, GeometryCompleted.Length);
            Array.Copy(GeometryFailureCounts, copy.GeometryFailureCounts, GeometryFailureCounts.Length);
            Array.Copy(_admitted, copy._admitted, _admitted.Length);
            Array.Copy(_conditionLatched, copy._conditionLatched, _conditionLatched.Length);

            return copy;
        }

        /// <summary>
        /// 开始一杆并按力度重置白球初始半径、变化速率和初速度。
        /// </summary>
        /// <param name="direction">非零的击球方向，内部归一化后应用。</param>
        /// <param name="power">调用方提供的零到一归一化力度。</param>
        /// <returns>是否成功开始一杆；白球当前半径超出台面或与其他活动球重叠时返回 false。</returns>
        public bool Shoot(Vector2 direction, float power)
        {
            var white = Balls[WhiteIndex];
            var config = white.Data.Config;

            if (!TryReleasePit(white, power, config))
            {
                return false;
            }

            if (!CanPlace(white.State.Position, white.State.Radius, WhiteIndex))
            {
                return false;
            }

            WhiteFoul = false;
            _whiteStoppedAtGoal = false;
            _hasPendingGoalFeedback = false;

            // 新杆只重置 Shot 作用域记录；球洞解锁和整关道具进度继续保留。
            for (int i = 0; i < PropUses.Length; i++)
            {
                if (_level.Props[i].UseScope == UseScope.Shot)
                {
                    PropUses[i] = 0;
                }
            }

            for (int i = 0; i < GeometryCompleted.Length; i++)
            {
                if (!_level.Geometries[i].UnlocksPocket
                    && _level.Props[PropIndex(_level.Geometries[i].TargetId)].UseScope == UseScope.Shot)
                {
                    GeometryCompleted[i] = false;
                }
            }

            Array.Clear(_admitted, 0, _admitted.Length);
            Array.Clear(_conditionLatched, 0, _conditionLatched.Length);
            AdvanceEffectDurations();

            float speed = Mathf.Lerp(config.SpeedMin, config.SpeedMax, power);
            float distance = Mathf.Lerp(config.DistanceMin, config.DistanceMax, power);
            // 用无碰撞基准路程推导本杆共用减速度，碰撞后各球的实际路程自行演化。
            _deceleration = speed * speed / (2 * distance);
            white.State.Radius = Mathf.Lerp(config.ShotRadiusMin, config.ShotRadiusMax, power);
            white.BaseRadius = white.State.Radius;
            white.RadiusDirection = 1;
            white.RateMultiplier = 1;
            white.GrowRate = Mathf.Lerp(config.GrowRateMin, config.GrowRateMax, power);
            ApplyActiveEffects(white);
            white.State.Velocity = direction.normalized * speed;
            AdmitOverlappingGeometries();
            Revision++;

            return true;
        }

        /// <summary>
        /// 消耗一杆时推进持续效果，并刷新表现状态。
        /// </summary>
        private void AdvanceEffectDurations()
        {
            RevealActive = false;
            PreviewActive = false;

            for (int i = 0; i < EffectRemaining.Length; i++)
            {
                if (EffectRemaining[i] > 0)
                {
                    EffectRemaining[i]--;
                }

                if (EffectRemaining[i] != 0)
                {
                    PropEffectKind kind = _level.Props[i].Kind;
                    RevealActive |= kind == PropEffectKind.Reveal;
                    PreviewActive |= kind == PropEffectKind.Preview;
                }
            }
        }

        /// <summary>
        /// 将仍在生效的物理道具应用到新杆白球。
        /// </summary>
        private void ApplyActiveEffects(SimulatedBall white)
        {
            for (int i = 0; i < EffectRemaining.Length; i++)
            {
                if (EffectRemaining[i] == 0)
                {
                    continue;
                }

                PropEffectKind kind = _level.Props[i].Kind;
                if (kind == PropEffectKind.Reveal || kind == PropEffectKind.Preview || kind == PropEffectKind.AddShot || kind == PropEffectKind.Key)
                {
                    continue;
                }

                _propEffects[i].Apply(white, _level.Props[i].RateMultiplier);
            }
        }

        /// <summary>
        /// 取出本次物理推进产生的额外杆数。
        /// </summary>
        public int ConsumeShotBonus()
        {
            int bonus = ShotBonus;
            ShotBonus = 0;
            return bonus;
        }

        /// <summary>
        /// 出杆时已与目标圆重叠的球先自然离开，避免启用阻挡时被推到圆外。
        /// </summary>
        private void AdmitOverlappingGeometries()
        {
            for (int g = 0; g < _level.Geometries.Count; g++)
            {
                if (!GeometryAvailable(g))
                {
                    continue;
                }

                var geometry = _level.Geometries[g];

                for (int b = 0; b < Balls.Length; b++)
                {
                    var ball = Balls[b];

                    // 仅放行初始穿透；边界外的接近仍由运动中的条件预测决定。
                    Vector2 boundaryNormal;
                    if (ball.Active
                        && geometry.MinimumBoundaryDistance(ball.State.Position, out boundaryNormal) < ball.State.Radius)
                    {
                        _admitted[g * Balls.Length + b] = true;
                    }
                }
            }
        }

        /// <summary>
        /// 使用连续事件推进一个固定步，全部球共享同一时间轴。
        /// </summary>
        /// <param name="duration">需要推进的固定时间片段，单位为秒。</param>
        public void Step(double duration)
        {
            Step(duration, -1, null);
        }

        /// <summary>
        /// 使用连续事件推进一个固定步，并按需记录指定球的精确预测分段。
        /// </summary>
        /// <param name="duration">需要推进的固定时间片段，单位为秒。</param>
        /// <param name="trackedBall">需要记录轨迹的球索引；负值表示不记录。</param>
        /// <param name="prediction">复用的预测路径结果；正式模拟时为空。</param>
        private void Step(double duration, int trackedBall, PredictionPath prediction)
        {
            UpdateTerrainTimers((float)duration);
            double remaining = duration;
            int iterations = 0;

            while (remaining > 1e-9 && Moving && !_approachEnded)
            {
                if (++iterations > _level.Physics.MaxEventIterations)
                {
                    throw new InvalidOperationException("桌球同时接触事件超过配置上限，请调整布局或接触容差。");
                }

                double segment = Prepare(remaining);
                Contact hit = FindContact(segment);
                double advance = hit.Kind == EventKind.None ? segment : hit.Time;
                bool trackingMovement = prediction != null
                    && !prediction.EndedByBallContact
                    && Balls[trackedBall].Active
                    && Balls[trackedBall].State.Velocity.sqrMagnitude > 0;
                // 全部球先推进到同一事件时刻，再响应接触，保持共享时间轴。
                Advance(advance);
                remaining -= advance;

                if (trackingMovement)
                {
                    RecordPredictionPoint(hit, trackedBall, prediction);
                }

                if (hit.Kind != EventKind.None)
                {
                    bool trackedBallContact = trackingMovement
                        && hit.Kind == EventKind.Ball
                        && (hit.Ball == trackedBall || hit.Other == trackedBall);
                    int contactTargetIndex = trackedBallContact
                        ? hit.Ball == trackedBall ? hit.Other : hit.Ball
                        : -1;
                    Vector2 contactCenter = trackedBallContact ? Balls[trackedBall].State.Position : Vector2.zero;
                    float contactRadius = trackedBallContact ? Balls[trackedBall].State.Radius : 0;
                    Vector2 targetCenter = trackedBallContact ? Balls[contactTargetIndex].State.Position : Vector2.zero;
                    float targetRadius = trackedBallContact ? Balls[contactTargetIndex].State.Radius : 0;
                    bool splitPrediction = trackingMovement
                        && PredictionEventSplitsPath(hit, trackedBall, prediction);
                    bool visibleRailJunction = hit.Kind == EventKind.Rail && hit.Ball == trackedBall;
                    Resolve(hit);

                    if (trackedBallContact)
                    {
                        Vector2 targetVelocity = Balls[contactTargetIndex].State.Velocity;

                        if (targetVelocity.magnitude <= _level.Physics.StopSpeed)
                        {
                            targetVelocity = Vector2.zero;
                        }

                        prediction.AddBallContact(
                            contactCenter,
                            contactRadius,
                            targetCenter,
                            targetRadius,
                            targetVelocity);
                        prediction.FinishSegment(true);
                    }
                    else if (splitPrediction)
                    {
                        prediction.FinishSegment(visibleRailJunction);

                        var tracked = Balls[trackedBall];

                        if (tracked.Active && tracked.State.Velocity.sqrMagnitude > 0)
                        {
                            prediction.BeginSegment(tracked.State.Position, visibleRailJunction);
                        }
                    }
                }

                ClearAdmissions();
            }
        }

        /// <summary>
        /// 记录推进后的白球球心；撞库时额外保存动态半径与法线。
        /// </summary>
        /// <param name="hit">当前时间片段的接触事件。</param>
        /// <param name="trackedBall">需要记录轨迹的球索引。</param>
        /// <param name="prediction">复用的预测路径结果。</param>
        private void RecordPredictionPoint(Contact hit, int trackedBall, PredictionPath prediction)
        {
            var tracked = Balls[trackedBall];
            prediction.AddPoint(tracked.State.Position);

            if (hit.Kind == EventKind.Rail && hit.Ball == trackedBall)
            {
                prediction.AddRailContact(tracked.State.Position, tracked.State.Radius, hit.Normal);
            }
        }

        /// <summary>
        /// 判断接触响应是否会改变被记录球的方向或进行数值位置修正。
        /// </summary>
        /// <param name="hit">当前接触事件。</param>
        /// <param name="trackedBall">需要记录轨迹的球索引。</param>
        /// <param name="prediction">预测路径；为空时不分段。</param>
        /// <returns>响应事件后是否需要另起显示分段。</returns>
        private static bool PredictionEventSplitsPath(Contact hit, int trackedBall, PredictionPath prediction)
        {
            if (prediction == null)
            {
                return false;
            }

            return (hit.Kind == EventKind.Rail || hit.Kind == EventKind.Gate) && hit.Ball == trackedBall;
        }

        /// <summary>
        /// 生成在停球、半径上下限处截断的所有轨迹。
        /// </summary>
        /// <param name="budget">当前固定步尚未推进的秒数。</param>
        /// <returns>在停球或半径上下限处截断后的轨迹片段时长。</returns>
        private double Prepare(double budget)
        {
            double segment = budget;

            for (int i = 0; i < Balls.Length; i++)
            {
                var ball = Balls[i];
                float speed = ball.State.Velocity.magnitude;

                if (!ball.Active || speed <= _level.Physics.StopSpeed)
                {
                    ball.State.Velocity = Vector2.zero;
                    continue;
                }

                segment = Math.Min(segment, speed / _deceleration);

                float rate = RadiusRate(ball);
                float limit = rate > 0 ? ball.Data.Config.RadiusMax : ball.Data.Config.RadiusMin;

                if (Math.Abs(rate) > .000001)
                {
                    segment = Math.Min(segment, (limit - ball.State.Radius) / rate);
                }
            }

            for (int i = 0; i < Balls.Length; i++)
            {
                var ball = Balls[i];
                Vector2 velocity = ball.State.Velocity;
                _trajectories[i] = new BallTrajectory(ball.State, velocity.sqrMagnitude > 0
                    ? -velocity.normalized * _deceleration : Vector2.zero, ball.Active
                    && velocity.sqrMagnitude > 0
                    ? RadiusRate(ball) : 0, segment);
            }

            return segment;
        }

        /// <summary>
        /// 计算半径变化率，同时限制贴边增大造成的持续穿透。
        /// </summary>
        /// <param name="ball">需要计算尺寸变化的模拟球。</param>
        /// <returns>每秒半径变化量；黑球、尺寸到限或增大受阻时为零。</returns>
        private float RadiusRate(SimulatedBall ball)
        {
            if (ball.Data.Config.Kind == BallKind.Black)
            {
                return 0;
            }

            var state = ball.State;
            var config = ball.Data.Config;
            float rate = ball.RadiusDirection * ball.GrowRate * ball.RateMultiplier;

            if (ball.Growing
                && state.Radius >= config.RadiusMax - .000001f
                || !ball.Growing
                && state.Radius <= config.RadiusMin + .000001f)
            {
                return 0;
            }

            if (rate > 0)
            {
                float margin = Mathf.Min(
                    state.Position.x - _level.Bounds.xMin,
                    _level.Bounds.xMax - state.Position.x,
                    state.Position.y - _level.Bounds.yMin,
                    _level.Bounds.yMax - state.Position.y);

                if (margin <= state.Radius + _level.Physics.ContactTolerance * 2)
                {
                    return 0;
                }

                foreach (var other in Balls)
                {
                    if (other != ball
                        && other.Active
                        && Vector2.Distance(state.Position, other.State.Position) <= state.Radius + other.State.Radius + _level.Physics.ContactTolerance * 2)
                    {
                        return 0;
                    }
                }
            }

            return rate;
        }

        /// <summary>
        /// 按稳定优先级查找世界最早事件。
        /// </summary>
        /// <param name="duration">当前轨迹片段时长，单位为秒。</param>
        /// <returns>最早事件；没有事件时类型为 None，时间为片段终点。</returns>
        private Contact FindContact(double duration)
        {
            Contact hit = new Contact
            {
                Time = duration,
                Kind = EventKind.None
            };

            for (int i = 0; i < Balls.Length; i++)
            {
                if (!Balls[i].Active)
                {
                    continue;
                }

                var trajectory = _trajectories[i];
                var ball = Balls[i];

                for (int g = 0; g < _level.Geometries.Count; g++)
                {
                    if (!GeometryAvailable(g))
                    {
                        continue;
                    }

                    var geometry = _level.Geometries[g];

                    if (ball.Data.Config.Kind == geometry.AllowedBallKind && !_conditionLatched[g * Balls.Length + i])
                    {
                        Choose(ref hit, _goals.FindGoal(geometry, trajectory), EventKind.Goal, i, g, Vector2.zero);
                    }
                }

                for (int p = 0; p < _level.Props.Count; p++)
                {
                    var prop = _level.Props[p];

                    if (ball.Data.Config.Kind == BallKind.White && prop.TriggerMode == TriggerMode.Contact && PropUses[p] < prop.UseLimit)
                    {
                        Choose(
                            ref hit,
                            _contacts.Circle(trajectory, Stationary(prop.Position, prop.TriggerRadius, duration)),
                            EventKind.Prop,
                            i,
                            p,
                            Vector2.zero);
                    }
                }

                for (int t = 0; t < _level.Terrains.Count; t++)
                {
                    TerrainPlacementData terrain = _level.Terrains[t];
                    TerrainRuntimeState terrainState = TerrainStates[t];

                    if (terrainState.EntryLocked
                        || terrainState.CooldownRemaining > 0
                        || ball.Terrain.TerrainId == terrain.Id
                        || ball.Terrain.SuppressesSurfaceContacts && terrain.Kind != TerrainKind.Teleporter)
                    {
                        continue;
                    }

                    Choose(
                        ref hit,
                        _contacts.Circle(trajectory, Stationary(terrain.Position, terrain.TriggerRadius, duration), true),
                        EventKind.Terrain,
                        i,
                        t,
                        Vector2.zero);
                }

                for (int p = 0; p < _level.Pockets.Count; p++)
                {
                    var pocket = _level.Pockets[p];

                    if (PocketStates[p] == PocketState.Unlocked && ball.State.Radius <= pocket.CaptureRadius)
                    {
                        Choose(
                            ref hit,
                            _contacts.Circle(trajectory, Stationary(pocket.Position, pocket.CaptureRadius, duration), true),
                            EventKind.Pocket,
                            i,
                            p,
                            Vector2.zero);
                    }
                }

                for (int j = i + 1; j < Balls.Length; j++)
                {
                    if (Balls[j].Active)
                    {
                        Choose(ref hit, _contacts.Circle(trajectory, _trajectories[j]), EventKind.Ball, i, j, Vector2.zero);
                    }
                }

                Rail(ref hit, i, Vector2.right, _level.Bounds.xMin, 0);
                Rail(ref hit, i, Vector2.left, -_level.Bounds.xMax, 1);
                Rail(ref hit, i, Vector2.up, _level.Bounds.yMin, 2);
                Rail(ref hit, i, Vector2.down, -_level.Bounds.yMax, 3);

                for (int g = 0; g < _level.Geometries.Count; g++)
                {
                    if (_ignoreGates || g == _ignoredGeometry || !GeometryAvailable(g) || _admitted[g * Balls.Length + i] || ball.Terrain.Airborne)
                    {
                        continue;
                    }

                    var geometry = _level.Geometries[g];
                    Vector2 normal;
                    double t = _contacts.Triangle(trajectory, geometry.A, geometry.B, geometry.C, out normal);
                    Choose(ref hit, t, EventKind.Gate, i, g, normal);
                }
            }

            return hit;
        }

        /// <summary>
        /// 洞口开放时让球跨过边界，关闭时恢复整条库边。
        /// </summary>
        /// <param name="hit">当前最早事件，可被更早的库边事件替换。</param>
        /// <param name="ball">待查询球的世界数组索引。</param>
        /// <param name="normal">库边指向台面内部的单位法线。</param>
        /// <param name="offset">库边在法线方向的投影偏移。</param>
        /// <param name="side">库边索引：零为左，一为右，二为下，三为上。</param>
        private void Rail(
            ref Contact hit,
            int ball,
            Vector2 normal,
            float offset,
            int side)
        {
            double time = _contacts.Plane(_trajectories[ball], normal, offset);

            if (double.IsPositiveInfinity(time))
            {
                return;
            }

            BallState state = _trajectories[ball].At(time);

            for (int p = 0; p < _level.Pockets.Count; p++)
            {
                var pocket = _level.Pockets[p];
                bool onSide = side == 0
                    ? (int)pocket.Slot % 3 == 0 : side == 1
                    ? (int)pocket.Slot % 3 == 2 : side == 2
                    ? (int)pocket.Slot >= 3 : (int)pocket.Slot < 3;
                float lateral = side < 2
                    ? Mathf.Abs(state.Position.y - pocket.Position.y) : Mathf.Abs(state.Position.x - pocket.Position.x);

                if (onSide
                    && PocketStates[p] == PocketState.Unlocked
                    && state.Radius <= pocket.CaptureRadius
                    && lateral + state.Radius <= pocket.MouthWidth * .5f)
                {
                    return;
                }
            }

            Choose(ref hit, time, EventKind.Rail, ball, side, normal);
        }

        /// <summary>
        /// 只替换更早事件，同刻按枚举值保持固定顺序。
        /// </summary>
        /// <param name="hit">当前最早事件。</param>
        /// <param name="time">候选事件的轨迹相对秒数。</param>
        /// <param name="kind">候选类型，同刻按枚举顺序决定优先级。</param>
        /// <param name="ball">候选事件涉及的球索引。</param>
        /// <param name="other">另一球、图形、道具、球洞或库边的索引。</param>
        /// <param name="normal">库边事件的内法线，其他事件传零向量。</param>
        private static void Choose(
            ref Contact hit,
            double time,
            EventKind kind,
            int ball,
            int other,
            Vector2 normal)
        {
            if (double.IsInfinity(time) || double.IsNaN(time))
            {
                return;
            }

            // 浮点容差内视为同刻事件，按枚举顺序先处理图形、道具和落袋。
            if (time < hit.Time - 1e-8 || Math.Abs(time - hit.Time) <= 1e-8 && (hit.Kind == EventKind.None || kind < hit.Kind))
            {
                hit = new Contact
                {
                    Time = time,
                    Kind = kind,
                    Ball = ball,
                    Other = other,
                    Normal = normal
                };
            }
        }

        /// <summary>
        /// 构造静态圆轨迹。
        /// </summary>
        /// <param name="center">台面局部坐标中的静态圆心。</param>
        /// <param name="radius">静态圆半径。</param>
        /// <param name="duration">轨迹有效时长，单位为秒。</param>
        /// <returns>位置及半径均不随时间变化的轨迹。</returns>
        private static BallTrajectory Stationary(Vector2 center, float radius, double duration)
        {
            return new BallTrajectory(
                new BallState
                {
                    Position = center,
                    Radius = radius
                },
                Vector2.zero,
                0,
                duration);
        }

        /// <summary>
        /// 同步推进全部球，摩擦停球后不继续增大。
        /// </summary>
        /// <param name="time">当前轨迹起点到目标时刻的秒数。</param>
        private void Advance(double time)
        {
            for (int i = 0; i < Balls.Length; i++)
            {
                var ball = Balls[i];

                if (!ball.Active)
                {
                    continue;
                }

                ball.State = _trajectories[i].At(time);
                ball.State.Radius = Mathf.Clamp(ball.State.Radius, ball.Data.Config.RadiusMin, ball.Data.Config.RadiusMax);

                if (ball.State.Velocity.magnitude <= _level.Physics.StopSpeed)
                {
                    ball.State.Velocity = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// 响应事件；图形目标成功只停止白球，不停止其他球。
        /// </summary>
        /// <param name="hit">已推进到接触时刻的事件。</param>
        private void Resolve(Contact hit)
        {
            var ball = Balls[hit.Ball];
            float epsilon = _level.Physics.ContactTolerance;

            switch (hit.Kind)
            {
                case EventKind.Goal:
                    var geometry = _level.Geometries[hit.Other];
                    _conditionLatched[hit.Other * Balls.Length + hit.Ball] = true;

                    if (geometry.UnlocksPocket)
                    {
                        // 几何条件只记录完成；球洞必须由绑定钥匙拾取后解锁。
                        GeometryCompleted[hit.Other] = true;
                    }
                    else
                    {
                        int prop = PropIndex(geometry.TargetId);
                        TriggerProp(prop, ball);
                        GeometryCompleted[hit.Other] = PropUses[prop] >= _level.Props[prop].UseLimit;
                    }

                    GeometryFailureCounts[hit.Other] = 0;

                    if (hit.Ball == WhiteIndex)
                    {
                        // 容差只用于识别成功；提交时吸附到命中的理论目标圆。
                        GoalMatch match = GoalJudge.Evaluate(geometry, ball.State);
                        ball.State.Position = match.Center;
                        ball.State.Radius = match.Radius;
                        ball.State.Velocity = Vector2.zero;
                        _whiteStoppedAtGoal = true;
                    }

                    Revision++;
                    break;
                case EventKind.Prop:
                    TriggerProp(hit.Other, ball);
                    break;
                case EventKind.Terrain:
                    TriggerTerrain(hit.Other, ball);
                    break;
                case EventKind.Pocket:
                    ball.Active = false;
                    ball.State.Velocity = Vector2.zero;

                    if (ball.Data.Config.Kind == BallKind.Black)
                    {
                        PocketStates[hit.Other] = PocketState.Occupied;
                    }
                    else
                    {
                        WhiteFoul = true;
                    }

                    Revision++;
                    break;
                case EventKind.Ball:
                    var other = Balls[hit.Other];
                    Vector2 delta = ball.State.Position - other.State.Position;
                    Vector2 normal = delta.sqrMagnitude > 1e-12 ? delta.normalized : Vector2.right;
                    float separation = Mathf.Max(epsilon, ball.State.Radius + other.State.Radius - delta.magnitude + epsilon);
                    bool ballStoppedAtGoal = _whiteStoppedAtGoal && hit.Ball == WhiteIndex;
                    bool otherStoppedAtGoal = _whiteStoppedAtGoal && hit.Other == WhiteIndex;

                    if (ballStoppedAtGoal)
                    {
                        Reflect(other, -normal, _level.Physics.BallRestitution);
                        other.State.Position -= normal * separation;
                    }
                    else if (otherStoppedAtGoal)
                    {
                        Reflect(ball, normal, _level.Physics.BallRestitution);
                        ball.State.Position += normal * separation;
                    }
                    else
                    {
                        float inward = Vector2.Dot(ball.State.Velocity - other.State.Velocity, normal);

                        if (inward < 0)
                        {
                            float impulse = -(1 + _level.Physics.BallRestitution) * inward / (1 / ball.Data.Config.Mass + 1 / other.Data.Config.Mass);
                            ball.State.Velocity += normal * (impulse / ball.Data.Config.Mass);
                            other.State.Velocity -= normal * (impulse / other.Data.Config.Mass);
                        }

                        // 分离接触面并保留最小间隙，避免下一次查询重复命中零时刻接触。
                        ball.State.Position += normal * (separation * .5f);
                        other.State.Position -= normal * (separation * .5f);
                    }

                    break;
                case EventKind.Rail:
                    Reflect(ball, hit.Normal, _level.Physics.RailRestitution);

                    float border = hit.Other == 0
                        ? _level.Bounds.xMin : hit.Other == 1
                        ? -_level.Bounds.xMax : hit.Other == 2
                        ? _level.Bounds.yMin : -_level.Bounds.yMax;
                    ball.State.Position += hit.Normal * Mathf.Max(epsilon, border + ball.State.Radius - Vector2.Dot(
                        hit.Normal,
                        ball.State.Position) + epsilon);
                    break;
                case EventKind.Gate:
                    if (_dryApproach)
                    {
                        _approachEnded = true;
                        break;
                    }

                    var gate = _level.Geometries[hit.Other];

                    if (ball.Data.Config.Kind == gate.AllowedBallKind && CanCompleteApproach(hit.Other, hit.Ball))
                    {
                        _admitted[hit.Other * Balls.Length + hit.Ball] = true;
                    }
                    else
                    {
                        Vector2 failurePosition = ball.State.Position;
                        float failureRadius = ball.State.Radius;
                        Vector2 n = hit.Normal.sqrMagnitude > .000001f ? hit.Normal : Vector2.up;
                        Reflect(ball, n, _level.Physics.GeometryRestitution);
                        Vector2 boundaryNormal;
                        float boundary = gate.MinimumBoundaryDistance(ball.State.Position, out boundaryNormal);
                        ball.State.Position += n * Mathf.Max(epsilon, ball.State.Radius - boundary + epsilon);

                        if (!_dryApproach && hit.Ball == WhiteIndex)
                        {
                            GoalMatch target = GoalJudge.NearestTarget(gate, failureRadius);
                            int failureCount = ++GeometryFailureCounts[hit.Other];
                            bool shouldPreview = ShouldPreview(failureCount);
                            _pendingGoalFeedback = new GoalFeedback(
                                gate.Id,
                                failurePosition,
                                failureRadius,
                                target.Radius,
                                target.Kind,
                                target.Center,
                                failureCount,
                                shouldPreview,
                                Revision + 1);
                            _hasPendingGoalFeedback = true;
                            Revision++;
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// 推进地形局部状态并解除已结束的入口锁定。
        /// </summary>
        private void UpdateTerrainTimers(float duration)
        {
            for (int i = 0; i < TerrainStates.Length; i++)
            {
                TerrainRuntimeState state = TerrainStates[i];
                state.CooldownRemaining = Mathf.Max(0, state.CooldownRemaining - duration);

                if (state.CooldownRemaining <= 0)
                {
                    state.EntryLocked = false;
                }
            }

            for (int i = 0; i < Balls.Length; i++)
            {
                TerrainBallState state = Balls[i].Terrain;
                state.TeleportCooldown = Mathf.Max(0, state.TeleportCooldown - duration);

                if ((state.Submerged || state.TunnelBlocked)
                    && Balls[i].State.Velocity.sqrMagnitude > _level.Physics.StopSpeed * _level.Physics.StopSpeed)
                {
                    ClearTerrainBallState(state);

                    continue;
                }

                if (state.Remaining > 0)
                {
                    state.Remaining = Mathf.Max(0, state.Remaining - duration);
                    if (state.Remaining <= 0)
                    {
                        ClearTerrainBallState(state);
                    }
                }
                else if (state.TeleportCooldown <= 0 && state.TerrainId != 0)
                {
                    ClearTerrainBallState(state);
                }
            }
        }

        private void ClearTerrainBallState(TerrainBallState state)
        {
            int terrainId = state.TerrainId;
            state.Clear();

            if (terrainId <= 0)
            {
                return;
            }

            int terrainIndex = TerrainIndex(terrainId);

            for (int i = 0; i < Balls.Length; i++)
            {
                if (Balls[i].Terrain.TerrainId == terrainId)
                {
                    return;
                }
            }

            TerrainStates[terrainIndex].Reset();
        }

        /// <summary>
        /// 统一地形策略入口；只改变运动和局部状态，不参与胜负结算。
        /// </summary>
        private void TriggerTerrain(int index, SimulatedBall ball)
        {
            TerrainPlacementData terrain = _level.Terrains[index];
            TerrainRuntimeState runtime = TerrainStates[index];
            runtime.Active = true;
            runtime.EntryLocked = true;
            runtime.CooldownRemaining = .05f;
            ball.Terrain.TerrainId = terrain.Id;

            switch (terrain.Kind)
            {
                case TerrainKind.JumpPad:
                    ball.Terrain.Airborne = true;
                    ball.Terrain.Remaining = terrain.Duration;
                    Vector2 jumpDirection = ball.State.Velocity.sqrMagnitude > .000001f
                        ? ball.State.Velocity.normalized
                        : terrain.Direction;
                    ball.State.Velocity = jumpDirection * (ball.State.Velocity.magnitude * terrain.SpeedMultiplier);
                    runtime.Airborne = true;
                    break;
                case TerrainKind.Tunnel:
                    ResolveTunnel(terrain, runtime, ball);
                    break;
                case TerrainKind.Pit:
                    ball.Terrain.Submerged = true;
                    ball.Terrain.Remaining = float.PositiveInfinity;
                    ball.State.Velocity = Vector2.zero;
                    runtime.Submerged = true;
                    break;
                case TerrainKind.ReverseBelt:
                    ball.Terrain.ReverseActive = true;
                    ball.Terrain.Remaining = terrain.Duration;
                    ball.State.Velocity = -ball.State.Velocity;
                    break;
                case TerrainKind.Teleporter:
                    runtime.Active = false;
                    Teleport(ball, terrain);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Revision++;
        }

        /// <summary>
        /// 将球转移到成对出口并设置冷却，防止同一固定步来回触发。
        /// </summary>
        private void Teleport(SimulatedBall ball, TerrainPlacementData entry)
        {
            int exitIndex = TerrainIndex(entry.ExitId);
            TerrainPlacementData exit = _level.Terrains[exitIndex];
            TerrainRuntimeState exitState = TerrainStates[exitIndex];
            ball.State.Position = exit.Position;
            exitState.Active = true;

            float incomingSpeed = ball.State.Velocity.magnitude;
            float outgoingSpeed = entry.InheritVelocity
                ? incomingSpeed
                : incomingSpeed * entry.SpeedMultiplier;
            Vector2 outgoingDirection = entry.InheritVelocity
                && entry.InheritRotation
                && ball.State.Velocity.sqrMagnitude > .000001f
                ? ball.State.Velocity.normalized
                : exit.Direction;
            ball.State.Velocity = outgoingDirection * outgoingSpeed;

            ball.Terrain.TeleportCooldown = .08f;
            ball.Terrain.TerrainId = exit.Id;
            exitState.EntryLocked = true;
            exitState.CooldownRemaining = .08f;
        }

        private void ResolveTunnel(TerrainPlacementData terrain, TerrainRuntimeState runtime, SimulatedBall ball)
        {
            float difference = ball.State.Radius - terrain.OpeningRadius;

            if (difference > _level.Physics.ContactTolerance)
            {
                ball.State.Velocity = Vector2.Reflect(ball.State.Velocity, terrain.Direction);
                ball.Terrain.Clear();
                runtime.Active = false;

                return;
            }

            ball.Terrain.TunnelActive = true;
            runtime.TunnelActive = true;

            if (Mathf.Abs(difference) <= _level.Physics.ContactTolerance)
            {
                ball.Terrain.TunnelBlocked = true;
                ball.Terrain.Remaining = float.PositiveInfinity;
                ball.State.Velocity = Vector2.zero;

                return;
            }

            ball.Terrain.Remaining = terrain.Duration;
            ball.State.Velocity = terrain.Direction * (ball.State.Velocity.magnitude * terrain.SpeedMultiplier);
        }

        private bool TryReleasePit(SimulatedBall ball, float power, BallConfigData config)
        {
            if (!ball.Terrain.Submerged)
            {
                return true;
            }

            TerrainPlacementData terrain = _level.Terrains[TerrainIndex(ball.Terrain.TerrainId)];

            if (terrain.Kind != TerrainKind.Pit)
            {
                return true;
            }

            float speed = Mathf.Lerp(config.SpeedMin, config.SpeedMax, power);

            if (speed < config.SpeedMax - _level.Physics.ContactTolerance)
            {
                return false;
            }

            ClearTerrainBallState(ball.Terrain);

            return true;
        }

        private int TerrainIndex(int id)
        {
            for (int i = 0; i < _level.Terrains.Count; i++)
            {
                if (_level.Terrains[i].Id == id)
                {
                    return i;
                }
            }

            throw new ArgumentException("不存在地形 " + id);
        }

        /// <summary>
        /// 判断本次失败是否命中配置的预亮梯度节点。
        /// </summary>
        private bool ShouldPreview(int failureCount)
        {
            int threshold = _level.GeometryStyle.PreviewFailureThreshold;
            int interval = _level.GeometryStyle.PreviewFailureInterval;

            return failureCount >= threshold && (failureCount - threshold) % interval == 0;
        }

        /// <summary>
        /// 几何门只放行本次前进确实能完成条件的球。
        /// </summary>
        /// <param name="geometry">当前接触的几何门索引。</param>
        /// <param name="ball">尝试通过几何门的球索引。</param>
        /// <returns>预算内是否能在本次接近过程中完成当前几何条件。</returns>
        private bool CanCompleteApproach(int geometry, int ball)
        {
            var query = Copy();
            query._ignoredGeometry = geometry;
            query._dryApproach = true;

            var gate = _level.Geometries[geometry];

            for (int step = 0; step < _level.Physics.PredictionBudget && query.Moving && !query._approachEnded; step++)
            {
                query.Step(_level.Physics.Step);

                if (query.GeometryCompleted[geometry] || query._conditionLatched[geometry * Balls.Length + ball])
                {
                    return true;
                }

                var candidate = query.Balls[ball];

                Vector2 boundaryNormal;
                float boundary = gate.MinimumBoundaryDistance(candidate.State.Position, out boundaryNormal);

                if (!candidate.Active
                    || boundary < -.002f
                    && Vector2.Dot(boundaryNormal, candidate.State.Velocity) < 0)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// 反射入射速度。
        /// </summary>
        /// <param name="ball">需要修改速度的模拟球。</param>
        /// <param name="normal">障碍指向允许活动区域的单位法线。</param>
        /// <param name="restitution">对应障碍的恢复系数，范围为零到一。</param>
        private static void Reflect(SimulatedBall ball, Vector2 normal, float restitution)
        {
            float inward = Vector2.Dot(ball.State.Velocity, normal);

            if (inward < 0)
            {
                ball.State.Velocity -= normal * ((1 + restitution) * inward);
            }
        }

        /// <summary>
        /// 条件放行和出杆初始重叠放行均在球完全离开目标圆后解除。
        /// </summary>
        private void ClearAdmissions()
        {
            for (int g = 0; g < _level.Geometries.Count; g++)
            {
                for (int b = 0; b < Balls.Length; b++)
                {
                    if (_conditionLatched[g * Balls.Length + b] && GoalJudge.Check(_level.Geometries[g], Balls[b].State) == GoalKind.None)
                    {
                        _conditionLatched[g * Balls.Length + b] = false;
                    }

                    Vector2 boundaryNormal;
                    float boundary = _level.Geometries[g].MinimumBoundaryDistance(Balls[b].State.Position, out boundaryNormal);

                    if (_admitted[g * Balls.Length + b]
                        && boundary > Balls[b].State.Radius + .002f)
                    {
                        _admitted[g * Balls.Length + b] = false;
                    }
                }
            }
        }

        /// <summary>
        /// 图形效果是否仍可触发。
        /// </summary>
        /// <param name="index">关卡图形数组索引。</param>
        /// <returns>效果尚未完成且绑定目标仍可触发时为 true。</returns>
        private bool GeometryAvailable(int index)
        {
            if (GeometryCompleted[index])
            {
                return false;
            }

            var geometry = _level.Geometries[index];

            return geometry.UnlocksPocket || PropUses[PropIndex(geometry.TargetId)] < _level.Props[PropIndex(geometry.TargetId)].UseLimit;
        }

        /// <summary>
        /// 执行翻转策略并记录作用域次数。
        /// </summary>
        /// <param name="index">关卡道具数组索引。</param>
        /// <param name="ball">接受策略效果的模拟球。</param>
        private void TriggerProp(int index, SimulatedBall ball)
        {
            var prop = _level.Props[index];

            if (prop.Kind == PropEffectKind.Key)
            {
                int pocket = PocketIndex(prop.TargetPocketId);

                if (PocketStates[pocket] != PocketState.Locked)
                {
                    throw new InvalidOperationException($"钥匙道具 {prop.Id} 触发时绑定洞 {prop.TargetPocketId} 不是上锁状态。");
                }

                PocketStates[pocket] = PocketState.Unlocked;
            }
            else
            {
                _propEffects[index].Apply(ball, prop.RateMultiplier);
                EffectRemaining[index] = IsPersistentEffect(prop)
                    ? -1
                    : prop.Kind == PropEffectKind.Preview && prop.UseScope == UseScope.Shot
                    ? Mathf.Max(1, prop.DurationShots) + 1
                    : Mathf.Max(0, prop.DurationShots);

                if (prop.Kind == PropEffectKind.Reveal)
                {
                    RevealActive = EffectRemaining[index] != 0;
                }
                else if (prop.Kind == PropEffectKind.Preview)
                {
                    PreviewActive = EffectRemaining[index] != 0;
                }
                else if (prop.Kind == PropEffectKind.AddShot)
                {
                    ShotBonus += Mathf.Max(1, Mathf.RoundToInt(prop.RateMultiplier));
                }
            }

            PropUses[index]++;
            Revision++;
        }

        private static bool IsPersistentEffect(PropPlacementData prop)
        {
            return prop.UseScope == UseScope.Level
                && (prop.Kind == PropEffectKind.Flip
                    || prop.Kind == PropEffectKind.Reverse
                    || prop.Kind == PropEffectKind.Reveal);
        }

        /// <summary>
        /// 查询球洞 ID 对应索引。
        /// </summary>
        /// <param name="id">本关球洞明细 ID。</param>
        /// <returns>本关球洞配置数组索引；不存在时抛出参数异常。</returns>
        private int PocketIndex(int id)
        {
            for (int i = 0; i < _level.Pockets.Count; i++)
            {
                if (_level.Pockets[i].Id == id)
                {
                    return i;
                }
            }

            throw new ArgumentException("不存在球洞 " + id);
        }

        /// <summary>
        /// 查询道具 ID 对应索引。
        /// </summary>
        /// <param name="id">本关道具明细 ID。</param>
        /// <returns>本关道具配置数组索引；不存在时抛出参数异常。</returns>
        private int PropIndex(int id)
        {
            for (int i = 0; i < _level.Props.Count; i++)
            {
                if (_level.Props[i].Id == id)
                {
                    return i;
                }
            }

            throw new ArgumentException("不存在道具 " + id);
        }

        /// <summary>
        /// 白球出杆尺寸变化或复位前检查台面及其他活动球，目标圆不参与摆放限制。
        /// </summary>
        /// <param name="position">待放置的台面局部圆心坐标。</param>
        /// <param name="radius">待放置的球半径。</param>
        /// <param name="except">重叠检查需要排除的当前球索引。</param>
        /// <returns>候选球是否完整位于台面且不与其他活动球重叠。</returns>
        private bool CanPlace(Vector2 position, float radius, int except)
        {
            if (!_level.ContainsCircle(position, radius))
            {
                return false;
            }

            for (int i = 0; i < Balls.Length; i++)
            {
                if (i != except
                    && Balls[i].Active
                    && Vector2.Distance(position, Balls[i].State.Position) <= radius + Balls[i].State.Radius)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 犯规后在出生点或最近合法网格点放回白球。
        /// </summary>
        public void RespawnWhite()
        {
            var ball = Balls[WhiteIndex];
            float radius = ball.Data.Radius;
            Vector2 spawn = ball.Data.Position;
            Vector2 best = spawn;
            float bestDistance = float.PositiveInfinity;

            if (CanPlace(spawn, radius, WhiteIndex))
            {
                bestDistance = 0;
            }
            else
            {
                for (float y = _level.Bounds.yMin + radius; y <= _level.Bounds.yMax - radius; y += radius)
                {
                    for (float x = _level.Bounds.xMin + radius; x <= _level.Bounds.xMax - radius; x += radius)
                    {
                        var p = new Vector2(x, y);
                        float distance = (p - spawn).sqrMagnitude;

                        if (distance < bestDistance && CanPlace(p, radius, WhiteIndex))
                        {
                            best = p;
                            bestDistance = distance;
                        }
                    }
                }
            }

            if (float.IsInfinity(bestDistance))
            {
                throw new InvalidOperationException("本关没有可放回白球的合法位置。");
            }

            ball.Active = true;
            _whiteStoppedAtGoal = false;
            ball.State = new BallState
            {
                Position = best,
                Radius = radius
            };
            ball.BaseRadius = radius;
            ball.RadiusDirection = 1;
            ball.RateMultiplier = 1;
            Revision++;
        }

        /// <summary>
        /// 预测白球轨迹，有限预算不足时报告截断而不伪装成停点；预测忽略几何门反弹，但图形成功仍停在目标圆。
        /// </summary>
        /// <param name="direction">非零的瞄准方向，内部归一化后应用。</param>
        /// <param name="power">零到一的归一化力度。</param>
        /// <param name="path">复用的预测路径结果，写入前清空。</param>
        /// <returns>白球落袋或所有球停止时为 true；出杆被拒绝或预算耗尽时为 false。</returns>
        public bool Predict(Vector2 direction, float power, PredictionPath path)
        {
            var query = Copy();
            // 预测不因条件未达成而在几何门反弹；若条件达成，仍与真实世界一致地停在目标圆。
            query._ignoreGates = true;
            var white = query.Balls[WhiteIndex];
            path.Reset(white.State.Position);

            if (!query.Shoot(direction, power))
            {
                return false;
            }

            for (int step = 0; step < _level.Physics.PredictionBudget && query.Moving; step++)
            {
                query.Step(_level.Physics.Step, WhiteIndex, path);
                path.SetEndPosition(white.State.Position);

                if (!white.Active)
                {
                    return true;
                }
            }

            return !query.Moving;
        }
    }
}
