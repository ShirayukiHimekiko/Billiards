using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 管理击球次数、暂停、进度保留及全部黑球入洞的结算。
    /// </summary>
    public sealed class Session
    {
        /// <summary>
        /// 当前会话共用的关卡配置快照。
        /// </summary>
        private readonly LevelData _level;

        /// <summary>
        /// 负责球体加载、同步与回收的管理器。
        /// </summary>
        private readonly BallManager _balls;

        /// <summary>
        /// 负责道具加载、效果策略与表现同步的管理器。
        /// </summary>
        private readonly PropManager _props;

        /// <summary>
        /// 负责六洞状态表现的管理器。
        /// </summary>
        private readonly PocketManager _pockets;

        /// <summary>
        /// 负责图形进度表现与提示显示的管理器。
        /// </summary>
        private readonly GeometryManager _geometries;

        /// <summary>
        /// 负责地形实例同步与表现状态更新的管理器。
        /// </summary>
        private readonly TerrainManager _terrains;

        /// <summary>
        /// 当前关卡的台面表现与输入投影入口。
        /// </summary>
        private readonly BoardView _board;

        /// <summary>
        /// 会话绑定的唯一白球输入入口。
        /// </summary>
        private readonly WhiteBall _white;

        /// <summary>
        /// 进入暂停前的会话状态，用于继续时恢复。
        /// </summary>
        private SessionState _beforePause;

        /// <summary>
        /// 尚未推进为固定模拟步的累计秒数。
        /// </summary>
        private float _accumulator;

        /// <summary>
        /// 最近一次发送界面快照时的世界状态版本。
        /// </summary>
        private int _revision;

        /// <summary>
        /// 会话是否已释放，用于阻止后续输入与帧推进。
        /// </summary>
        private bool _released;

        /// <summary>
        /// 上一次同步到台面的预演状态，避免每次同步都使预测缓存失效。
        /// </summary>
        private bool _lastPreviewActive;

        /// <summary>
        /// 本次发布周期内已拾取的钥匙绑定洞，用于生成一次性反馈文案。
        /// </summary>
        private readonly List<int> _keyFeedbacks = new List<int>();

        /// <summary>
        /// 真实模拟世界。
        /// </summary>
        public PhysicsWorld World
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前会话状态。
        /// </summary>
        public SessionState State
        {
            get;
            private set;
        }

        /// <summary>
        /// 剩余击球次数。
        /// </summary>
        public int ShotsLeft
        {
            get;
            private set;
        }

        /// <summary>
        /// 尚可使用的折射预测道具次数。
        /// </summary>
        public int PredictionPropRemaining
        {
            get;
            private set;
        }

        /// <summary>
        /// 折射预测道具是否已为下一杆启用。
        /// </summary>
        public bool PredictionPropArmed
        {
            get;
            private set;
        }

        /// <summary>
        /// 状态消息。
        /// </summary>
        public string Message
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前道具使用记录是否非零；单杆及整关作用域的记录均参与判断。
        /// </summary>
        public bool ItemUsed
        {
            get
            {
                foreach (int uses in World.PropUses)
                {
                    if (uses > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 界面快照包含所有 HUD 所需的只读会话数据。
        /// </summary>
        public SessionUISnapshot Snapshot
        {
            get
            {
                SimulatedBall white = World.Balls[World.WhiteIndex];
                WhiteBallUISnapshot whiteSnapshot = BuildWhiteBallSnapshot(white);
                IReadOnlyList<EffectUISnapshot> effects = BuildEffectSnapshots();
                IReadOnlyList<PocketUISnapshot> pockets = BuildPocketSnapshots();
                PreviewUISnapshot preview = new PreviewUISnapshot(
                    PredictionPropRemaining,
                    PredictionPropArmed,
                    World.PreviewActive);

                return new SessionUISnapshot(
                    State,
                    ShotsLeft,
                    white.Growing,
                    ItemUsed,
                    Message,
                    World.RemainingBlack,
                    PredictionPropRemaining,
                    PredictionPropArmed,
                    _props.GetKeyStates(World),
                    whiteSnapshot,
                    effects,
                    pockets,
                    preview);
            }
        }

        private WhiteBallUISnapshot BuildWhiteBallSnapshot(SimulatedBall white)
        {
            float progress = Mathf.InverseLerp(white.Data.Config.RadiusMin, white.Data.Config.RadiusMax, white.State.Radius);
            int band = Mathf.Clamp(Mathf.FloorToInt(progress * 24f) + 1, 1, 24);
            SessionUIRadiusTrend trend;

            if (Mathf.Approximately(white.RateMultiplier, 0))
            {
                trend = SessionUIRadiusTrend.Frozen;
            }
            else if (white.RateMultiplier > 1)
            {
                trend = white.Growing ? SessionUIRadiusTrend.FastGrowing : SessionUIRadiusTrend.FastShrinking;
            }
            else
            {
                trend = white.Growing ? SessionUIRadiusTrend.Growing : SessionUIRadiusTrend.Shrinking;
            }

            return new WhiteBallUISnapshot(band, progress, trend);
        }

        private IReadOnlyList<EffectUISnapshot> BuildEffectSnapshots()
        {
            var effects = new List<EffectUISnapshot>(_level.Props.Count);

            for (int i = 0; i < _level.Props.Count; i++)
            {
                PropPlacementData prop = _level.Props[i];
                effects.Add(new EffectUISnapshot(
                    prop.Id,
                    prop.Kind,
                    World.PropUses[i],
                    prop.UseLimit,
                    prop.DurationShots,
                    World.EffectRemaining[i],
                    prop.RateMultiplier,
                    prop.TargetPocketId));
            }

            return effects.AsReadOnly();
        }

        private IReadOnlyList<PocketUISnapshot> BuildPocketSnapshots()
        {
            var pockets = new List<PocketUISnapshot>(_level.Pockets.Count);

            for (int i = 0; i < _level.Pockets.Count; i++)
            {
                PocketPlacementData pocket = _level.Pockets[i];
                pockets.Add(new PocketUISnapshot(pocket.Id, pocket.Slot, World.PocketStates[i]));
            }

            return pockets.AsReadOnly();
        }

        /// <summary>
        /// 创建会话并绑定管理器及输入。
        /// </summary>
        /// <param name="level">本关配置快照。</param>
        /// <param name="balls">已初始化的球管理器。</param>
        /// <param name="props">已初始化的道具管理器及效果策略。</param>
        /// <param name="pockets">本关球洞表现管理器。</param>
        /// <param name="geometries">本关图形表现管理器。</param>
        /// <param name="terrains">本关地形表现管理器。</param>
        /// <param name="board">会话绑定的台面及输入入口。</param>
        public Session(
            LevelData level,
            BallManager balls,
            PropManager props,
            PocketManager pockets,
            GeometryManager geometries,
            TerrainManager terrains,
            BoardView board)
        {
            _level = level;
            _balls = balls;
            _props = props;
            _pockets = pockets;
            _geometries = geometries;
            _terrains = terrains;
            _board = board;
            _white = balls.WhiteBall;
            _white.Shoot.SetSession(this, board);
            board.Bind(this, geometries);
            Restart();
        }

        /// <summary>
        /// 接受击球后消耗一次机会，无法原地容纳球半径时提示重新选择力度。
        /// </summary>
        /// <param name="direction">非零的瞄准方向。</param>
        /// <param name="power">击球力度，内部限制到零至一。</param>
        /// <returns>是否接受出杆并消耗一次机会；状态或摆放不允许时为 false。</returns>
        public bool Shoot(Vector2 direction, float power)
        {
            if (_released || State != SessionState.Ready || ShotsLeft <= 0 || direction.sqrMagnitude < .0001f)
            {
                return false;
            }

            if (!World.Shoot(direction, Mathf.Clamp01(power)))
            {
                Message = "白球当前尺寸放不下，无法出杆";
                Publish();

                return false;
            }

            // 只有世界接受本次出杆后才扣除机会，尺寸放不下的尝试不计次数。
            ShotsLeft--;

            ApplyShotBonus();

            if (PredictionPropArmed)
            {
                PredictionPropRemaining--;
                SetPredictionPropArmed(false);
            }

            _accumulator = 0;
            State = SessionState.Rolling;
            Message = "白球推动黑球运动中…";
            Sync();
            Publish();

            return true;
        }

        /// <summary>
        /// 按统一世界步长推进。
        /// </summary>
        /// <param name="delta">非缩放帧时间，单位为秒；单帧累计上限为 0.1 秒。</param>
        public void Tick(float delta)
        {
            if (_released || State != SessionState.Rolling)
            {
                return;
            }

            _accumulator += Mathf.Min(delta, .1f);

            while (_accumulator >= _level.Physics.Step && State == SessionState.Rolling)
            {
                World.Step(_level.Physics.Step);
                _accumulator -= _level.Physics.Step;

                ApplyShotBonus();

                if (!World.Moving)
                {
                    FinishShot();
                }
            }

            Sync();

            if (_revision != World.Revision)
            {
                _revision = World.Revision;
                Publish();
            }
        }

        /// <summary>
        /// 停球后结算，所有已有进度保留；同杆最后黑球与白球落袋时先完成犯规复位再结算。
        /// </summary>
        private void FinishShot()
        {
            // 犯规先复位白球，再依据黑球进度结算，不撤销同杆已经完成的进球。
            bool foul = World.WhiteFoul;

            if (foul)
            {
                World.RespawnWhite();
            }

            if (World.RemainingBlack == 0)
            {
                State = SessionState.Won;
                Message = foul ? "全部黑球已入洞，白球犯规并已复位" : "全部黑球已入洞，通关！";
            }
            else if (ShotsLeft == 0)
            {
                State = SessionState.Lost;
                Message = $"机会用完，还剩 {World.RemainingBlack} 个黑球";
            }
            else
            {
                State = SessionState.Ready;
                Message = foul ? "白球落袋犯规，已放回合法空位；进度保留" : "本杆结束，球位置和已解锁进度保留";
            }

            _white.Shoot.CancelCharge();
            Publish();
        }

        /// <summary>
        /// 恢复整关配置状态。
        /// </summary>
        public void Restart()
        {
            if (_released)
            {
                return;
            }

            _white.Shoot.CancelCharge();
            World = new PhysicsWorld(_level, _props.Effects);
            ShotsLeft = _level.MaxShots;
            PredictionPropRemaining = _level.PredictionPropCount;
            SetPredictionPropArmed(false);
            _lastPreviewActive = false;
            _accumulator = 0;
            State = SessionState.Ready;
            Message = $"{_level.Name}：{_level.Objective}";
            Sync();
            Publish();
        }

        /// <summary>
        /// 为下一次成功出杆切换折射预测，取消启用时不消耗次数。
        /// </summary>
        /// <returns>是否成功切换道具状态。</returns>
        public bool TryTogglePredictionProp()
        {
            if (_released || State != SessionState.Ready || PredictionPropRemaining <= 0)
            {
                return false;
            }

            SetPredictionPropArmed(!PredictionPropArmed);
            Message = PredictionPropArmed ? "折射预测已启用，将在下一杆消耗" : "已取消折射预测，本次不消耗";
            Publish();

            return true;
        }

        /// <summary>
        /// 同步单次折射预测的启用状态及台面显示范围。
        /// </summary>
        /// <param name="armed">是否显示第一次撞库后的单次反射并等待下一杆消耗。</param>
        private void SetPredictionPropArmed(bool armed)
        {
            PredictionPropArmed = armed;
            _board.SetPredictionReflectionsVisible(armed);
        }

        /// <summary>
        /// 切换暂停，同时取消蓄力。
        /// </summary>
        public void TogglePause()
        {
            if (_released || State == SessionState.Won || State == SessionState.Lost)
            {
                return;
            }

            _white.Shoot.CancelCharge();
            _accumulator = 0;

            if (State == SessionState.Paused)
            {
                State = _beforePause;
                Message = "已继续";
            }
            else
            {
                _beforePause = State;
                State = SessionState.Paused;
                Message = "已暂停";
            }

            Publish();
        }

        /// <summary>
        /// 失去焦点时暂停。
        /// </summary>
        public void LoseFocus()
        {
            if (State == SessionState.Ready || State == SessionState.Rolling)
            {
                TogglePause();
            }
        }

        /// <summary>
        /// 同步世界状态到独立表现管理器。
        /// </summary>
        private void Sync()
        {
            _balls.Sync(World);
            _props.Sync(World);
            _pockets.Sync(World);
            _geometries.Sync(World);
            _terrains.Sync(World);
            _board.SyncKeyLinks(World);

            _board.SetReveal(World.RevealActive);

            if (_lastPreviewActive != World.PreviewActive)
            {
                _lastPreviewActive = World.PreviewActive;
                _board.SetPredictionReflectionsVisible(PredictionPropArmed || World.PreviewActive);
            }
        }

        /// <summary>
        /// 将物理世界产生的加杆事件统一结算到会话杆数。
        /// </summary>
        private void ApplyShotBonus()
        {
            int bonus = World.ConsumeShotBonus();

            if (bonus <= 0)
            {
                return;
            }

            ShotsLeft += bonus;
            Message = $"获得额外击球次数 +{bonus}";
        }

        /// <summary>
        /// 发送界面事件。
        /// </summary>
        private void Publish()
        {
            _keyFeedbacks.Clear();

            if (_props.ConsumeKeyPickupFeedbacks(_keyFeedbacks))
            {
                if (_keyFeedbacks.Count == 1)
                {
                    Message = $"钥匙已拾取，已解锁 { _keyFeedbacks[0] } 号洞";
                }
                else
                {
                    Message = "钥匙已拾取，绑定球洞已解锁";
                }
            }

            GameEvent.Send(SessionEvents.StateChanged, Snapshot);
        }

        /// <summary>
        /// 解除输入与会话绑定，允许 Unity 对象已先被销毁。
        /// </summary>
        public void Release()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            _accumulator = 0;

            if (_white != null && _white.Shoot != null)
            {
                _white.Shoot.SetSession(null, null);
            }

            if (_board != null)
            {
                _board.Bind(null, null);
            }
        }
    }
}
