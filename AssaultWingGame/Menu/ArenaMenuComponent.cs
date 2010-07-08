using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Graphics;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Menu
{
    /// <summary>
    /// The arena selection menu component where players can choose which arenas to play.
    /// </summary>
    class ArenaMenuComponent : MenuComponent
    {
        const int MENU_ITEM_COUNT = 8; // number of items that fit in the menu at once

        Control controlBack, controlDone;
        MultiControl controlUp, controlDown;
        TriggeredCallbackCollection controlCallbacks;
        Vector2 pos; // position of the component's background texture in menu system coordinates
        SpriteFont menuBigFont, menuSmallFont;
        Texture2D backgroundTexture, cursorTexture, highlightTexture, tagTexture;
        List<Texture2D> arenaPreviews;

        /// <summary>
        /// Cursor fade curve as a function of time in seconds.
        /// Values range from 0 (transparent) to 255 (opaque).
        /// </summary>
        Curve cursorFade;

        /// <summary>
        /// Time at which the cursor started fading.
        /// </summary>
        TimeSpan cursorFadeStartTime;

        /// <summary>
        /// Index of currently highlighted arena in the arena name list.
        /// </summary>
        int currentArena;

        /// <summary>
        /// Index of first arena in the arena name list that is visible on screen.
        /// </summary>
        int arenaListStart;

        List<ArenaInfo> ArenaInfos { get { return AssaultWing.Instance.DataEngine.ArenaInfos; } }
        List<string> selectedArenaNames;

        /// <summary>
        /// Does the menu component react to input.
        /// </summary>
        public override bool Active
        {
            set
            {
                base.Active = value;
                if (value)
                {
                    InitializeControls();
                    InitializeControlCallbacks();
                }
            }
        }

        /// <summary>
        /// The center of the menu component in menu system coordinates.
        /// </summary>
        /// This is a good place to center the menu view to when the menu component
        /// is to be seen well on the screen.
        public override Vector2 Center { get { return pos + new Vector2(560, 515); } }

        /// <summary>
        /// Creates an arena selection menu component for a menu system.
        /// </summary>
        /// <param name="menuEngine">The menu system.</param>
        public ArenaMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            pos = new Vector2(1220, 698);

            selectedArenaNames = new List<string>();
            selectedArenaNames.Add(ArenaInfos.First().Name);
            cursorFade = new Curve();
            cursorFade.Keys.Add(new CurveKey(0, 255, 0, 0, CurveContinuity.Step));
            cursorFade.Keys.Add(new CurveKey(0.5f, 0, 0, 0, CurveContinuity.Step));
            cursorFade.Keys.Add(new CurveKey(1, 255, 0, 0, CurveContinuity.Step));
            cursorFade.PreLoop = CurveLoopType.Cycle;
            cursorFade.PostLoop = CurveLoopType.Cycle;
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            var content = (AWContentManager)AssaultWing.Instance.Content;
            menuBigFont = content.Load<SpriteFont>("MenuFontBig");
            menuSmallFont = content.Load<SpriteFont>("MenuFontSmall");
            backgroundTexture = content.Load<Texture2D>("menu_levels_bg");
            cursorTexture = content.Load<Texture2D>("menu_levels_cursor");
            highlightTexture = content.Load<Texture2D>("menu_levels_hilite");
            tagTexture = content.Load<Texture2D>("menu_levels_tag");
            arenaPreviews = ArenaInfos.Select(info =>
            {
                var previewName = content.Exists<Texture2D>(info.PreviewName) ? info.PreviewName : "no_preview";
                return content.Load<Texture2D>(previewName);
            }).ToList();
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public override void UnloadContent()
        {
            // The textures and fonts we reference will be disposed by GraphicsEngine.
        }

        /// <summary>
        /// Updates the menu component.
        /// </summary>
        public override void Update()
        {
            // Check our controls and react to them.
            if (Active)
            {
                controlCallbacks.Update();

                // Limit cursor to sensible limits and scroll currently highlighted arena into view.
                currentArena = currentArena.Clamp(0, ArenaInfos.Count - 1);
                arenaListStart = arenaListStart.Clamp(currentArena - MENU_ITEM_COUNT + 1, currentArena);
            }
        }

        /// <summary>
        /// Draws the menu component.
        /// </summary>
        /// <param name="view">Top left corner of the menu view in menu system coordinates.</param>
        /// <param name="spriteBatch">The sprite batch to use. <c>Begin</c> is assumed
        /// to have been called and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(backgroundTexture, pos - view, Color.White);

            // Draw arena list.
            Vector2 lineDeltaPos = new Vector2(0, 40);
            Vector2 arenaNamePos = pos - view + new Vector2(147, 237);
            Vector2 arenaTagPos = pos - view + new Vector2(283, 235);
            for (int i = 0; i < MENU_ITEM_COUNT && arenaListStart + i < ArenaInfos.Count; ++i)
            {
                int arenaI = arenaListStart + i;
                spriteBatch.DrawString(menuSmallFont, ArenaInfos[arenaI].Name, arenaNamePos + i * lineDeltaPos, Color.White);
                if (selectedArenaNames.Contains(ArenaInfos[arenaI].Name))
                    spriteBatch.Draw(tagTexture, arenaTagPos + i * lineDeltaPos, Color.White);
            }

            // Draw condolences.
            if (ArenaInfos.Count == 0)
                spriteBatch.DrawString(menuBigFont, "No arenas, can't play, sorry!",
                    pos - view + new Vector2(540, 297), Color.White);

            // Draw cursor and highlight.
            Vector2 highlightPos = pos - view + new Vector2(124, 223) + (currentArena - arenaListStart) * lineDeltaPos;
            Vector2 cursorPos = highlightPos + new Vector2(2, 1);
            Vector2 arenaPreviewPos = pos-view + new Vector2(430, 232);
            float cursorTime = (float)(AssaultWing.Instance.GameTime.TotalRealTime - cursorFadeStartTime).TotalSeconds;
            spriteBatch.Draw(highlightTexture, highlightPos, Color.White);
            spriteBatch.Draw(cursorTexture, cursorPos, new Color(255, 255, 255, (byte)cursorFade.Evaluate(cursorTime)));
            spriteBatch.Draw(arenaPreviews[currentArena], arenaPreviewPos, Color.White);
        }

        private void InitializeControlCallbacks()
        {
            controlCallbacks = new TriggeredCallbackCollection();
            controlCallbacks.TriggeredCallback = () =>
            {
                cursorFadeStartTime = AssaultWing.Instance.GameTime.TotalRealTime;
            };
            controlCallbacks.Callbacks.Add(new TriggeredCallback(controlBack, () =>
            { 
                MenuEngine.ActivateComponent(MenuComponentType.Equip); 
            }));
           
            controlCallbacks.Callbacks.Add(new TriggeredCallback(controlDone, () =>
            {
                if (currentArena >= 0 && currentArena < ArenaInfos.Count)
                {
                    if (SelectCurrentArena())
                    {
                        AssaultWing.Instance.SoundEngine.PlaySound("MenuChangeItem");
                        MenuEngine.ActivateComponent(MenuComponentType.Equip);
                    }
                }
            }));
            
            controlCallbacks.Callbacks.Add(new TriggeredCallback(controlUp, () =>
            {
                --currentArena;
                AssaultWing.Instance.SoundEngine.PlaySound("MenuBrowseItem");
            }));
            controlCallbacks.Callbacks.Add(new TriggeredCallback(controlDown, () =>
            {
                ++currentArena;
                AssaultWing.Instance.SoundEngine.PlaySound("MenuBrowseItem");
            }));
        }

        private bool SelectCurrentArena()
        {
            if (currentArena >= 0 && currentArena < ArenaInfos.Count)
            {
                var name = ArenaInfos[currentArena].Name;
                if (!selectedArenaNames.Contains(name))
                {
                    selectedArenaNames.Clear();
                    selectedArenaNames.Add(name);
                    AssaultWing.Instance.DataEngine.ArenaPlaylist = new AW2.Helpers.Collections.Playlist(selectedArenaNames);
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Sets up the menu component's controls based on players' current control setup.
        /// </summary>
        private void InitializeControls()
        {
            if (controlDone != null) controlDone.Dispose();
            if (controlBack != null) controlBack.Dispose();
            if (controlUp != null) controlUp.Dispose();
            if (controlDown != null) controlDown.Dispose();

            controlDone = new KeyboardKey(Keys.Enter);
            controlBack = new KeyboardKey(Keys.Escape);
            controlUp = new MultiControl();
            controlUp.Add(new KeyboardKey(Keys.Up));
            controlDown = new MultiControl();
            controlDown.Add(new KeyboardKey(Keys.Down));

            foreach (var player in AssaultWing.Instance.DataEngine.Spectators)
            {
                controlUp.Add(player.Controls.Thrust);
                controlDown.Add(player.Controls.Down);
            }
        }
    }
}
