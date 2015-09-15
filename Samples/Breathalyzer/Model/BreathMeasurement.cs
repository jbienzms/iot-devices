using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Breathalyzer
{
    public class BreathMeasurement
    {
        public string Alias { get; set; }

        public DateTime TimeStamp { get; set; }

        public double Value { get; set; }

    }
}
