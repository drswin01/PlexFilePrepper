using Prepper.Abstractions;
using System;
using System.Collections.Generic;

namespace Prepper
{
    public class AddToPrepEventArgs : EventArgs
    {
        public List<IEpisode> Episodes { get; }

        public AddToPrepEventArgs(List<IEpisode> episodes)
        {
            Episodes = episodes;
        }
    }
}
