using System;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Windows.Forms;
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
            _game.Initialize();
            _game.BeginRun();
            ((Action)BackgroundLoop).BeginInvoke(BackgroundLoopEnd, null);
        }

        public void Exit()
        {
            _exiting = true;
            // Wait for BackgroundLoop to finish
            while (!_exited) Thread.Sleep(100);
        }

        private void BackgroundLoop()
        {
            var nextUpdate = TimeSpan.Zero;
            var lastUpdate = TimeSpan.Zero;
            var totalGameTime = TimeSpan.Zero;
            var timer = new Stopwatch();
            timer.Start();
            while (!_exiting)
            {
                var now = timer.Elapsed;
                if (now < nextUpdate)
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
            _exited = true;
        }

        private void BackgroundLoopEnd(IAsyncResult result)
        {
            var deleg = (Action)((AsyncResult)result).AsyncDelegate;
            deleg.EndInvoke(result);
            _game.EndRun();
            _game.Dispose();
        }
    }
}
