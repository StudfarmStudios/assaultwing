using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;

namespace AW2.Core
{
    public class AWGameRunner
    {
        public delegate void Initialize();
        private Initialize _initialize;
        private AWGame _game;
        private Stopwatch _timer;
        private bool _gameInitialized;
        private bool _graphicsEnabled;
        private bool _sleepIfEarly;
        private bool _useParentTime;
        private bool _debug = false;
        private TimeSpan _now;

        private TimeSpan _nextUpdate;
        private TimeSpan totalGameTime = TimeSpan.Zero;

        /// <summary>
        /// If true, then the next frame update should have started already; any pending frame draw
        /// should be skipped to catch up.
        /// </summary>
        public bool IsTimeForNextUpdate { get { return CheckIsTimeForNextUpdate(_timer.Elapsed); } }

        public AWGameRunner(AWGame game, Initialize initialize, bool useParentTime, bool sleepIfEarly, bool graphicsEnabled)
        {
            _game = game;
            _initialize = initialize;
            _timer = new Stopwatch();
            _timer.Start();
            _sleepIfEarly = sleepIfEarly;
            _graphicsEnabled = graphicsEnabled;
            _useParentTime = useParentTime;
        }

        public void Dispose()
        {
            _timer.Stop();
        }

        private bool CheckIsTimeForNextUpdate(TimeSpan now) { return now + Waiter.PRECISION >= _nextUpdate; }

        public void DebugLog(string message)
        {
            if (_debug)
            {
                Debug.WriteLine($"AWGameRunner: {_now / AssaultWingCore.TargetElapsedTime:0.##}: {message}");
            }
        }

        public void Update(GameTime parentTime)
        {
            _now = _timer.Elapsed;

            if (!_gameInitialized)
            {
                DebugLog($"Initialize, precision {Waiter.PRECISION.TotalMilliseconds:0.##}ms, TargetElapsedTime {AssaultWingCore.TargetElapsedTime.TotalMilliseconds:0.####}ms");

                _gameInitialized = true;

                _initialize(); // Do client initializes

                // Initialize the game
                _game.Initialize();
                _game.BeginRun();

                _now = _timer.Elapsed;
                _nextUpdate = _now;
            }

            if (!_useParentTime && !CheckIsTimeForNextUpdate(_now))
            {
                var sleep = _nextUpdate - _now;
                DebugLog($"It's not yet time for update. {sleep.TotalMilliseconds:0.#}ms early");

                if (_sleepIfEarly)
                {
                    Waiter.Instance.Sleep(sleep);
                    _now = _timer.Elapsed;
                }
                else
                {
                    _nextUpdate = _now;
                }
            }

            if (_now > _nextUpdate + TimeSpan.FromSeconds(10))
            {
                DebugLog("Update is lagging a lot; skip over the missed updates.");
                _nextUpdate = _now;
            }

            do
            {
                var updateInterval = AssaultWingCore.TargetElapsedTime;
                AWGameTime gameTime;
                if (_useParentTime)
                {
                    gameTime = new AWGameTime(parentTime.TotalGameTime, parentTime.ElapsedGameTime, _now);
                }
                else
                {
                    gameTime = new AWGameTime(totalGameTime, updateInterval, _now);
                }
                _nextUpdate += updateInterval;
                totalGameTime += updateInterval;

                do
                {
                    DebugLog("Update");
                    _game.UpdateNeeded = false;
                    // There is at least one case where the logic state change expects update to be called again before calling
                    // paint again or it will crash.
                    _game.Update(gameTime);
                } while (_game.UpdateNeeded);

                _now = _timer.Elapsed;
                // Loop updates to catch up with the updates if necessary
            } while (!_useParentTime && CheckIsTimeForNextUpdate(_now));


            // We can't skip paint or we risk graphics flickering
            if (_graphicsEnabled)
            {
                DebugLog("Paint");
                _game.Draw();
            }
        }
    }
}
