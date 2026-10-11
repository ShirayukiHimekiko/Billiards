namespace GameLogic
{
    /// <summary>
    /// 道具触发时交由模拟世界写入的结果。
    /// </summary>
    public readonly struct PropEffectResult
    {
        /// <summary>
        /// 本次触发增加的击球次数。
        /// </summary>
        public readonly int ShotBonus;

        /// <summary>
        /// 本次触发解锁的球洞 ID；零表示不解锁。
        /// </summary>
        public readonly int UnlockPocketId;

        /// <summary>
        /// 本次触发开启的可视能力。
        /// </summary>
        public readonly PropVisibility Visibility;

        /// <summary>
        /// 创建道具触发结果。
        /// </summary>
        /// <param name="shotBonus">新增击球次数。</param>
        /// <param name="unlockPocketId">需要解锁的球洞 ID。</param>
        /// <param name="visibility">需要开启的可视能力。</param>
        public PropEffectResult(int shotBonus = 0, int unlockPocketId = 0,
            PropVisibility visibility = PropVisibility.None)
        {
            ShotBonus = shotBonus;
            UnlockPocketId = unlockPocketId;
            Visibility = visibility;
        }
    }
}
