using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HPScreen.Admin;
using HPScreen.Entities;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct2D1;
using SharpDX.Direct3D9;

namespace HPScreen
{
    public class ScreenSaver : Game
    {
        private GraphicsDeviceManager _graphics;
        private Microsoft.Xna.Framework.Graphics.SpriteBatch _spriteBatch;
        private KeyboardState _currentKeyboardState;
        private KeyboardState _previousKeyboardState;
        private MouseState _currentMouseState;
        private MouseState _previousMouseState;
        private int loadFrames = 0;
        private const int LOAD_FRAMES_THRESH = 10;

        // Tip cycling
        private List<Card> _allCards;
        private HashSet<int> _shownIndices;
        private Card _currentCard;
        private double _tipTimer;
        private const double TIP_DISPLAY_SECONDS = 8.0;
        private ValueColorizer _colorizer;

        protected bool RunSetup { get; set; }
        public ScreenSaver()
        {
            Graphics.Current.GraphicsDM = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;

            // Set the following property to false to ensure alternating Step() and Draw() functions
            // Set the property to true to (hopefully) improve game smoothness by ignoring some draw calls if needed.
            IsFixedTimeStep = false;

            RunSetup = true;
        }
        protected override void Initialize()
        {
            // Note: This takes places BEFORE LoadContent()
            Graphics.Current.Init(this.GraphicsDevice, this.Window, true);
            base.Initialize();
        }
        protected override void LoadContent()
        {
            Graphics.Current.SpriteB = new Microsoft.Xna.Framework.Graphics.SpriteBatch(Graphics.Current.GraphicsDM.GraphicsDevice);

            // Sprites
            Graphics.Current.SpritesByName.Add("arambai", Content.Load<Texture2D>("Sprites/arambai"));
            Graphics.Current.SpritesByName.Add("ballista_elephant", Content.Load<Texture2D>("Sprites/ballista_elephant"));
            Graphics.Current.SpritesByName.Add("berserk", Content.Load<Texture2D>("Sprites/berserk"));
            Graphics.Current.SpritesByName.Add("boyar", Content.Load<Texture2D>("Sprites/boyar"));
            Graphics.Current.SpritesByName.Add("camel_archer", Content.Load<Texture2D>("Sprites/camel_archer"));
            Graphics.Current.SpritesByName.Add("caravel", Content.Load<Texture2D>("Sprites/caravel"));
            Graphics.Current.SpritesByName.Add("cataphract", Content.Load<Texture2D>("Sprites/cataphract"));
            Graphics.Current.SpritesByName.Add("centurion", Content.Load<Texture2D>("Sprites/centurion"));
            Graphics.Current.SpritesByName.Add("chakram_thrower", Content.Load<Texture2D>("Sprites/chakram_thrower"));
            Graphics.Current.SpritesByName.Add("chu_ko_nu", Content.Load<Texture2D>("Sprites/chu_ko_nu"));
            Graphics.Current.SpritesByName.Add("composite_bowman", Content.Load<Texture2D>("Sprites/composite_bowman"));
            Graphics.Current.SpritesByName.Add("condottiero", Content.Load<Texture2D>("Sprites/condottiero"));
            Graphics.Current.SpritesByName.Add("genoese_crossbowman", Content.Load<Texture2D>("Sprites/genoese_crossbowman"));

            Graphics.Current.Fonts = new Dictionary<string, SpriteFont>();
            Graphics.Current.Fonts.Add("arial-48", Content.Load<SpriteFont>($"Fonts/arial_48"));
            Graphics.Current.Fonts.Add("arial-72", Content.Load<SpriteFont>($"Fonts/arial_72"));
            Graphics.Current.Fonts.Add("arial-96", Content.Load<SpriteFont>($"Fonts/arial_96"));
            Graphics.Current.Fonts.Add("arial-144", Content.Load<SpriteFont>($"Fonts/arial_144"));
        }
        protected override void Update(GameTime gameTime)
        {
            CheckInput(); // Used to exit game when input detected (aka screensaver logic)
            if (RunSetup) { Setup(); }

            // Cycle tips on a timer
            if (_allCards != null && _allCards.Count > 0)
            {
                _tipTimer += gameTime.ElapsedGameTime.TotalSeconds;
                if (_tipTimer >= TIP_DISPLAY_SECONDS)
                {
                    PickNextCard();
                }
            }

            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            // Color that the screen is wiped with each frame before drawing anything else:
            GraphicsDevice.Clear(Color.Black);

            DrawBackground();

            // Draw the current tip card centered on screen
            if (_currentCard != null)
            {
                Graphics.Current.SpriteB.Begin();
                _currentCard.Draw();
                Graphics.Current.SpriteB.End();
            }

            base.Draw(gameTime);
        }

        protected void Setup()
        {
            // Load tips and color thresholds
            string tipsPath = Path.Combine(Content.RootDirectory, "tips.txt");
            string thresholdsPath = Path.Combine(Content.RootDirectory, "thresholds.txt");
            _allCards = Card.LoadFromFile(tipsPath);
            _colorizer = ValueColorizer.LoadFromFile(thresholdsPath);
            _shownIndices = new HashSet<int>();

            // Assign colorizer and measure all cards now that fonts are loaded
            foreach (var card in _allCards)
            {
                card.Colorizer = _colorizer;
                card.MeasureLayout();
            }

            // Debug: restrict tip pool to only units with sprites loaded
            //ApplyDebugFilter();

            // Show the first random tip
            PickNextCard();

            RunSetup = false;
        }

        protected void DrawBackground()
        {
            // Draw your background image here if you want one:

            //Rectangle destinationRectangle = new Rectangle(0, 0, Graphics.Current.ScreenWidth, Graphics.Current.ScreenHeight);
            //Graphics.Current.SpriteB.Begin();
            //Graphics.Current.SpriteB.Draw(
            //    Graphics.Current.SpritesByName["sprite_name"],  // Sprite: (texture2d)
            //    destinationRectangle,
            //    Color.White
            //);
            //Graphics.Current.SpriteB.End();
        }
        private void PickNextCard()
        {
            // Reset shown set if all have been displayed
            if (_shownIndices.Count >= _allCards.Count)
            {
                _shownIndices.Clear();
            }

            // Build list of unseen indices
            var available = new List<int>();
            for (int i = 0; i < _allCards.Count; i++)
            {
                if (!_shownIndices.Contains(i))
                    available.Add(i);
            }

            // Pick a random unseen tip
            int pick = available[Ran.Current.Next(0, available.Count - 1)];
            _shownIndices.Add(pick);
            _currentCard = _allCards[pick];

            // Center the card on screen
            _currentCard.X = (Graphics.Current.ScreenWidth - _currentCard.Width) / 2f;
            _currentCard.Y = (Graphics.Current.ScreenHeight - _currentCard.Height) / 2f;

            // Reset timer
            _tipTimer = 0;
        }

        /// <summary>
        /// Restricts the tip pool to only cards whose SpriteName is in the debug list.
        /// Comment out the call to this method in Setup() to restore the full pool.
        /// </summary>
        private void ApplyDebugFilter()
        {
            var debugSpriteNames = new HashSet<string>
            {
                "arambai",
                "ballista_elephant",
                "berserk",
                "boyar",
                "camel_archer",
                "caravel",
                "cataphract",
                "centurion",
                "chakram_thrower",
                "chu_ko_nu",
                "composite_bowman",
                "condottiero",
                "genoese_crossbowman"
            };

            _allCards = _allCards
                .Where(c => !string.IsNullOrEmpty(c.SpriteName) && debugSpriteNames.Contains(c.SpriteName))
                .ToList();
        }

        protected void CheckInput()
        {
            loadFrames++;
            _previousKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();
            _previousMouseState = _currentMouseState;
            _currentMouseState = Mouse.GetState();

            if (loadFrames >= LOAD_FRAMES_THRESH)
            {
                // Check if any key was pressed
                if (_currentKeyboardState.GetPressedKeys().Length > 0 && _previousKeyboardState.GetPressedKeys().Length == 0)
                {
                    Exit();
                }

                // Check if the mouse has moved
                Vector2 currentPos = new Vector2(_currentMouseState.Position.X, _currentMouseState.Position.Y);
                Vector2 previousPos = new Vector2(_previousMouseState.Position.X, _previousMouseState.Position.Y);
                if (Global.ApproxDist(currentPos, previousPos) >= 1)
                {
                    Exit();
                }
            }
        }
    }
}