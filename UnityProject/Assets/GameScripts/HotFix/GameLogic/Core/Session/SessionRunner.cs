using UnityEngine;
using UnityEngine.InputSystem;

namespace GameLogic
{
    /// <summary>
    /// 将 Unity 更新及焦点变化转发给当前会话。
    /// </summary>
    public sealed class SessionRunner : MonoBehaviour
    {
        /// <summary>
        /// DEMO 约定的单键重置按键；UI 按钮与该入口共用 Session.Restart。
        /// </summary>
        /// <summary>
        /// 当前绑定的桌球会话，释放或解绑后为空。
        /// </summary>
        private Session _session;

        /// <summary>
        /// 绑定或解除当前会话。
        /// </summary>
        /// <param name="session">待运行的会话，解绑时传空。</param>
        public void Bind(Session session)
        {
            _session = session;
        }

        /// <summary>
        /// 使用非缩放帧时间推进当前会话。
        /// </summary>
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                _session?.Restart();

                return;
            }

            _session?.Tick(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 应用失去焦点时暂停当前会话。
        /// </summary>
        /// <param name="focus">应用是否获得焦点，为 false 时暂停会话。</param>
        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                _session?.LoseFocus();
            }
        }

        /// <summary>
        /// 应用进入后台暂停时暂停当前会话。
        /// </summary>
        /// <param name="pause">应用是否进入后台暂停，为 true 时暂停会话。</param>
        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                _session?.LoseFocus();
            }
        }
    }
}
