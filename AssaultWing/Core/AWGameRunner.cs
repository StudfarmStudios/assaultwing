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
        private Action _draw;
        private Action<GameTime> _update;
        private bool _exiting;
        private bool _exited;

        public AWGameRunner(AWGame game, Action draw, Action<GameTime> update)
        {
            _game = game;
            _draw = draw;
            _update = update;
        }

        /// <summary>
        /// Starts running the game in a background thread.
        /// </summary>
        public void Run()
        {
            ((Action)GameUpdateAndDrawLoop).BeginInvoke(GameUpdateAndDrawLoopEnd, null);
        }

        /// <summary>
        /// Exits the previously started thread that updates and draws the game.
        /// </summary>
        public void Exit()
        {
            _exiting = true;
            // Wait for BackgroundLoop to finish
            while (!_exited) Thread.Sleep(100);
        }

        private void GameUpdateAndDrawLoop()
        {
            _game.Initialize();
            _game.LoadContent();
            _game.BeginRun();
            var nextUpdate = TimeSpan.Zero;
            var lastUpdate = TimeSpan.Zero;
            var totalGameTime = TimeSpan.Zero;
            var timer = new Stopwatch();
            timer.Start();
            while (!_exiting)
            {
                var now = timer.Elapsed;
                if (now + Waiter.PRECISION < nextUpdate)
                {
                    Waiter.Instance.Sleep(nextUpdate - now);
                }
                else
                {
                    var updateInterval = _game.TargetElapsedTime;
                    var nextNextUpdate = nextUpdate + updateInterval;
                    var gameTime = new GameTime(timer.Elapsed, now - lastUpdate, totalGameTime, updateInterval);
                    _update(gameTime);
                    if (now < nextNextUpdate) _draw();
                    nextUpdate = nextNextUpdate;
                    lastUpdate = now;
                    totalGameTime += updateInterval;
                }
            }
        }

        private void GameUpdateAndDrawLoopEnd(IAsyncResult result)
        {
            var deleg = (Action)((AsyncResult)result).AsyncDelegate;
            deleg.EndInvoke(result);
            _game.EndRun();
            _game.Dispose();
            _exited = true;
        }
    }
}
