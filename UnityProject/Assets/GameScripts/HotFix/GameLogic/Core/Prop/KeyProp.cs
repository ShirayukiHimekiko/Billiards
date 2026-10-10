namespace GameLogic
{
    /// <summary>
    /// 接触拾取后解锁绑定球洞的钥匙表现组件。
    /// </summary>
    public sealed class KeyProp : PropBase
    {
        /// <summary>
        /// 钥匙拾取后从台面表现中回收，重置时重新显示。
        /// </summary>
        /// <param name="available">钥匙是否尚未拾取。</param>
        public override void SetAvailable(bool available)
        {
            gameObject.SetActive(available);
            base.SetAvailable(available);
        }
    }
}
