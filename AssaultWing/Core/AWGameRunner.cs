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
        private bool _exiting;
        private bool _exited;
        private SemaphoreSlim _pauseSemaphore;
        private IAsyncResult _gameUpdateAndDrawLoopAsyncResult;

        public event Action Initialized;

        private bool BackgroundLoopFinished { get { return _exited || _gameUpdateAndDrawLoopAsyncResult == null; } }

        public AWGameRunner(AWGame game, Action draw, Action<AWGameTime> update)
        {
            _game = game;
            _timer = new Stopwatch();
            _draw = draw;
            _update = update;
            _pauseSemaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Starts running the game in a background thread.
        /// </summary>
        public void Run()
        {
            _gameUpdateAndDrawLoopAsyncResult = ((Action)GameUpdateAndDrawLoop).BeginInvoke(GameUpdateAndDrawLoopEnd, null);
        }

        /// <summary>
        /// Prevents the update and draw loop from running but doesn't stop update timer.
        /// Use only for short pauses.
        /// </summary>
        public void Pause()
        {
            _pauseSemaphore.Wait();
        }

        /// <summary>
        /// Resumes from a previous <see cref="Pause"/>.
        /// </summary>
        public void Resume()
        {
            _pauseSemaphore.Release();
        }

        /// <summary>
        /// Exits the previously started thread that updates and draws the game.
        /// </summary>
        public void Exit()
        {
            _exiting = true;
            while (!BackgroundLoopFinished) Thread.Sleep(100);
        }

        private void GameUpdateAndDrawLoop()
        {
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
                _pauseSemaphore.Wait();
                var now = _timer.Elapsed;
                if (now + Waiter.PRECISION < nextUpdate)
                    Waiter.Instance.Sleep(nextUpdate - now);
                else if (now > nextUpdate + TimeSpan.FromSeconds(30))
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
                _pauseSemaphore.Release();
            }
        }

        private void GameUpdateAndDrawLoopEnd(IAsyncResult result)
        {
            var deleg = (Action)((AsyncResult)result).AsyncDelegate;
            deleg.EndInvoke(result);
            _game.EndRun();
            _exited = true;
        }
    }
}
