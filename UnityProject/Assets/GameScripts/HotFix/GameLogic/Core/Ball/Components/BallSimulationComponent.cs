using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 白球表现的尺寸趋势镜像；运动和半径变化由共享世界模拟推进。
    /// </summary>
    public sealed class BallSimulationComponent : BallComponentBase
    {
        /// <summary>
        /// 当前增大趋势。
        /// </summary>
        public bool Growing
        {
            get;
            private set;
        } = true;

        /// <summary>
        /// 当前半径是否被冻结。
        /// </summary>
        public bool Frozen
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前半径是否处于疾变状态。
        /// </summary>
        public bool FastChanging
        {
            get;
            private set;
        }

        /// <summary>
        /// 应用模拟世界的趋势。
        /// </summary>
        /// <param name="growing">是否处于增大趋势；为 false 时显示缩小趋势。</param>
        public void SetGrowing(bool growing)
        {
            Growing = growing;
        }

        /// <summary>
        /// 同步半径趋势和持续效果表现状态。
        /// </summary>
        public void SetState(bool growing, float rateMultiplier)
        {
            Growing = growing;
            Frozen = Mathf.Approximately(rateMultiplier, 0);
            FastChanging = rateMultiplier > 1;
        }

        /// <summary>
        /// 恢复初始趋势。
        /// </summary>
        public override void ResetFeature()
        {
            Growing = true;
            Frozen = false;
            FastChanging = false;
        }
    }
}
