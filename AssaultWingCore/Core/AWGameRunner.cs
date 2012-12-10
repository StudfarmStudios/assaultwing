using System;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Microsoft.Xna.Framework;

namespace AW2.Core
{
    public class AWGameRunner
    {
        public delegate void UpdateAndDrawAction(AWGameTime gameTime);

        private AWGame _game;
        private Stopwatch _timer;
        private Action<Action> _invoker;
        private Action<Exception> _exceptionHandler;
        private UpdateAndDrawAction _updateAndDraw;
        private bool _paused;
        private bool _pauseDisabled;
        private object _pausedLock;
        private bool _exiting;
        private SemaphoreSlim _exitSemaphore;
        private IAsyncResult _gameUpdateAndDrawLoopAsyncResult;
        private bool _readyForNextUpdateAndDraw;
        private TimeSpan _nextUpdate;

        public event Action Initialized;

        /// <summary>
        /// If true, then the next frame update should have started already; any pending frame draw
        /// should be skipped to catch up.
        /// </summary>
        public bool IsTimeForNextUpdate { get { return _timer.Elapsed + Waiter.PRECISION >= _nextUpdate; } }

        /// <param name="invoker">A delegate that invokes a delegate in the main thread.</param>
        /// <param name="updateAndDraw">A delegate that updates the game world and draws the game screen.</param>
        public AWGameRunner(AWGame game, Action<Action> invoker, Action<Exception> exceptionHandler, UpdateAndDrawAction updateAndDraw)
        {
            if (game == null || exceptionHandler == null || updateAndDraw == null) throw new ArgumentNullException();
            _game = game;
            _invoker = invoker;
            _exceptionHandler = exceptionHandler;
            _updateAndDraw = updateAndDraw;
            _timer = new Stopwatch();
            _pausedLock = new object();
            _exitSemaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Starts running the game in a background thread.
        /// </summary>
        public void Run()
        {
            _exitSemaphore.Wait();
            if (_exiting) return;
            _gameUpdateAndDrawLoopAsyncResult = ((Action)GameUpdateAndDrawLoop).BeginInvoke(GameUpdateAndDrawLoopEnd, null);
        }

        /// <summary>
        /// To be called when a frame update and draw has finished.
        /// </summary>
        public void UpdateAndDrawFinished()
        {
            _readyForNextUpdateAndDraw = true;
        }

        /// <summary>
        /// Prevents the update and draw loop from running but doesn't stop update timer.
        /// Use only for short pauses.
        /// </summary>
        public void Pause()
        {
            if (_pauseDisabled) return;
            lock (_pausedLock)
            {
                if (_paused) return;
                Monitor.Enter(_timer, ref _paused);
            }
        }

        /// <summary>
        /// Resumes from a previous <see cref="Pause"/>.
        /// </summary>
        public void Resume()
        {
            lock (_pausedLock)
            {
                if (!_paused) return;
                Monitor.Exit(_timer);
                _paused = false;
            }
        }

        /// <summary>
        /// Exits the previously started thread that updates and draws the game.
        /// </summary>
        public void Exit()
        {
            _exiting = true;
            _pauseDisabled = true;
            Resume();
            _exitSemaphore.Wait();
            _exitSemaphore.Release();
        }

        private void GameUpdateAndDrawLoop()
        {
#if !DEBUG
            try
            {
#endif
                GameUpdateAndDrawLoopImpl();
#if !DEBUG
            }
            catch (Exception e)
            {
                _exceptionHandler(e);
            }
#endif
        }

        private void GameUpdateAndDrawLoopImpl()
        {
            _invoker(() =>
            {
                try
                {
                    _game.Initialize();
                    _game.BeginRun();
                }
                catch (Exception e)
                {
                    _exceptionHandler(e);
                }
            });
            var totalGameTime = TimeSpan.Zero;
            _timer.Start();
            if (Initialized != null)
            {
                Initialized();
                Initialized = null;
            }
            _readyForNextUpdateAndDraw = true;
            while (!_exiting)
            {
                lock (_timer)
                {
                    var now = _timer.Elapsed;
                    if (!IsTimeForNextUpdate)
                    {
                        // It's not yet time for update.
                        Waiter.Instance.Sleep(_nextUpdate - now);
                    }
                    else if (now > _nextUpdate + TimeSpan.FromSeconds(10))
                    {
                        // Update is lagging a lot; skip over the missed updates.
                        _nextUpdate = now;
                    }
                    else if (_readyForNextUpdateAndDraw)
                    {
                        var updateInterval = AssaultWingCore.TargetElapsedTime;
                        var gameTime = new AWGameTime(totalGameTime, updateInterval, now);
                        _nextUpdate += updateInterval;
                        totalGameTime += updateInterval;
                        _readyForNextUpdateAndDraw = false;
                        _updateAndDraw(gameTime);
                    }
                    else
                    {
                        // We didn't make it in time for a frame update. Wait for a while and try again.
                        Waiter.Instance.Sleep(TimeSpan.FromMilliseconds(5));
                    }
                }
            }
        }

        private void GameUpdateAndDrawLoopEnd(IAsyncResult result)
        {
            var deleg = (Action)((AsyncResult)result).AsyncDelegate;
            deleg.EndInvoke(result);
            _game.EndRun();
            _exitSemaphore.Release();
        }
    }
}
