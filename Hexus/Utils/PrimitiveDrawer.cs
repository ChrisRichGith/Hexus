using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Hexus.Utils
{
    public static class PrimitiveDrawer
    {
        private static Texture2D _pixel;

        private static void EnsureInitialized(GraphicsDevice graphicsDevice)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(graphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
        }

        public static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness)
        {
            EnsureInitialized(spriteBatch.GraphicsDevice);
            var delta = end - start;
            spriteBatch.Draw(
                _pixel,
                start,
                null,
                color,
                (float)Math.Atan2(delta.Y, delta.X),
                Vector2.Zero,
                new Vector2(delta.Length(), thickness),
                SpriteEffects.None,
                0f
            );
        }

        public static void DrawHexOutline(SpriteBatch spriteBatch, Vector2 center, float size, Color color, int thickness)
        {
            Vector2[] corners = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = MathHelper.ToRadians(60 * i); // Pointy top
                corners[i] = new Vector2(
                    center.X + size * (float)Math.Cos(angle),
                    center.Y + size * (float)Math.Sin(angle)
                );
            }

            for (int i = 0; i < 6; i++)
            {
                DrawLine(spriteBatch, corners[i], corners[(i + 1) % 6], color, thickness);
            }
        }

        public static void DrawFilledRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            EnsureInitialized(spriteBatch.GraphicsDevice);
            spriteBatch.Draw(_pixel, rect, color);
        }
    }
}
