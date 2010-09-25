using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game
{
    /// <summary>
    /// An arena layer.
    /// </summary>
    /// Arena layers are a means to visualise depth in spite of the
    /// orthogonal projection of 3D graphics.
    [LimitedSerialization]
    [System.Diagnostics.DebuggerDisplay("Z:{Z} Gobs:{Gobs.Count} IsGameplay:{isGameplayLayer} Parallax:{parallaxName}")]
    public class ArenaLayer : IConsistencyCheckable
    {
        [TypeParameter]
        private bool isGameplayLayer;
        [TypeParameter]
        private float z;
        [TypeParameter]
        private CanonicalString parallaxName;

        // This field will be serialised so that the gobs have their runtime state
        // (positions, movements, etc.) serialised and not their type parameters.
        [TypeParameter]
        [LimitationSwitch(typeof(TypeParameterAttribute), typeof(RuntimeStateAttribute))]
        private ArenaLayerGobCollection gobs;

        /// <summary>
        /// The gobs on this arena layer.
        /// </summary>
        public ArenaLayerGobCollection Gobs { get { return gobs; } }

        /// <summary>
        /// Is this the arena layer where gameplay takes place.
        /// </summary>
        /// It is assumed that only one layer in each arena is the gameplay layer.
        /// If several layers claim to be gameplay layers, any one of them can be
        /// considered as <i>the</i> gameplay layer.
        public bool IsGameplayLayer { get { return isGameplayLayer; } }

        /// <summary>
        /// Z coordinate of the layer.
        /// </summary>
        /// The Z coordinate of the gameplay layer is 0. Negative coordinates
        /// are farther away from the camera.
        public float Z { get { return z; } }

        /// <summary>
        /// Name of the texture to use as parallax or the empty string for no parallax.
        /// </summary>
        public CanonicalString ParallaxName { get { return parallaxName; } }

        /// <summary>
        /// Creates an uninitialised arena layer.
        /// </summary>
        /// This constructor is only for serialisation.
        public ArenaLayer()
        {
            isGameplayLayer = true;
            z = 0;
            parallaxName = (CanonicalString)"dummysprite";
            gobs = new ArenaLayerGobCollection();
        }

        /// <summary>
        /// Creates an arena layer.
        /// </summary>
        /// <param name="isGameplayLayer">Is the layer the gameplay layer.</param>
        /// <param name="z">Depth of the layer.</param>
        /// <param name="parallaxName">The name of the layer's parallax, or <c>null</c>.</param>
        public ArenaLayer(bool isGameplayLayer, float z, string parallaxName)
        {
            this.isGameplayLayer = isGameplayLayer;
            this.z = z;
            this.parallaxName = (CanonicalString)parallaxName;
            gobs = new ArenaLayerGobCollection();
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
        ArenaLayer(ArenaLayer other)
        {
            isGameplayLayer = other.isGameplayLayer;
            z = other.z;
            parallaxName = other.parallaxName;
            gobs = new ArenaLayerGobCollection();
        }

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public void MakeConsistent(Type limitationAttribute)
        {
            if (z >= 1000)
            {
                Log.Write("Warning: Clamping too big arena layer Z coordinate: " + z);
                z = 500;
            }
        }

        #endregion
    }
}
