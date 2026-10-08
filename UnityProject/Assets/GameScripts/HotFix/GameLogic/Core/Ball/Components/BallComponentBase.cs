using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 球体功能组件共用的初始化和释放入口。
    /// </summary>
    public abstract class BallComponentBase : MonoBehaviour
    {
        /// <summary>
        /// 当前功能组件所属的球体表现。
        /// </summary>
        protected BallBase Ball;

        /// <summary>
        /// 当前球体共享的类型参数。
        /// </summary>
        protected BallConfigData Config;

        /// <summary>
        /// 绑定球体及模拟参数。
        /// </summary>
        /// <param name="ball">组件所属球体。</param>
        /// <param name="config">球体参数。</param>
        public virtual void Initialize(BallBase ball, BallConfigData config)
        {
            Ball = ball;
            Config = config;
        }

        /// <summary>
        /// 重置组件运行状态。
        /// </summary>
        public abstract void ResetFeature();

        /// <summary>
        /// 重置组件并解除球体和配置引用。
        /// </summary>
        public virtual void Release()
        {
            ResetFeature();
            Ball = null;
            Config = null;
        }
    }
}
