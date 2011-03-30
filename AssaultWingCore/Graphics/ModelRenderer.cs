using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using AW2.Core;

namespace AW2.Graphics
{
    public static class ModelRenderer
    {
        private static GraphicsDevice GraphicsDevice { get { return AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice; } }

        public static void Draw(Model model, Matrix world, Matrix view, Matrix projection, Matrix[] modelPartTransforms)
        {
            foreach (var mesh in model.Meshes)
            {
                foreach (BasicEffect be in mesh.Effects)
                {
                    be.Projection = projection;
                    be.View = view;
                    be.World = modelPartTransforms[mesh.ParentBone.Index] * world;
                }
                mesh.Draw();
            }
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
