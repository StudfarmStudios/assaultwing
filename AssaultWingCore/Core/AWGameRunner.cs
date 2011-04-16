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
        private Action _draw;
        private Action<AWGameTime> _update;
        private bool _paused;
        private bool _pauseDisabled;
        private object _pausedLock;
        private bool _exiting;
        private SemaphoreSlim _exitSemaphore;
        private IAsyncResult _gameUpdateAndDrawLoopAsyncResult;

        public event Action Initialized;

        public AWGameRunner(AWGame game, Action draw, Action<AWGameTime> update)
        {
            _game = game;
            _timer = new Stopwatch();
            _draw = draw;
            _update = update;
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
            // FIXME: If this method throws an exception, it is not caught by Forms ExceptionHandler Why?
            _game.Initialize();
            _game.LoadContent();
            _game.BeginRun();
            var nextUpdate = TimeSpan.Zero;
            var lastUpdate = TimeSpan.Zero;
            var totalGameTime = TimeSpan.Zero;
            _timer.Start();
            if (Initialized != null)
            {
                Initialized();
                Initialized = null;
            }
            while (!_exiting)
            {
                lock (_timer)
                {
                    var now = _timer.Elapsed;
                    if (now + Waiter.PRECISION < nextUpdate)
                        Waiter.Instance.Sleep(nextUpdate - now);
                    else if (now > nextUpdate + TimeSpan.FromSeconds(10))
                        nextUpdate = now;
                    else
                    {
                        var updateInterval = _game.TargetElapsedTime;
                        var nextNextUpdate = nextUpdate + updateInterval;
                        var gameTime = new AWGameTime(totalGameTime, updateInterval, _timer.Elapsed);
                        _update(gameTime);
                        if (now < nextNextUpdate) _draw();
                        nextUpdate = nextNextUpdate;
                        lastUpdate = now;
                        totalGameTime += updateInterval;
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
