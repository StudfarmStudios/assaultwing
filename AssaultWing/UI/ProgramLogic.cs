using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Core.GameComponents;
using AW2.Core.OverlayComponents;
using AW2.Menu;

namespace AW2.UI
{
    /// <summary>
    /// Manages a set of program components, providing a user experience consisting of control
    /// flow from components to others throughout a run of the program. Examples of logics are
    /// dedicated server and standard desktop launch.
    /// </summary>
    public abstract class ProgramLogic
    {
        public AssaultWing Game { get; private set; }
        public virtual bool IsShipControlsEnabled { get { return true; } }

        public ProgramLogic(AssaultWing game)
        {
            Game = game;
        }

        public virtual void Initialize() { }
        public virtual void Update() { }
        public virtual void EndRun() { }
        public virtual void StartArena() { }
        public virtual void FinishArena() { }

        public virtual void EnableGameState(GameState value) { }
        public virtual void DisableGameState(GameState value) { }

        // TODO !!! Make private
        public virtual void PrepareArena() { }
        public virtual void StopServer() { }
        public virtual void StopClient(string errorOrNull) { }
        public virtual void ShowMainMenuAndResetGameplay() { }
        public virtual void ShowEquipMenu() { }

        public virtual void ShowDialog(OverlayDialogData dialogData) { }

        /// <summary>
        /// Like calling <see cref="ShowDialog"/> with <see cref="TriggeredCallback.PROCEED_CONTROL"/> that
        /// doesn't do anything.
        /// </summary>
        public virtual void ShowInfoDialog(string text, string groupName = null) { }

        public virtual void HideDialog(string groupName = null) { }
    }
}
