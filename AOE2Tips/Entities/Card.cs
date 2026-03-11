using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HPScreen.Admin;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HPScreen.Entities
{
    public class Card
    {
        // Layout constants
        private const int PADDING = 20;
        private const int LINE_SPACING = 8;
        private const int LABEL_VALUE_GAP = 12;
        private const int TITLE_BOTTOM_MARGIN = 16;
        private const int SPRITE_RIGHT_MARGIN = 20;

        public string Title { get; set; }
        public List<KeyValuePair<string, string>> Fields { get; set; }
        public string SpriteName { get; set; }
        public string TipType { get; set; }

        // Position and size
        public float X { get; set; }
        public float Y { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        // Fonts for rendering
        private Font _titleFont;
        private Font _labelFont;
        private Font _valueFont;

        // Optional colorizer for value quality colors
        public ValueColorizer Colorizer { get; set; }

        public Card(string title, List<KeyValuePair<string, string>> fields, string spriteName = null, string tipType = null)
        {
            Title = title;
            Fields = fields ?? new List<KeyValuePair<string, string>>();
            SpriteName = spriteName;
            TipType = tipType;

            Color highlight = GetHighlightColor();
            _titleFont = new Font(highlight, Font.Type.arial, Font.Size.SIZE_M, shadow: true);
            _labelFont = new Font(Color.LightGray, Font.Type.arial, Font.Size.SIZE_S);
            _valueFont = new Font(Color.White, Font.Type.arial, Font.Size.SIZE_S);
        }

        private Color GetHighlightColor()
        {
            switch (TipType?.ToLowerInvariant())
            {
                case "unit": return Color.Red;
                case "unique unit": return Color.Magenta;
                case "technology": return Color.White;
                case "building": return Color.Gray;
                default: return Color.Gold;
            }
        }

        /// <summary>
        /// Measures and caches the card dimensions based on current content and fonts.
        /// Call this after construction or if content changes.
        /// </summary>
        public void MeasureLayout()
        {
            var gfx = Graphics.Current;

            int contentWidth = 0;
            int contentHeight = 0;

            // Title height
            int titleHeight = gfx.StringHeight(Title, _titleFont);
            contentHeight += titleHeight + TITLE_BOTTOM_MARGIN;

            // Measure each field row
            int maxLabelWidth = 0;
            int maxValueWidth = 0;
            int rowHeight = 0;

            foreach (var field in Fields)
            {
                int lw = gfx.StringWidth(field.Key, _labelFont);
                int vw = gfx.StringWidth(field.Value, _valueFont);
                int rh = Math.Max(gfx.StringHeight(field.Key, _labelFont),
                                  gfx.StringHeight(field.Value, _valueFont));

                if (lw > maxLabelWidth) maxLabelWidth = lw;
                if (vw > maxValueWidth) maxValueWidth = vw;
                if (rh > rowHeight) rowHeight = rh;
            }

            int fieldsWidth = maxLabelWidth + LABEL_VALUE_GAP + maxValueWidth;
            contentHeight += Fields.Count * (rowHeight + LINE_SPACING);

            // Account for sprite on the right if present
            int spriteSpace = 0;
            if (!string.IsNullOrEmpty(SpriteName) && gfx.SpritesByName.ContainsKey(SpriteName))
            {
                var tex = gfx.SpritesByName[SpriteName];
                spriteSpace = tex.Width + SPRITE_RIGHT_MARGIN;
                int spriteContentHeight = tex.Height + PADDING * 2;
                if (spriteContentHeight > contentHeight + PADDING * 2)
                    contentHeight = spriteContentHeight - PADDING * 2;
            }

            contentWidth = Math.Max(gfx.StringWidth(Title, _titleFont), fieldsWidth + spriteSpace);

            Width = contentWidth + PADDING * 2;
            Height = contentHeight + PADDING * 2;
        }

        /// <summary>
        /// Draws the card at its current (X, Y) position.
        /// SpriteBatch.Begin() must already be called before invoking this.
        /// </summary>
        public void Draw()
        {
            var gfx = Graphics.Current;
            int x = (int)X;
            int y = (int)Y;

            // Draw card background
            DrawFilledRect(x, y, Width, Height, new Color(20, 20, 20, 220));
            // Draw border
            Color highlight = GetHighlightColor();
            DrawBorder(x, y, Width, Height, highlight, 2);

            int cursorX = x + PADDING;
            int cursorY = y + PADDING;

            // Determine sprite area if present
            int spriteAreaWidth = 0;
            Texture2D spriteTex = null;
            if (!string.IsNullOrEmpty(SpriteName) && gfx.SpritesByName.ContainsKey(SpriteName))
            {
                spriteTex = gfx.SpritesByName[SpriteName];
                spriteAreaWidth = spriteTex.Width + SPRITE_RIGHT_MARGIN;
            }

            // Draw sprite on the right side if available
            if (spriteTex != null)
            {
                int spriteX = x + Width - PADDING - spriteTex.Width;
                int spriteY = y + PADDING;
                gfx.SpriteB.Draw(spriteTex, new Rectangle(spriteX, spriteY, spriteTex.Width, spriteTex.Height), Color.White);
            }

            // Draw title
            int titleHeight = gfx.StringHeight(Title, _titleFont);
            gfx.DrawString(Title, new Vector2(cursorX, cursorY), _titleFont);
            cursorY += titleHeight + TITLE_BOTTOM_MARGIN;

            // Draw separator line under title
            DrawFilledRect(cursorX, cursorY - TITLE_BOTTOM_MARGIN / 2, Width - PADDING * 2 - spriteAreaWidth, 2, highlight);

            // Draw fields as label: value pairs
            int maxLabelWidth = 0;
            foreach (var field in Fields)
            {
                int lw = gfx.StringWidth(field.Key + ":", _labelFont);
                if (lw > maxLabelWidth) maxLabelWidth = lw;
            }

            foreach (var field in Fields)
            {
                int rowHeight = Math.Max(gfx.StringHeight(field.Key, _labelFont),
                                         gfx.StringHeight(field.Value, _valueFont));

                // Label (left-aligned)
                gfx.DrawString(field.Key + ":", new Vector2(cursorX, cursorY), _labelFont);

                // Value (offset by max label width + gap), colorized if thresholds exist
                int valueX = cursorX + maxLabelWidth + LABEL_VALUE_GAP;
                Color? valueColor = Colorizer?.GetColor(field.Key, field.Value);
                gfx.DrawString(field.Value, new Vector2(valueX, cursorY), _valueFont, overrideColor: valueColor);

                cursorY += rowHeight + LINE_SPACING;
            }
        }

        #region Drawing Helpers

        private void DrawFilledRect(int x, int y, int width, int height, Color color)
        {
            var gfx = Graphics.Current;
            Texture2D pixel = new Texture2D(gfx.Device, 1, 1);
            pixel.SetData(new[] { Color.White });
            gfx.SpriteB.Draw(pixel, new Rectangle(x, y, width, height), color);
        }

        private void DrawBorder(int x, int y, int width, int height, Color color, int thickness)
        {
            DrawFilledRect(x, y, width, thickness, color);                          // Top
            DrawFilledRect(x, y + height - thickness, width, thickness, color);     // Bottom
            DrawFilledRect(x, y, thickness, height, color);                         // Left
            DrawFilledRect(x + width - thickness, y, thickness, height, color);     // Right
        }

        #endregion

        #region Static Loading

        /// <summary>
        /// Loads all tips from a text file and returns a list of Cards.
        /// Expected format per tip:
        ///   [TIP]
        ///   Title=Some Title
        ///   Key=Value
        ///   Sprite=optional_sprite_name
        ///   [END]
        /// </summary>
        public static List<Card> LoadFromFile(string filePath)
        {
            var cards = new List<Card>();
            var lines = File.ReadAllLines(filePath);

            string title = null;
            var fields = new List<KeyValuePair<string, string>>();
            string spriteName = null;
            string tipType = null;
            bool insideTip = false;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line == "[TIP]")
                {
                    insideTip = true;
                    title = null;
                    fields = new List<KeyValuePair<string, string>>();
                    spriteName = null;
                    tipType = null;
                    continue;
                }

                if (line == "[END]")
                {
                    if (insideTip && title != null)
                    {
                        cards.Add(new Card(title, fields, spriteName, tipType));
                    }
                    insideTip = false;
                    continue;
                }

                if (!insideTip || string.IsNullOrEmpty(line))
                    continue;

                int eqIndex = line.IndexOf('=');
                if (eqIndex < 0)
                    continue;

                string key = line.Substring(0, eqIndex).Trim();
                string value = line.Substring(eqIndex + 1).Trim();

                if (key.Equals("Title", StringComparison.OrdinalIgnoreCase))
                {
                    title = value;
                }
                else if (key.Equals("Sprite", StringComparison.OrdinalIgnoreCase))
                {
                    spriteName = string.IsNullOrEmpty(value) ? null : value;
                }
                else if (key.Equals("Type", StringComparison.OrdinalIgnoreCase))
                {
                    tipType = string.IsNullOrEmpty(value) ? null : value;
                }
                else
                {
                    fields.Add(new KeyValuePair<string, string>(key, value));
                }
            }

            return cards;
        }

        #endregion
    }
}
