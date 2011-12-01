using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PikabluCS {
    internal static class Duration {
        private static readonly TimeSpan infinite = new TimeSpan(0, 0, 0, 0, -1);

        public static TimeSpan Infinite {
            get { return infinite; }
        }
    }
}
