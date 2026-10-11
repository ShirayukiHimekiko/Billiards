namespace GameLogic
{
    /// <summary>
    /// 地形实例的派生表现状态；由球阶段聚合得到，不参与模拟，也不随预测副本复制。
    /// </summary>
    public sealed class TerrainRuntimeState
    {
        /// <summary>
        /// 该地形当前是否有球处于持续地形阶段，用于表现高亮。
        /// </summary>
        public bool Highlighted;

        /// <summary>
        /// 重置为未高亮。
        /// </summary>
        public void Reset()
        {
            Highlighted = false;
        }
    }
}
