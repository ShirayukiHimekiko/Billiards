namespace GameLogic
{
    /// <summary>
    /// 纯数据球状态，可用于真实模拟及隔离的预测副本。
    /// </summary>
    public sealed class SimulatedBall
    {
        /// <summary>
        /// 真实世界与预测副本共用的出生配置。
        /// </summary>
        public readonly BallPlacementData Data;

        /// <summary>
        /// 当前球心位置、速度及半径。
        /// </summary>
        public BallState State;

        /// <summary>
        /// 球是否仍参与台面模拟；落袋后关闭。
        /// </summary>
        public bool Active = true;

        /// <summary>
        /// 当前尺寸变化方向，正数为增大，负数为减小。
        /// </summary>
        public int RadiusDirection = 1;

        /// <summary>
        /// 本杆开始时由力量确定的初始半径。
        /// </summary>
        public float BaseRadius;

        /// <summary>
        /// 本杆由击球力度确定的基础半径变化速率，单位为每秒半径增量。
        /// </summary>
        public float GrowRate;

        /// <summary>
        /// 变化速率倍率，供冻结、疾变等效果使用。
        /// </summary>
        public float RateMultiplier = 1;

        /// <summary>
        /// 当前尺寸变化是否处于增大趋势，保留为旧 UI 和翻转道具的兼容入口。
        /// </summary>
        public bool Growing
        {
            get
            {
                return RadiusDirection > 0;
            }
            set
            {
                RadiusDirection = value ? 1 : -1;
            }
        }

        /// <summary>
        /// 创建球体状态。
        /// </summary>
        /// <param name="data">球体出生位置、半径及共享类型参数。</param>
        public SimulatedBall(BallPlacementData data)
        {
            Data = data;
            GrowRate = data.Config.GrowRateMin;
            BaseRadius = data.Radius;
            State = new BallState
            {
                Position = data.Position,
                Radius = data.Radius
            };
        }

        /// <summary>
        /// 复制全部可变状态。
        /// </summary>
        /// <returns>共享出生配置但具有独立可变状态的新球对象。</returns>
        public SimulatedBall Copy()
        {
            return new SimulatedBall(Data)
            {
                State = State,
                Active = Active,
                RadiusDirection = RadiusDirection,
                BaseRadius = BaseRadius,
                GrowRate = GrowRate,
                RateMultiplier = RateMultiplier
            };
        }
    }
}
