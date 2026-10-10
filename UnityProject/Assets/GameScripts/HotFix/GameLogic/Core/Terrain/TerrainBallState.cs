namespace GameLogic
{
    /// <summary>
    /// 球当前受到的地形局部状态；纯数据，可由预测世界深复制。
    /// </summary>
    public sealed class TerrainBallState
    {
        public bool Airborne;
        public bool TunnelActive;
        public bool Submerged;
        public bool ReverseActive;
        public float Remaining;
        public float TeleportCooldown;
        public int TerrainId;

        public bool SuppressesSurfaceContacts => Airborne || TunnelActive || Submerged;

        public TerrainBallState Copy()
        {
            return new TerrainBallState
            {
                Airborne = Airborne,
                TunnelActive = TunnelActive,
                Submerged = Submerged,
                ReverseActive = ReverseActive,
                Remaining = Remaining,
                TeleportCooldown = TeleportCooldown,
                TerrainId = TerrainId
            };
        }

        public void Clear()
        {
            Airborne = false;
            TunnelActive = false;
            Submerged = false;
            ReverseActive = false;
            Remaining = 0;
            TeleportCooldown = 0;
            TerrainId = 0;
        }
    }
}
