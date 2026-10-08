namespace GameLogic
{
    /// <summary>
    /// 单例生命周期接口。
    /// </summary>
    public interface ISingleton
    {
        /// <summary>
        /// 激活接口，通常用于在某个时机手动实例化。
        /// </summary>
        void Active();

        /// <summary>
        /// 释放接口。
        /// </summary>
        void Release();
    }
}
