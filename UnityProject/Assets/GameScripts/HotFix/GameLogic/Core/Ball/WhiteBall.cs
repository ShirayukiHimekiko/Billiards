using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家控制的白球，负责击球输入和尺寸趋势表现的组件生命周期。
    /// </summary>
    [RequireComponent(typeof(BallShootComponent), typeof(BallSimulationComponent))]
    public sealed class WhiteBall : BallBase
    {
        /// <summary>
        /// 白球处于缩小趋势时的球面颜色。
        /// </summary>
        private static readonly Color _shrinkingColor = new Color(0.52f, 0.82f, 1);

        /// <summary>
        /// 初始化时缓存的白球输入组件。
        /// </summary>
        private BallShootComponent _shoot;

        /// <summary>
        /// 初始化时缓存的白球尺寸趋势表现组件。
        /// </summary>
        private BallSimulationComponent _simulation;

        /// <summary>
        /// 白球独有的瞄准、蓄力和击球输入组件。
        /// </summary>
        public BallShootComponent Shoot => _shoot;

        /// <summary>
        /// 白球尺寸趋势的表现镜像，实际半径由世界模拟计算。
        /// </summary>
        public BallSimulationComponent Simulation => _simulation;

        /// <summary>
        /// 根据白球的增长或缩小趋势决定球面颜色。
        /// </summary>
        protected override Color SurfaceColor => _simulation.Growing ? Color.white : _shrinkingColor;

        /// <summary>
        /// 自动获取并缓存白球独有的能力组件。
        /// </summary>
        /// <param name="config">白球的击球和尺寸变化配置。</param>
        protected override void InitializeFeatures(BallConfigData config)
        {
            _shoot = GetRequiredComponent<BallShootComponent>(gameObject);
            _simulation = GetRequiredComponent<BallSimulationComponent>(gameObject);
            _shoot.Initialize(this, config);
            _simulation.Initialize(this, config);
        }

        /// <summary>
        /// 将世界模拟中的白球尺寸趋势同步到表现组件。
        /// </summary>
        /// <param name="ball">白球的模拟快照。</param>
        protected override void SyncFeatures(SimulatedBall ball)
        {
            _simulation.SetGrowing(ball.Growing);
        }

        /// <summary>
        /// 取消输入状态并恢复白球的初始尺寸趋势。
        /// </summary>
        protected override void ResetFeatures()
        {
            _shoot.ResetFeature();
            _simulation.ResetFeature();
        }

        /// <summary>
        /// 解绑白球的会话及能力组件，兼容初始化未完成时的释放。
        /// </summary>
        protected override void ReleaseFeatures()
        {
            _shoot?.Release();
            _simulation?.Release();
        }
    }
}
