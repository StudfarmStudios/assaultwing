using System;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Microsoft.Xna.Framework;

namespace AW2.Core
{
    public class AWGameRunner
    {
        private AWGame _game;
        private Stopwatch _timer;
        private Action<Action> _invoker;
        private Action<Exception> _exceptionHandler;
        private Action _draw;
        private Action<AWGameTime> _update;
        private bool _paused;
        private bool _pauseDisabled;
        private object _pausedLock;
        private bool _exiting;
        private SemaphoreSlim _exitSemaphore;
        private IAsyncResult _gameUpdateAndDrawLoopAsyncResult;
        private bool _readyForNextUpdate;
        private bool _readyForNextDraw;

        public event Action Initialized;

        /// <param name="invoker">A delegate that invokes a delegate in the main thread.</param>
        /// <param name="draw">A delegate that draws the game screen.</param>
        /// <param name="update">A delegate that updates the game world.</param>
        public AWGameRunner(AWGame game, Action<Action> invoker, Action<Exception> exceptionHandler, Action draw, Action<AWGameTime> update)
        {
            if (game == null || exceptionHandler == null || draw == null || update == null) throw new ArgumentNullException();
            _game = game;
            _invoker = invoker;
            _exceptionHandler = exceptionHandler;
            _draw = draw;
            _update = update;
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
        /// To be called when a frame update has finished.
        /// </summary>
        public void UpdateFinished()
        {
            _readyForNextUpdate = true;
        }

        /// <summary>
        /// To be called when a frame draw has finished.
        /// </summary>
        public void DrawFinished()
        {
            _readyForNextDraw = true;
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
                _game.Initialize();
                _game.BeginRun();
            });
            var nextUpdate = TimeSpan.Zero;
            var lastUpdate = TimeSpan.Zero;
            var totalGameTime = TimeSpan.Zero;
            _timer.Start();
            if (Initialized != null)
            {
                Initialized();
                Initialized = null;
            }
            _readyForNextUpdate = true;
            _readyForNextDraw = true;
            while (!_exiting)
            {
                lock (_timer)
                {
                    var now = _timer.Elapsed;
                    if (now + Waiter.PRECISION < nextUpdate)
                    {
                        // It's not yet time for update.
                        Waiter.Instance.Sleep(nextUpdate - now);
                    }
                    else if (now > nextUpdate + TimeSpan.FromSeconds(10))
                    {
                        // Update is lagging a lot; skip over the missed updates.
                        nextUpdate = now;
                    }
                    else if (_readyForNextUpdate && _readyForNextDraw)
                    {
                        var updateInterval = _game.TargetElapsedTime;
                        var nextNextUpdate = nextUpdate + updateInterval;
                        var gameTime = new AWGameTime(totalGameTime, updateInterval, _timer.Elapsed);
                        _readyForNextUpdate = false;
                        _update(gameTime);
                        if (now < nextNextUpdate)
                        {
                            // There is time left before the following update; draw in the meantime.
                            _readyForNextDraw = false;
                            _draw();
                        }
                        nextUpdate = nextNextUpdate;
                        lastUpdate = now;
                        totalGameTime += updateInterval;
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
