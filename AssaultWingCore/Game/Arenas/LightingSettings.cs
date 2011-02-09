using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers.Serialization;

namespace AW2.Game.Arenas
{
    public class LightingSettings : IConsistencyCheckable
    {
        private Vector3 _light0DiffuseColor;
        private Vector3 _light0Direction;
        private bool _light0Enabled;
        private Vector3 _light0SpecularColor;
        private Vector3 _light1DiffuseColor;
        private Vector3 _light1Direction;
        private bool _light1Enabled;
        private Vector3 _light1SpecularColor;
        private Vector3 _light2DiffuseColor;
        private Vector3 _light2Direction;
        private bool _light2Enabled;
        private Vector3 _light2SpecularColor;
        private Vector3 _fogColor;
        private bool _fogEnabled;
        private float _fogEnd;
        private float _fogStart;

        public Vector3 Light0DiffuseColor { get { return _light0DiffuseColor; } set { _light0DiffuseColor = value; } }
        public Vector3 Light0Direction { get { return _light0Direction; } set { _light0Direction = value; } }
        public bool Light0Enabled { get { return _light0Enabled; } set { _light0Enabled = value; } }
        public Vector3 Light0SpecularColor { get { return _light0SpecularColor; } set { _light0SpecularColor = value; } }
        public Vector3 Light1DiffuseColor { get { return _light1DiffuseColor; } set { _light1DiffuseColor = value; } }
        public Vector3 Light1Direction { get { return _light1Direction; } set { _light1Direction = value; } }
        public bool Light1Enabled { get { return _light1Enabled; } set { _light1Enabled = value; } }
        public Vector3 Light1SpecularColor { get { return _light1SpecularColor; } set { _light1SpecularColor = value; } }
        public Vector3 Light2DiffuseColor { get { return _light2DiffuseColor; } set { _light2DiffuseColor = value; } }
        public Vector3 Light2Direction { get { return _light2Direction; } set { _light2Direction = value; } }
        public bool Light2Enabled { get { return _light2Enabled; } set { _light2Enabled = value; } }
        public Vector3 Light2SpecularColor { get { return _light2SpecularColor; } set { _light2SpecularColor = value; } }
        public Vector3 FogColor { get { return _fogColor; } set { _fogColor = value; } }
        public bool FogEnabled { get { return _fogEnabled; } set { _fogEnabled = value; } }
        public float FogEnd { get { return _fogEnd; } set { _fogEnd = value; } }
        public float FogStart { get { return _fogStart; } set { _fogStart = value; } }

        public LightingSettings()
        {
            Light0DiffuseColor = Vector3.Zero;
            Light0Direction = -Vector3.UnitZ;
            Light0Enabled = true;
            Light0SpecularColor = Vector3.Zero;
            Light1DiffuseColor = Vector3.Zero;
            Light1Direction = -Vector3.UnitZ;
            Light1Enabled = false;
            Light1SpecularColor = Vector3.Zero;
            Light2DiffuseColor = Vector3.Zero;
            Light2Direction = -Vector3.UnitZ;
            Light2Enabled = false;
            Light2SpecularColor = Vector3.Zero;
            FogColor = Vector3.Zero;
            FogEnabled = false;
            FogEnd = 1.0f;
            FogStart = 0.0f;
        }

        public void PrepareEffect(BasicEffect effect)
        {
            effect.DirectionalLight0.DiffuseColor = Light0DiffuseColor;
            effect.DirectionalLight0.Direction = Light0Direction;
            effect.DirectionalLight0.Enabled = Light0Enabled;
            effect.DirectionalLight0.SpecularColor = Light0SpecularColor;
            effect.DirectionalLight1.DiffuseColor = Light1DiffuseColor;
            effect.DirectionalLight1.Direction = Light1Direction;
            effect.DirectionalLight1.Enabled = Light1Enabled;
            effect.DirectionalLight1.SpecularColor = Light1SpecularColor;
            effect.DirectionalLight2.DiffuseColor = Light2DiffuseColor;
            effect.DirectionalLight2.Direction = Light2Direction;
            effect.DirectionalLight2.Enabled = Light2Enabled;
            effect.DirectionalLight2.SpecularColor = Light2SpecularColor;
            effect.FogColor = FogColor;
            effect.FogEnabled = FogEnabled;
            effect.FogEnd = FogEnd;
            effect.FogStart = FogStart;
        }

        public void MakeConsistent(Type limitationAttribute)
        {
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                Light0DiffuseColor = Vector3.Clamp(Light0DiffuseColor, Vector3.Zero, Vector3.One);
                Light0Direction = Vector3.Normalize(Light0Direction);
                Light0SpecularColor = Vector3.Clamp(Light0SpecularColor, Vector3.Zero, Vector3.One);
                Light1DiffuseColor = Vector3.Clamp(Light1DiffuseColor, Vector3.Zero, Vector3.One);
                Light1Direction = Vector3.Normalize(Light1Direction);
                Light1SpecularColor = Vector3.Clamp(Light1SpecularColor, Vector3.Zero, Vector3.One);
                Light2DiffuseColor = Vector3.Clamp(Light2DiffuseColor, Vector3.Zero, Vector3.One);
                Light2Direction = Vector3.Normalize(Light2Direction);
                Light2SpecularColor = Vector3.Clamp(Light2SpecularColor, Vector3.Zero, Vector3.One);
                FogColor = Vector3.Clamp(FogColor, Vector3.Zero, Vector3.One);
                FogEnd = MathHelper.Max(FogEnd, 0);
                FogStart = MathHelper.Max(FogStart, 0);
            }
        }
    }
}
