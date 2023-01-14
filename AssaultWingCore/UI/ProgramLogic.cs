using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Core.GameComponents;

namespace AW2.UI
{
    /// <summary>
    /// Manages a set of program components, providing a user experience consisting of control
    /// flow from components to others throughout a run of the program. Examples of logics are
    /// dedicated server and standard desktop launch.
    /// </summary>
    public abstract class ProgramLogic<E>
    {
        private bool _gameStateChanged = true;
        private int _gameState;
        protected int GameState
        {
            get { return _gameState; }
            set
            {
                DisableGameState(_gameState);
                _gameState = value;
                _gameStateChanged = true;
                EnableGameState(value);
            }
        }

        public bool GameStateChanged
        {
            get { return _gameStateChanged; }
            set
            {
                _gameStateChanged = value;
            }
        }

        public AssaultWing<E> Game { get; private set; }
        public virtual bool IsGameplay { get { return true; } }

        public ProgramLogic(AssaultWing<E> game)
        {
            Game = game;
        }

        public virtual void Initialize() { }
        public virtual void Update() { }
        public virtual void EndRun() { }
        public virtual void StartArena() { }
        public virtual void FinishArena() { }

        // TODO !!! Make private
        public virtual void PrepareArena(int wallCount) { }
        public virtual void StartServer() { }
        public virtual void StopServer() { }
        public virtual void StopClient(string errorOrNull) { }
        public virtual void ShowMainMenuAndResetGameplay() { }
        public virtual void ShowEquipMenu() { }

        public virtual void ExternalEvent(E e) { }
        public virtual void ShowCustomDialog(string text, string groupName, params TriggeredCallback[] actions) { }

        /// <summary>
        /// Like calling <see cref="ShowDialog"/> with <see cref="TriggeredCallback.PROCEED_CONTROL"/> that
        /// doesn't do anything.
        /// </summary>
        public virtual void ShowInfoDialog(string text, string groupName = null) { }

        public virtual void HideDialog(string groupName = null) { }

        protected virtual void EnableGameState(int value) { }
        protected virtual void DisableGameState(int value) { }
    }
}
