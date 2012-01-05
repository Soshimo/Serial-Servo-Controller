using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace SerialServoController
{
    public class BuddyPair
    {
        private TrackBar _buddy1;
        private NumericUpDown _buddy2;
        private int _key;
        public BuddyPair(int key, TrackBar buddy1, NumericUpDown buddy2, int min, int max)
        {
            _key = key;

            _buddy1 = buddy1;

            _buddy2 = buddy2;

            _buddy2.Minimum = _buddy1.Minimum = min;
            _buddy2.Maximum = _buddy1.Maximum = max;
        }

        public int Key
        {
            get { return _key;  }
        }

        public int Value
        {
            get { return _buddy1.Value;  }
        }

    
        public void UpdateFromTrackbar()
        {
            _buddy2.Value = _buddy1.Value;
        }

        public void UpdateFromNumeric()
        {
            _buddy1.Value = (int)_buddy2.Value;
        }
    }
}
