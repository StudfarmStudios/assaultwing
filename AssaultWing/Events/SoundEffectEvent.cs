using System;
using System.Collections.Generic;
using System.Text;
using AW2.Sound;

namespace AW2.Events
{
    /// <summary>
    /// Event carrying a request for sample 
    /// </summary>
    class SoundEffectEvent : Event
    {
        private SoundOptions.Action action;
        private SoundOptions.Effect effect;

        /// <summary>
        /// Returns information about which sample should be played.
        /// </summary>
        /// <returns></returns>
        public SoundOptions.Action getAction()
        {
            return action;
        }

        /// <summary>
        /// Set which sample should be played.
        /// </summary>
        /// <param name="action">select the sound with ACTION enum</param>
        public void setAction(SoundOptions.Action action)
        {
            this.action = action;
        }

        /// <summary>
        /// Returns information about what post processing should be applied to the played sample
        /// </summary>
        /// <returns></returns>
        public SoundOptions.Effect getEffect()
        {
            return effect;
        }

        /// <summary>
        /// Sets what post processing should be applied to the played sample
        /// </summary>
        /// <param name="effect"></param>
        public void setEffect(SoundOptions.Effect effect)
        {
            this.effect = effect;
        }
    }
}
