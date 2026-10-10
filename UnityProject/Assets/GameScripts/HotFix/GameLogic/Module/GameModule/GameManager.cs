using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 管理菜单、关卡加载和桌球会话的生命周期。
    /// </summary>
    public sealed class GameManager : Singleton<GameManager>
    {
        /// <summary>
        /// 流程管理器初始保存的关卡 ID。
        /// </summary>
        private const int INITIAL_LEVEL_ID = 1;

        /// <summary>
        /// 负责配置聚合和关卡目录查询的管理器。
        /// </summary>
        private readonly LevelManager _levels = new LevelManager();

        /// <summary>
        /// 负责球体加载、同步与回收的管理器。
        /// </summary>
        private readonly BallManager _balls = new BallManager();

        /// <summary>
        /// 负责道具加载、效果策略与表现同步的管理器。
        /// </summary>
        private readonly PropManager _props = new PropManager();

        /// <summary>
        /// 负责六洞状态表现的管理器。
        /// </summary>
        private readonly PocketManager _pockets = new PocketManager();

        /// <summary>
        /// 负责图形进度表现与提示显示的管理器。
        /// </summary>
        private readonly GeometryManager _geometries = new GeometryManager();

        /// <summary>
        /// 负责地形实例加载、同步与释放的管理器。
        /// </summary>
        private readonly TerrainManager _terrains = new TerrainManager();

        /// <summary>
        /// 当前选择或正在运行的关卡 ID。
        /// </summary>
        private int _levelId = INITIAL_LEVEL_ID;

        /// <summary>
        /// 当前关卡的台面表现与输入投影入口。
        /// </summary>
        private BoardView _board;

        /// <summary>
        /// 资源系统加载的台面实例，卸载关卡时销毁。
        /// </summary>
        private GameObject _tableObject;

        /// <summary>
        /// 当前进入流程的取消源，由该流程完成后释放。
        /// </summary>
        private CancellationTokenSource _enterCancellation;

        /// <summary>
        /// 是否正在准备关卡，用于阻止重复进入请求。
        /// </summary>
        private bool _entering;

        /// <summary>
        /// 是否正在打开选关窗口，用于串行处理导航。
        /// </summary>
        private bool _navigating;

        /// <summary>
        /// 退出状态，阻止延迟完成的异步流程重新操作界面。
        /// </summary>
        private bool _shuttingDown;

        /// <summary>
        /// 进入请求版本，取消后使旧请求的失败回调失效。
        /// </summary>
        private int _request;

        /// <summary>
        /// 获取当前正在运行的桌球会话。
        /// </summary>
        public Session Session
        {
            get;
            private set;
        }

        /// <summary>
        /// 按配置排序的选关目录，不加载关卡世界。
        /// </summary>
        public IReadOnlyList<GameConfig.Level> LevelCatalogue => _levels.GetCatalogue();

        /// <summary>
        /// 显示开始菜单。
        /// </summary>
        /// <param name="message">显示给玩家的提示。</param>
        public async UniTask ShowMenuAsync(string message = "")
        {
            if (_shuttingDown)
            {
                return;
            }

            try
            {
                var menu = await GameModule.UI.ShowUIAsyncAwait<StartUI>();

                if (_shuttingDown)
                {
                    return;
                }

                if (menu == null)
                {
                    throw new InvalidOperationException("StartUI 加载失败。");
                }

                menu.SetMessage(message);
                GameModule.UI.CloseUI<LevelSelectUI>();
            }
            catch (Exception exception)
            {
                Log.Error($"开始界面打开失败：{exception}");
            }
        }

        /// <summary>
        /// 打开选关界面，加载成功后关闭开始菜单。
        /// </summary>
        /// <param name="message">选关提示或准备失败原因。</param>
        public async UniTask ShowLevelSelectAsync(string message = "")
        {
            if (_shuttingDown || _entering || _navigating || Session != null)
            {
                return;
            }

            _navigating = true;

            try
            {
                var selection = await GameModule.UI.ShowUIAsyncAwait<LevelSelectUI>();

                if (_shuttingDown)
                {
                    return;
                }

                if (selection == null)
                {
                    throw new InvalidOperationException("选关界面加载失败。");
                }

                selection.SetMessage(message);
                GameModule.UI.CloseUI<SettingsUI>();
                GameModule.UI.CloseUI<StartUI>();
            }
            catch (Exception exception)
            {
                Log.Error($"选关界面打开失败：{exception}");

                if (!_shuttingDown)
                {
                    GameModule.UI.CloseUI<LevelSelectUI>();
                    await ShowMenuAsync("选关界面打开失败，请重试。");
                }
            }
            finally
            {
                _navigating = false;
            }
        }

        /// <summary>
        /// 异步进入指定关卡，取消或失败时回收已加载内容。
        /// </summary>
        /// <param name="levelId">选关目录中已存在的关卡 ID。</param>
        public async UniTask StartGameAsync(int levelId)
        {
            if (_shuttingDown || _entering || _navigating || Session != null)
            {
                return;
            }

            _levelId = levelId;
            _entering = true;

            string failureMessage = null;
            int request = ++_request;
            var cancellation = new CancellationTokenSource();
            _enterCancellation = cancellation;

            try
            {
                await PrepareGameAsync(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                Cleanup();

                // 取消后窗口仍可能加载完成；退出时 UI 模块也可能已经释放。
                if (!_shuttingDown)
                {
                    GameModule.UI.CloseUI<MainUI>();
                }
            }
            catch (Exception exception)
            {
                Log.Error($"进入关卡失败：{exception}");

                if (!_shuttingDown && request == _request)
                {
                    Cleanup();
                    GameModule.UI.CloseUI<MainUI>();
                    failureMessage = "准备失败，请重试：" + exception.Message;
                }
            }
            finally
            {
                if (_enterCancellation == cancellation)
                {
                    _enterCancellation = null;
                }

                cancellation.Dispose();
                _entering = false;
            }

            if (failureMessage != null && !_shuttingDown && request == _request)
            {
                await ShowLevelSelectAsync(failureMessage);
            }
        }

        /// <summary>
        /// 依次准备主界面、世界台面、关卡与会话。
        /// </summary>
        /// <param name="token">当前进入流程的取消令牌。</param>
        private async UniTask PrepareGameAsync(CancellationToken token)
        {
            GameModule.UI.CloseUI<SettingsUI>();
            GameModule.UI.CloseUI<StartUI>();
            GameModule.UI.CloseUI<LevelSelectUI>();

            var main = await GameModule.UI.ShowUIAsyncAwait<MainUI>();
            token.ThrowIfCancellationRequested();

            if (main == null)
            {
                throw new InvalidOperationException("主界面加载失败。");
            }

            var level = _levels.LoadLevel(_levelId, token);
            _tableObject = await GameModule.Resource.LoadGameObjectAsync(level.BoardLocation, cancellationToken: token);
            // 延迟完成的实例也交由调用方 Cleanup 回收，避免取消造成资源残留。
            token.ThrowIfCancellationRequested();

            if (_tableObject == null)
            {
                throw new InvalidOperationException("2D 桌球台加载失败。");
            }

            _board = _tableObject.GetComponent<BoardView>();

            if (_board == null)
            {
                throw new InvalidOperationException("桌球台缺少 BoardView。");
            }

            _board.Prepare(level);
            await _balls.InitializeAsync(level, _board, token);
            await _props.InitializeAsync(level, _board, token);
            await _terrains.InitializeAsync(level, _board, token);
            _pockets.Initialize(level, _board);
            _geometries.Initialize(level, _board);
            token.ThrowIfCancellationRequested();
            main.ConfigureLevel(level, _board);
            Session = new Session(level, _balls, _props, _pockets, _geometries, _terrains, _board);
            _board.Runner.Bind(Session);
        }

        /// <summary>
        /// 取消未完成的加载，释放关卡并返回菜单。
        /// </summary>
        public async UniTask ReturnToMenuAsync()
        {
            if (_shuttingDown)
            {
                return;
            }

            await StopGameAsync();
            await ShowMenuAsync();
        }

        /// <summary>
        /// 结束当前关卡并回到选关目录。
        /// </summary>
        public async UniTask ReturnToLevelSelectAsync()
        {
            if (_shuttingDown)
            {
                return;
            }

            await StopGameAsync();
            await ShowLevelSelectAsync();
        }

        /// <summary>
        /// 等待被取消的进入流程完成回收，再切换窗口。
        /// </summary>
        private async UniTask StopGameAsync()
        {
            CancelEntering();

            if (_entering)
            {
                await UniTask.WaitUntil(() => !_entering || _shuttingDown);
            }

            if (_shuttingDown)
            {
                return;
            }

            Cleanup();
            GameModule.UI.CloseUI<MainUI>();
            GameModule.UI.CloseUI<SettingsUI>();
        }

        /// <summary>
        /// 取消当前进入请求并使旧请求失效。
        /// </summary>
        private void CancelEntering()
        {
            ++_request;
            _enterCancellation?.Cancel();
        }

        /// <summary>
        /// 按会话、球体、台面的顺序释放关卡内容。
        /// </summary>
        private void Cleanup()
        {
            // 先停止帧驱动和输入绑定，再销毁表现实例，避免释放后继续访问 Unity 对象。
            if (_board != null && _board.Runner != null)
            {
                _board.Runner.Bind(null);
            }

            Session?.Release();
            Session = null;
            _geometries.Release();
            _terrains.Release();
            _pockets.Release();
            _props.Release();
            _balls.Release();
            _levels.UnloadLevel(_board);
            _board = null;

            if (_tableObject != null)
            {
                UnityEngine.Object.Destroy(_tableObject);
            }

            _tableObject = null;
        }

        /// <summary>
        /// 清理关卡并退出游戏；编辑器中结束 Play Mode。
        /// </summary>
        public void ExitGame()
        {
            _shuttingDown = true;
            CancelEntering();
            Cleanup();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 结束管理器生命周期，取消加载并清理当前关卡。
        /// </summary>
        protected override void OnRelease()
        {
            _shuttingDown = true;
            CancelEntering();
            Cleanup();
        }

        /// <summary>
        /// 通关后按主表排序进入下一关，最后一关返回选关。
        /// </summary>
        public async UniTask NextLevelAsync()
        {
            if (_entering || Session == null || Session.State != SessionState.Won)
            {
                return;
            }

            int next = _levels.NextLevelId(_levelId);

            if (next == 0)
            {
                await ReturnToLevelSelectAsync();

                return;
            }

            Cleanup();
            GameModule.UI.CloseUI<MainUI>();
            await StartGameAsync(next);
        }
    }
}
