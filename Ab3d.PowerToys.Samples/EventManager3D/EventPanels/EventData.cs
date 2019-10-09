using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ab3d.PowerToys.Samples.EventManager3D.EventPanels
{
    /// <summary>
    /// Event data for displaying list of all triggered events
    /// </summary>
    public class EventData
    {
        public string Time { get; private set; }
        public string Name { get; private set; }
        public EventArgs Args { get; private set; }

        public EventData(string name, EventArgs args)
        {
            this.Name = name;
            this.Args = args;

            this.Time = DateTime.Now.ToString("HH:mm:ss.f");
        }
    }
}
