using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 标准六洞的摆放、尺寸及初始状态。
    /// </summary>
    public sealed class PocketPlacementData
    {
        /// <summary>
        /// 球洞明细 ID，供图形解锁效果绑定。
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// 四角及长边中点中的标准球洞槽位。
        /// </summary>
        public readonly PocketSlot Slot;

        /// <summary>
        /// 重开关卡时恢复的球洞状态。
        /// </summary>
        public readonly PocketState InitialState;

        /// <summary>
        /// 由台面边界和槽位偏移推导的局部坐标。
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        /// 捕获球心使用的圆半径，也是允许入洞的球半径上限。
        /// </summary>
        public readonly float CaptureRadius;

        /// <summary>
        /// 库边放行判定使用的洞口宽度。
        /// </summary>
        public readonly float MouthWidth;

        /// <summary>
        /// 按四角和长边中点计算坐标。
        /// </summary>
        /// <param name="row">本关球洞槽位及初始状态。</param>
        /// <param name="layout">对应槽位的偏移、捕获半径及洞口宽度。</param>
        /// <param name="bounds">台面局部坐标中的边界。</param>
        public PocketPlacementData(LevelPocket row, PocketLayout layout, Rect bounds)
        {
            Id = row.Id;
            Slot = row.Slot;
            InitialState = row.InitialState;

            int column = (int)Slot % 3;
            Position = new Vector2(
                column == 0 ? bounds.xMin : column == 1 ? bounds.center.x : bounds.xMax,
                (int)Slot < 3 ? bounds.yMax : bounds.yMin) + layout.Offset;
            CaptureRadius = layout.CaptureRadius;
            MouthWidth = layout.MouthWidth;
        }
    }
}
