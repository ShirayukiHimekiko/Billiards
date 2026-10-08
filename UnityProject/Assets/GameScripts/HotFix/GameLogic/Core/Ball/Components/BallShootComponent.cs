using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// 处理台面瞄准、蓄力和击球输入。
    /// </summary>
    public sealed class BallShootComponent : BallComponentBase
    {
        /// <summary>
        /// 当前绑定的桌球会话，释放或解绑后为空。
        /// </summary>
        private Session _session;

        /// <summary>
        /// 当前关卡的台面表现与输入投影入口。
        /// </summary>
        private BoardView _board;

        /// <summary>
        /// 最近一次有效瞄准得到的单位方向。
        /// </summary>
        private Vector2 _direction = Vector2.right;

        /// <summary>
        /// 当前累计的蓄力秒数，使用非缩放时间。
        /// </summary>
        private float _charge;

        /// <summary>
        /// 获取是否正在蓄力。
        /// </summary>
        public bool Charging
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取归一化蓄力值。
        /// </summary>
        public float Power
        {
            get
            {
                return Charging ? Mathf.Clamp01(_charge / Config.ChargeDuration) : 0;
            }
        }

        /// <summary>
        /// 绑定会话及台面，绑定变化时取消蓄力。
        /// </summary>
        /// <param name="session">当前会话，释放时传空。</param>
        /// <param name="board">当前台面，释放时传空。</param>
        public void SetSession(Session session, BoardView board)
        {
            CancelCharge();
            _session = session;
            _board = board;
        }

        /// <summary>
        /// 每帧更新瞄准、蓄力及桌面内的出杆操作。
        /// </summary>
        private void Update()
        {
            if (_session == null || Ball == null || Mouse.current == null)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Charging)
                {
                    CancelCharge();
                }
                else
                {
                    _session.TogglePause();
                }
            }

            if (_session.State != SessionState.Ready)
            {
                CancelCharge();
                _board.SetAim(Ball.State.Position, _direction, Ball.State.Radius, false);

                return;
            }

            Vector2 screen = Mouse.current.position.ReadValue();
            bool canAim = _board.TryProjectScreenPoint(screen, out Vector2 target);
            Vector2 direction = target - Ball.State.Position;

            if (canAim && direction.sqrMagnitude > 0.0001f)
            {
                _direction = direction.normalized;
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelCharge();
                _board.SetAim(Ball.State.Position, _direction, Ball.State.Radius, canAim);

                return;
            }

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool canShoot = canAim && _board.IsOnTable(target) && !overUI;

            if (Mouse.current.leftButton.wasPressedThisFrame && canShoot)
            {
                Charging = true;
                _charge = 0;
            }

            if (Charging)
            {
                _charge += Time.unscaledDeltaTime;
            }

            if (Charging && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                float power = Power;
                CancelCharge();

                if (canShoot)
                {
                    _session.Shoot(_direction, power);
                }
            }

            _board.SetAim(Ball.State.Position, _direction, Ball.State.Radius, canAim && _session.State == SessionState.Ready, Power);
        }

        /// <summary>
        /// 取消当前蓄力。
        /// </summary>
        public void CancelCharge()
        {
            Charging = false;
            _charge = 0;
        }

        /// <summary>
        /// 取消蓄力并恢复初始瞄准方向。
        /// </summary>
        public override void ResetFeature()
        {
            CancelCharge();
            _direction = Vector2.right;
        }

        /// <summary>
        /// 解绑会话、台面和球体。
        /// </summary>
        public override void Release()
        {
            SetSession(null, null);
            base.Release();
        }
    }
}
