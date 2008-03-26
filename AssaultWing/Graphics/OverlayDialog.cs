using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;
using AW2.UI;
using AW2.Events;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class OverlayDialog : Microsoft.Xna.Framework.DrawableGameComponent
    {
        enum DialogMode
        {
            /// <summary>
            /// Dialog is out of the screen.
            /// </summary>
            Out,

            /// <summary>
            /// Dialog is entering the screen.
            /// </summary>
            Entry,

            /// <summary>
            /// Dialog is totally in the screen.
            /// </summary>
            In,

            /// <summary>
            /// Dialog is exiting the screen.
            /// </summary>
            Exit,
        }

        SpriteFont textWriter;
        SpriteBatch spriteBatch;
        Texture2D dialogTexture;
        string dialogText;
        Action<object> yesAction;
        Action<object> noAction;
        List<Control> dialogYesControls, dialogNoControls;

        /// <summary>
        /// Curve along which the dialog moves on entry.
        /// Maps time since movement start, measured in real time seconds,
        /// to relative coordinates between 0 (movement started)
        /// and 1 (movement finished).
        /// </summary>
        Curve dialogEntry;

        /// <summary>
        /// Curve along which the dialog moves on exit.
        /// Maps time since movement start, measured in real time seconds,
        /// to relative coordinates between 0 (movement started)
        /// and 1 (movement finished).
        /// </summary>
        Curve dialogExit;

        /// <summary>
        /// The time it takes the dialog to enter.
        /// </summary>
        TimeSpan dialogEntryDuration;

        /// <summary>
        /// The time it takes the dialog to exit.
        /// </summary>
        TimeSpan dialogExitDuration;

        /// <summary>
        /// Location of the dialog.
        /// </summary>
        DialogMode dialogMode;

        /// <summary>
        /// Time, in real time, at which the dialog's movement started,
        /// or unspecified if <b>dialogMode</b> is In or Out.
        /// </summary>
        TimeSpan dialogMoveStart;

        /// <summary>
        /// The relative coordinate of the dialog,
        /// between 0 (movement started) and 1 (movement finished),
        /// at the beginning of the current entry or exit.
        /// Usually 0 unless movement started at an unusual position.
        /// </summary>
        float dialogShiftStart;

        /// <summary>
        /// Time, measured in real time seconds, of how long has the dialog
        /// been moving in the current movement (exit or entry).
        /// </summary>
        float TimeMoved { get { return (float)(AssaultWing.Instance.GameTime.TotalRealTime.TotalSeconds - dialogMoveStart.TotalSeconds); } }

        /// <summary>
        /// The text to display in the dialog.
        /// </summary>
        public string DialogText { get { return dialogText; } set { dialogText = value; } }

        /// <summary>
        /// The action to perform when the user gives positive input.
        /// </summary>
        public Action<object> YesAction { set { yesAction = value; } }

        /// <summary>
        /// The action to perform when the user gives negative input.
        /// </summary>
        public Action<object> NoAction { set { noAction = value; } }

        /// <summary>
        /// Creates an overlay dialog.
        /// </summary>
        /// <param name="game">The game instance to attach the dialog to.</param>
        public OverlayDialog(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            dialogText = "Huh?";
            yesAction = delegate(object obj) { };
            noAction = delegate(object obj) { };
            dialogYesControls = new List<Control>(); 
            dialogYesControls.Add(new KeyboardKey(Keys.Y));
            dialogNoControls = new List<Control>();
            dialogNoControls.Add(new KeyboardKey(Keys.N));
            dialogNoControls.Add(new KeyboardKey(Keys.Escape));
            dialogEntry = new Curve();
            dialogEntry.Keys.Add(new CurveKey(0, 0));
            dialogEntry.Keys.Add(new CurveKey(0.15f, 0.33f));
            dialogEntry.Keys.Add(new CurveKey(0.45f, 0.80f));
            dialogEntry.Keys.Add(new CurveKey(0.6f, 0.95f));
            dialogEntry.Keys.Add(new CurveKey(1.0f, 1));
            dialogEntry.ComputeTangents(CurveTangent.Smooth);
            dialogEntry.PostLoop = CurveLoopType.Constant;
            dialogExit = new Curve();
            dialogExit.Keys.Add(new CurveKey(0, 0));
            dialogExit.Keys.Add(new CurveKey(0.15f, 0.33f));
            dialogExit.Keys.Add(new CurveKey(0.45f, 0.80f));
            dialogExit.Keys.Add(new CurveKey(0.6f, 0.95f));
            dialogExit.Keys.Add(new CurveKey(1.0f, 1));
            dialogExit.ComputeTangents(CurveTangent.Smooth);
            dialogExit.PostLoop = CurveLoopType.Constant;
            dialogEntryDuration = new TimeSpan((long)(10 * 1000 * 1000 * dialogEntry.Keys[dialogEntry.Keys.Count - 1].Position));
            dialogExitDuration = new TimeSpan((long)(10 * 1000 * 1000 * dialogExit.Keys[dialogExit.Keys.Count - 1].Position));
            dialogMode = DialogMode.Out;
            dialogMoveStart = new TimeSpan();
            dialogShiftStart = 0;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // Update dialog mode.
            if (dialogMode == DialogMode.Entry && dialogMoveStart + dialogEntryDuration <= gameTime.TotalRealTime)
                dialogMode = DialogMode.In;
            if (dialogMode == DialogMode.Exit && dialogMoveStart + dialogExitDuration <= gameTime.TotalRealTime)
            {
                dialogMode = DialogMode.Out;
                yesAction(null);
            }

            // Check our controls and react to them.
            foreach (Control control in dialogYesControls)
                if (control.Pulse)
                {
                    // Make the dialog exit and then perform the 'yes' action.
                    ChangeMode(DialogMode.Exit);
                    break;
                }
            foreach (Control control in dialogNoControls)
                if (control.Pulse)
                {
                    noAction(null);
                    break;
                }

#if DEBUG
            // Check for cheat codes.
            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.K) && keys.IsKeyDown(Keys.P))
            {
                // K + P = kill players
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                data.ForEachPlayer(delegate(Player player)
                {
                    if (player.Ship != null)
                        player.Ship.Die();
                });
            }
#endif

            base.Update(gameTime);
        }

        /// <summary>
        /// Called when graphics resources need to be loaded. Override this method to
        /// load any component-specific graphics resources.
        /// </summary>
        protected override void LoadContent()
        {           
            textWriter = this.Game.Content.Load<SpriteFont>(System.IO.Path.Combine("fonts", "DotMatrix"));
            dialogTexture = this.Game.Content.Load<Texture2D>(System.IO.Path.Combine("textures", "dialog")); 
            spriteBatch = new SpriteBatch(this.GraphicsDevice);
        }

        /// <summary>
        /// Called when the DrawableGameComponent needs to be drawn. Override this method
        /// with component-specific drawing code.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Microsoft.Xna.Framework.DrawableGameComponent.Draw(Microsoft.Xna.Framework.GameTime).</param>
        public override void Draw(GameTime gameTime)
        {
            #region Overlay menu
            float relativePos = 0;
            if (dialogMode == DialogMode.Entry)
                relativePos = dialogShiftStart + (1 - dialogShiftStart) * (dialogEntry.Evaluate(TimeMoved) - 1);
            if (dialogMode == DialogMode.Exit)
                relativePos = dialogShiftStart + (1 - dialogShiftStart) * -dialogEntry.Evaluate(TimeMoved);
            Vector2 dialogShift = new Vector2(dialogTexture.Width, 0) * relativePos;

            spriteBatch.Begin();
            Vector2 dialogTopLeft = new Vector2(0, AssaultWing.Instance.ClientBounds.Height - dialogTexture.Height) / 2
                + dialogShift;
            Vector2 textCenter = dialogTopLeft + new Vector2(474, 150);
            Vector2 textSize = textWriter.MeasureString(dialogText);
            spriteBatch.Draw(dialogTexture, dialogTopLeft, Color.White);
            spriteBatch.DrawString(textWriter, dialogText, textCenter, Color.White, 0,
                textSize / 2, 1, SpriteEffects.None, 0);
            spriteBatch.End();
            #endregion

        }

        /// <summary>
        /// Called when the Visible property changes. Raises the VisibleChanged event.
        /// </summary>
        /// <param name="sender">The DrawableGameComponent.</param>
        /// <param name="args">Arguments to the VisibleChanged event.</param>
        protected override void OnVisibleChanged(object sender, EventArgs args)
        {
            if (Visible)
            {
                ChangeMode(DialogMode.Entry);
            }
            base.OnVisibleChanged(sender, args);
        }

        /// <summary>
        /// Changes the dialog's mode.
        /// </summary>
        /// <param name="newMode">The new mode. Use <b>DialogMode.Entry</b>
        /// or <b>DialogMode.Exit</b>.</param>
        void ChangeMode(DialogMode newMode)
        {
            if (newMode == dialogMode) return;
            switch (newMode)
            {
                case DialogMode.In:
                    throw new ArgumentException("Please use DialogMode.Entry or DialogMode.Exit");
                case DialogMode.Out:
                    throw new ArgumentException("Please use DialogMode.Entry or DialogMode.Exit");
                case DialogMode.Entry:
                    // Entry starts midway an exit?
                    if (dialogMode == DialogMode.Exit)
                        dialogShiftStart = 1 - dialogExit.Evaluate(TimeMoved);
                    else
                        dialogShiftStart = 0;
                    break;
                case DialogMode.Exit:
                    // Exit starts midway an entry?
                    if (dialogMode == DialogMode.Entry)
                        dialogShiftStart = 1 - dialogEntry.Evaluate(TimeMoved);
                    else
                        dialogShiftStart = 0;
                    break;
            }
            dialogMoveStart = AssaultWing.Instance.GameTime.TotalRealTime;
            dialogMode = newMode;
        }
    }
}