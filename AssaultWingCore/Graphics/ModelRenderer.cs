using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Helpers;

namespace AW2.Graphics
{
    public static class ModelRenderer
    {
        private const string OUTLINE_MESH_NAME_PREFIX = "Outline";

        private static GraphicsDevice GraphicsDevice { get { return AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice; } }

        public static void DrawBorderedText(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 pos, Color baseColor, float alpha, float shadowThickness)
        {
            var textColor = Color.Multiply(baseColor, alpha);
            var shadowColor = Color.Multiply(Color.Black, 0.4f * alpha);
            spriteBatch.DrawString(font, text, pos + new Vector2(-shadowThickness, -shadowThickness), shadowColor);
            spriteBatch.DrawString(font, text, pos + new Vector2(shadowThickness, -shadowThickness), shadowColor);
            spriteBatch.DrawString(font, text, pos + new Vector2(-shadowThickness, shadowThickness), shadowColor);
            spriteBatch.DrawString(font, text, pos + new Vector2(shadowThickness, shadowThickness), shadowColor);
            spriteBatch.DrawString(font, text, pos, textColor);
        }

        public static void DrawChatLines(SpriteBatch spriteBatch, SpriteFont font, IEnumerable<WrappedTextList.Line> messageLines, Vector2 textPos)
        {
            foreach (var line in messageLines)
            {
                if (line.ContainsPretext)
                {
                    var splitIndex = line.Text.IndexOf('>');
                    if (splitIndex < 0) throw new ApplicationException("Pretext char not found");
                    var pretext = line.Text.Substring(0, splitIndex + 1);
                    var properText = line.Text.Substring(splitIndex + 1);
                    ModelRenderer.DrawBorderedText(spriteBatch, font, pretext, Vector2.Round(textPos), AW2.Game.PlayerMessage.PRETEXT_COLOR, 1, 1);
                    var properPos = Vector2.Round(textPos + new Vector2(font.MeasureString(pretext).X, 0));
                    ModelRenderer.DrawBorderedText(spriteBatch, font, properText, properPos, line.Color, 1, 1);
                }
                else
                    ModelRenderer.DrawBorderedText(spriteBatch, font, line.Text, Vector2.Round(textPos), line.Color, 1, 1);
                textPos.Y += font.LineSpacing;
            }
        }

        public static void Draw(Model model, Matrix world, Matrix view, Matrix projection, Matrix[] modelPartTransforms)
        {
            foreach (var mesh in model.Meshes)
            {
                if (mesh.Name.StartsWith(OUTLINE_MESH_NAME_PREFIX)) continue;
                foreach (BasicEffect be in mesh.Effects)
                {
                    be.Projection = projection;
                    be.View = view;
                    be.World = modelPartTransforms[mesh.ParentBone.Index] * world;
                }
                mesh.Draw();
            }
        }

        public static void DrawOutlineTransparent(Model model, Matrix world, Matrix view, Matrix projection, Matrix[] modelPartTransforms, float alpha)
        {
            EnableTransparency(model, alpha);
            foreach (var mesh in model.Meshes)
            {
                if (!mesh.Name.StartsWith(OUTLINE_MESH_NAME_PREFIX)) continue;
                foreach (BasicEffect be in mesh.Effects)
                {
                    be.Projection = projection;
                    be.View = view;
                    be.World = modelPartTransforms[mesh.ParentBone.Index] * world;
                }
                mesh.Draw();
            }
            DisableTransparency(model);
        }

        public static void DrawTransparent(Model model, Matrix world, Matrix view, Matrix projection, Matrix[] modelPartTransforms, float alpha)
        {
            EnableTransparency(model, alpha);
            Draw(model, world, view, projection, modelPartTransforms);
            DisableTransparency(model);
        }

        public static void DrawBleached(Model model, Matrix world, Matrix view, Matrix projection, Matrix[] modelPartTransforms, float bleach)
        {
            Draw(model, world, view, projection, modelPartTransforms);
            EnableBleach(model, bleach);
            Draw(model, world, view, projection, modelPartTransforms);
            DisableBleach(model);
        }

        private static void EnableTransparency(Model model, float alpha)
        {
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            foreach (var mesh in model.Meshes)
                foreach (BasicEffect be in mesh.Effects)
                    be.Alpha = alpha;
        }

        private static void DisableTransparency(Model model)
        {
            GraphicsDevice.BlendState = BlendState.Opaque;
            foreach (var mesh in model.Meshes)
                foreach (BasicEffect be in mesh.Effects)
                    be.Alpha = 1;
        }

        private static void EnableBleach(Model model, float bleach)
        {
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            foreach (var mesh in model.Meshes)
                foreach (BasicEffect be in mesh.Effects)
                {
                    be.LightingEnabled = false;
                    be.TextureEnabled = false;
                    be.Alpha = bleach;
                }
        }

        private static void DisableBleach(Model model)
        {
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            foreach (var mesh in model.Meshes)
                foreach (BasicEffect be in mesh.Effects)
                {
                    be.LightingEnabled = true;
                    be.TextureEnabled = true;
                    be.Alpha = 1;
                }
        }
    }
}
