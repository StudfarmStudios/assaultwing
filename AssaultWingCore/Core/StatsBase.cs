using System;

namespace AW2.Core
{
    /// <summary>
    /// A dummy implementation of gathering statistics. Game clients use this implementation.
    /// </summary>
    public class StatsBase : AWGameComponent
    {
        /// <summary>
        /// Is basic arena and server information sent since the last arena change
        /// or the last reconnection to the stats server.
        /// </summary>
        public bool BasicInfoSent { get; set; }

        public StatsBase(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        public virtual void Send(object obj) { }
    }
}
