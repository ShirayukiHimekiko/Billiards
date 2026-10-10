using System.Collections.Generic;
using GameConfig;

namespace GameLogic
{
    /// <summary>
    /// 白球半径变化趋势，UI 不直接读取物理半径。
    /// </summary>
    public enum SessionUIRadiusTrend
    {
        Growing,
        Shrinking,
        Frozen,
        FastGrowing,
        FastShrinking
    }

    /// <summary>
    /// 白球尺寸表现快照。
    /// </summary>
    public readonly struct WhiteBallUISnapshot
    {
        public readonly int RadiusBand;
        public readonly float RadiusProgress;
        public readonly SessionUIRadiusTrend Trend;

        public WhiteBallUISnapshot(int radiusBand, float radiusProgress, SessionUIRadiusTrend trend)
        {
            RadiusBand = radiusBand;
            RadiusProgress = radiusProgress;
            Trend = trend;
        }
    }

    /// <summary>
    /// 道具使用和持续时效快照。
    /// </summary>
    public readonly struct EffectUISnapshot
    {
        public readonly int PropId;
        public readonly PropEffectKind Kind;
        public readonly int Uses;
        public readonly int UseLimit;
        public readonly int DurationShots;
        public readonly int RemainingShots;
        public readonly float RateMultiplier;
        public readonly int TargetPocketId;

        public bool Active
        {
            get
            {
                return RemainingShots > 0;
            }
        }

        public EffectUISnapshot(
            int propId,
            PropEffectKind kind,
            int uses,
            int useLimit,
            int durationShots,
            int remainingShots,
            float rateMultiplier,
            int targetPocketId)
        {
            PropId = propId;
            Kind = kind;
            Uses = uses;
            UseLimit = useLimit;
            DurationShots = durationShots;
            RemainingShots = remainingShots;
            RateMultiplier = rateMultiplier;
            TargetPocketId = targetPocketId;
        }
    }

    /// <summary>
    /// 固定六洞的状态快照。
    /// </summary>
    public readonly struct PocketUISnapshot
    {
        public readonly int PocketId;
        public readonly PocketSlot Slot;
        public readonly PocketState State;

        public PocketUISnapshot(int pocketId, PocketSlot slot, PocketState state)
        {
            PocketId = pocketId;
            Slot = slot;
            State = state;
        }
    }

    /// <summary>
    /// 预演信息快照，明确区分下一杆预演和普通预测线。
    /// </summary>
    public readonly struct PreviewUISnapshot
    {
        public readonly int RemainingUses;
        public readonly bool Armed;
        public readonly bool EffectActive;
        public readonly bool TrajectoryVisible;
        public readonly bool ContactPointVisible;
        public readonly bool RadiusCurveVisible;

        public PreviewUISnapshot(int remainingUses, bool armed, bool effectActive)
        {
            RemainingUses = remainingUses;
            Armed = armed;
            EffectActive = effectActive;
            TrajectoryVisible = armed || effectActive;
            ContactPointVisible = armed || effectActive;
            RadiusCurveVisible = armed || effectActive;
        }
    }

    /// <summary>
    /// 单个钥匙及其绑定球洞的界面状态。
    /// </summary>
    public readonly struct KeyUISnapshot
    {
        public readonly int PropId;
        public readonly int TargetPocketId;
        public readonly bool Collected;
        public readonly PocketState PocketState;

        public KeyUISnapshot(int propId, int targetPocketId, bool collected, PocketState pocketState)
        {
            PropId = propId;
            TargetPocketId = targetPocketId;
            Collected = collected;
            PocketState = pocketState;
        }
    }

    /// <summary>
    /// 界面刷新所需的会话状态快照。
    /// </summary>
    public readonly struct SessionUISnapshot
    {
        /// <summary>
        /// 尚未入洞的黑球数量。
        /// </summary>
        public readonly int RemainingBlack;

        /// <summary>
        /// 会话运行状态。
        /// </summary>
        public readonly SessionState State;

        /// <summary>
        /// 剩余击球次数。
        /// </summary>
        public readonly int Shots;

        /// <summary>
        /// 球体是否正在增大。
        /// </summary>
        public readonly bool Growing;

        /// <summary>
        /// 当前进度中是否已有道具使用记录。
        /// </summary>
        public readonly bool ItemUsed;

        /// <summary>
        /// 尚可使用的折射预测道具次数。
        /// </summary>
        public readonly int PredictionPropRemaining;

        /// <summary>
        /// 折射预测道具是否已为下一杆启用。
        /// </summary>
        public readonly bool PredictionPropArmed;

        /// <summary>
        /// 当前状态提示。
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// 本关钥匙总数。
        /// </summary>
        public readonly int KeyCount;

        /// <summary>
        /// 已拾取钥匙数量。
        /// </summary>
        public readonly int CollectedKeyCount;

        /// <summary>
        /// 钥匙到绑定球洞的状态映射。
        /// </summary>
        public readonly IReadOnlyList<KeyUISnapshot> Keys;

        /// <summary>
        /// 白球尺寸档位和趋势。
        /// </summary>
        public readonly WhiteBallUISnapshot WhiteBall;

        /// <summary>
        /// 所有道具的使用次数及持续状态。
        /// </summary>
        public readonly IReadOnlyList<EffectUISnapshot> Effects;

        /// <summary>
        /// 固定六洞状态列表。
        /// </summary>
        public readonly IReadOnlyList<PocketUISnapshot> Pockets;

        /// <summary>
        /// 预演状态。
        /// </summary>
        public readonly PreviewUISnapshot Preview;

        /// <summary>
        /// 创建界面状态快照。
        /// </summary>
        /// <param name="state">会话状态。</param>
        /// <param name="shots">剩余击球次数。</param>
        /// <param name="growing">球体是否正在增大。</param>
        /// <param name="itemUsed">当前进度中是否已有道具使用记录。</param>
        /// <param name="message">状态提示。</param>
        /// <param name="remainingBlack">尚未入洞的黑球数。</param>
        /// <param name="predictionPropRemaining">尚可使用的折射预测道具次数。</param>
        /// <param name="predictionPropArmed">折射预测道具是否已为下一杆启用。</param>
        /// <param name="keys">钥匙及绑定球洞状态。</param>
        /// <param name="whiteBall">白球尺寸和趋势。</param>
        /// <param name="effects">道具使用次数及持续状态。</param>
        /// <param name="pockets">固定六洞状态列表。</param>
        /// <param name="preview">预演状态。</param>
        public SessionUISnapshot(
            SessionState state,
            int shots,
            bool growing,
            bool itemUsed,
            string message,
            int remainingBlack,
            int predictionPropRemaining,
            bool predictionPropArmed,
            IReadOnlyList<KeyUISnapshot> keys,
            WhiteBallUISnapshot whiteBall,
            IReadOnlyList<EffectUISnapshot> effects,
            IReadOnlyList<PocketUISnapshot> pockets,
            PreviewUISnapshot preview)
        {
            State = state;
            Shots = shots;
            Growing = growing;
            ItemUsed = itemUsed;
            Message = message;
            RemainingBlack = remainingBlack;
            PredictionPropRemaining = predictionPropRemaining;
            PredictionPropArmed = predictionPropArmed;
            Keys = keys;
            KeyCount = keys.Count;

            int collected = 0;

            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].Collected)
                {
                    collected++;
                }
            }

            CollectedKeyCount = collected;
            WhiteBall = whiteBall;
            Effects = effects;
            Pockets = pockets;
            Preview = preview;
        }
    }
}
