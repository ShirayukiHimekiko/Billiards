namespace GameLogic
{
    /// <summary>
    /// 地形实例的可变状态；与球状态分离，便于会话重置和表现同步。
    /// </summary>
    public sealed class TerrainRuntimeState
    {
        public bool Active;
        public bool EntryLocked;
        public float CooldownRemaining;
        public bool Airborne;
        public bool TunnelActive;
        public bool Submerged;

        public TerrainRuntimeState Copy()
        {
            return new TerrainRuntimeState
            {
                Active = Active,
                EntryLocked = EntryLocked,
                CooldownRemaining = CooldownRemaining,
                Airborne = Airborne,
                TunnelActive = TunnelActive,
                Submerged = Submerged
            };
        }

        public void Reset()
        {
            Active = false;
            EntryLocked = false;
            CooldownRemaining = 0;
            Airborne = false;
            TunnelActive = false;
            Submerged = false;
        }
    }
}
