using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 球体在台面局部坐标中的位置、速度和半径。
    /// </summary>
    public struct BallState
    {
        /// <summary>
        /// 台面局部坐标中的球心位置。
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// 台面局部坐标中的速度。
        /// </summary>
        public Vector2 Velocity;

        /// <summary>
        /// 当前球半径。
        /// </summary>
        public float Radius;
    }
}
