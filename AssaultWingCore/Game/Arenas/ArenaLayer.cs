using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Arenas
{
    /// <summary>
    /// Arena layers are a means to visualise depth in spite of the
    /// orthogonal projection of 3D graphics.
    /// </summary>
    [LimitedSerialization]
    [System.Diagnostics.DebuggerDisplay("Z:{Z} Gobs:{Gobs._gobs.Count} IsGameplay:{_isGameplayLayer} Parallax:{_parallaxName}")]
    public class ArenaLayer
    {
        [TypeParameter]
        private bool _isGameplayLayer;
        [TypeParameter]
        private float _z;
        [TypeParameter]
        private CanonicalString _parallaxName;

        // This field will be serialised so that the gobs have their runtime state
        // (positions, movements, etc.) serialised and not their type parameters.
        [TypeParameter]
        [LimitationSwitch(typeof(TypeParameterAttribute), typeof(RuntimeStateAttribute))]
        private ArenaLayerGobCollection _gobs;

        /// <summary>
        /// The gobs on this arena layer.
        /// </summary>
        public ArenaLayerGobCollection Gobs { get { return _gobs; } }

        /// <summary>
        /// Is this the arena layer where gameplay takes place.
        /// </summary>
        /// It is assumed that only one layer in each arena is the gameplay layer.
        /// If several layers claim to be gameplay layers, any one of them can be
        /// considered as <i>the</i> gameplay layer.
        public bool IsGameplayLayer { get { return _isGameplayLayer; } }

        /// <summary>
        /// Z coordinate of the layer.
        /// </summary>
        /// The Z coordinate of the gameplay layer is 0. Negative coordinates
        /// are farther away from the camera.
        public float Z { get { return _z; } }

        /// <summary>
        /// Name of the texture to use as parallax or the empty string for no parallax.
        /// </summary>
        public CanonicalString ParallaxName { get { return _parallaxName; } }

        /// <summary>
        /// Creates an uninitialised arena layer.
        /// </summary>
        /// This constructor is only for serialisation.
        public ArenaLayer()
        {
            _isGameplayLayer = true;
            _z = 0;
            _parallaxName = (CanonicalString)"dummysprite";
            _gobs = new ArenaLayerGobCollection();
        }

        /// <summary>
        /// Creates an arena layer.
        /// </summary>
        /// <param name="isGameplayLayer">Is the layer the gameplay layer.</param>
        /// <param name="z">Depth of the layer.</param>
        /// <param name="parallaxName">The name of the layer's parallax, or <c>null</c>.</param>
        public ArenaLayer(bool isGameplayLayer, float z, string parallaxName)
        {
            _isGameplayLayer = isGameplayLayer;
            _z = z;
            _parallaxName = (CanonicalString)parallaxName;
            _gobs = new ArenaLayerGobCollection();
        }

        /// <summary>
        /// Returns a new arena layer with the same specifications but no gobs.
        /// </summary>
        /// <returns>A duplicate arena layer without gobs.</returns>
        public ArenaLayer EmptyCopy()
        {
            return new ArenaLayer(this);
        }

        /// <summary>
        /// Creates a copy of an arena layer excluding its gobs.
        /// </summary>
        private ArenaLayer(ArenaLayer other)
        {
            _isGameplayLayer = other._isGameplayLayer;
            _z = other._z;
            _parallaxName = other._parallaxName;
            _gobs = new ArenaLayerGobCollection();
        }   
    }
}
