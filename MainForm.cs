using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace SerialServoController
{
    public partial class MainForm : Form
    {
        Dictionary<int, BuddyPair> _buddyPairs = new Dictionary<int, BuddyPair>();

        public enum State { Disconnected, Connecting, Connected, Version, Error };

        private State _state = State.Disconnected;
        private string _port = string.Empty;

        public MainForm()
        {
            InitializeComponent();

            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

            string [] ports = SerialPort.GetPortNames();
            foreach (string port in ports) 
            {
                fileSelectCommPortMenuItem.DropDownItems.Add(port);
            }

            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);

            LinkBuddies(trackBar1, numericUpDown1, 0);
            LinkBuddies(trackBar2, numericUpDown2, 1);
            LinkBuddies(trackBar3, numericUpDown3, 2);
            LinkBuddies(trackBar4, numericUpDown4, 3);
            LinkBuddies(trackBar5, numericUpDown5, 4);
            LinkBuddies(trackBar6, numericUpDown6, 5);
            LinkBuddies(trackBar7, numericUpDown7, 6);
            LinkBuddies(trackBar8, numericUpDown8, 7);
            LinkBuddies(trackBar9, numericUpDown9, 8);
            LinkBuddies(trackBar10, numericUpDown10, 9);
            LinkBuddies(trackBar11, numericUpDown11, 10);
            LinkBuddies(trackBar12, numericUpDown12, 11);

            UpdateStatus();
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => { WriteAllServos(); }));
            }
            else
            {
                WriteAllServos();
            }
        }

        void WriteAllServos()
        {
            foreach (BuddyPair bp in _buddyPairs.Values)
            {
                WriteServoPosition((byte)bp.Key, (byte)bp.Value);
            }
        }


        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();

                _state = State.Disconnected;
                UpdateStatus();
            }

            _port = e.Argument.ToString();
            serialPort1.PortName = _port;

            _state = State.Connecting;
            UpdateStatus();

            try
            {
                serialPort1.Open();
                _state = State.Connected;
            }
            catch (System.IO.IOException ioex)
            {
                _state = State.Error;
            }

            UpdateStatus();

        }

        private void LinkBuddies(TrackBar trackBar, NumericUpDown numericUpDown, int channel)
        {
            trackBar.Tag = channel.ToString();
            numericUpDown.Tag = channel.ToString();

            trackBar.ValueChanged += new EventHandler(trackBar_ValueChanged);
            numericUpDown.ValueChanged += new EventHandler(numericUpDown_ValueChanged);

            BuddyPair bp = new BuddyPair(channel, trackBar, numericUpDown, 0, 180);
            _buddyPairs.Add(channel, bp);

            // synchronize
            bp.UpdateFromTrackbar();
        }

        private void UpdateStatus()
        {
            switch(_state)
            {
                case State.Disconnected:
                    toolStripStateText.Text = "Disconnected";
                    break;

                case State.Connecting:
                    toolStripStateText.Text = "Connecting to " + _port;
                    break;

                case State.Error:
                    toolStripStateText.Text = "Error";
                    break;

                case State.Connected:
                    toolStripStateText.Text = "Connected on " + _port;
                    break;
            }
        }

        void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bufferSize = serialPort1.BytesToRead;
            byte[] buffer = new byte[bufferSize];

            serialPort1.Read(buffer, 0, serialPort1.BytesToRead);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < buffer.Length; i++)
            {
                builder.Append((char)buffer[i]);
            }

            Console.Write(builder.ToString());
        }

        private void WriteServoPosition(byte channel, byte angle)
        {
            if (serialPort1.IsOpen)
            {
                byte[] buffer = new byte[9];

                buffer[0] = (byte)'!';
                buffer[1] = (byte)'A';
                buffer[2] = (byte)'T';
                buffer[3] = (byte)'T';
                buffer[4] = (byte)'S';
                buffer[5] = (byte)'C';
                buffer[6] = (byte)'W';
                buffer[7] = channel;
                buffer[8] = angle;

                serialPort1.Write(buffer, 0, 9);
            }
        }

        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            if (sender is TrackBar) 
            {
                TrackBar tb = sender as TrackBar;

                int channel;
                if (int.TryParse(tb.Tag.ToString(), out channel))
                {

                    if (_buddyPairs.ContainsKey(channel))
                    {
                        // angle is value
                        int angle = tb.Value;

                        // find "buddy" control
                        // and update it's value
                        _buddyPairs[channel].UpdateFromTrackbar();

                        // finally update servo
                        WriteServoPosition((byte)channel, (byte)angle);
                    }
                }
            }
        }

        private void fileSelectCommPortMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            PopulateMenu();
        }

        private void PopulateMenu()
        {
            fileSelectCommPortMenuItem.DropDownItems.Clear();

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(port);
                fileSelectCommPortMenuItem.DropDownItems.Add(item);

                if (port == _port)
                {
                    item.Checked = true;
                }
            }
        }

        private void numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (sender is NumericUpDown)
            {
                NumericUpDown ud = sender as NumericUpDown;

                int channel;
                if (int.TryParse(ud.Tag.ToString(), out channel))
                {

                    if (_buddyPairs.ContainsKey(channel))
                    {
                        // angle is value
                        byte angle = (byte)ud.Value;

                        // find "buddy" control
                        // and update it's value
                        _buddyPairs[channel].UpdateFromNumeric();

                        // finally update servo
                        WriteServoPosition((byte)channel, angle);
                    }
                }
            }
        }

        private void toolStripDropDownButtonCommPort_DropDownOpening(object sender, EventArgs e)
        {
            PopulateToolStripMenu();
        }

        private void PopulateToolStripMenu()
        {
            toolStripDropDownButtonCommPort.DropDownItems.Clear();

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(port);
                toolStripDropDownButtonCommPort.DropDownItems.Add(item);

                if (port == _port)
                {
                    item.Checked = true;
                }
            }
        }

        private void toolStripDropDownButtonCommPort_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ChangePort(e.ClickedItem.Text);
        }

        private void fileSelectCommPortMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ChangePort(e.ClickedItem.Text);
        }

        BackgroundWorker bw = new BackgroundWorker();

        private void ChangePort(string port)
        {
            if (_state != State.Connecting && _port != port && !bw.IsBusy)
            {
                bw.RunWorkerAsync(port);
            }
        }

        private void writeDef(int channel, byte value)
        {
            if (serialPort1.IsOpen)
            {
                byte[] buffer = new byte[9];

                buffer[0] = (byte)'!';
                buffer[1] = (byte)'A';
                buffer[2] = (byte)'T';
                buffer[3] = (byte)'T';
                buffer[4] = (byte)'D';
                buffer[5] = (byte)'E';
                buffer[6] = (byte)'F';

                buffer[7] = (byte)channel;
                buffer[8] = value;

                serialPort1.Write(buffer, 0, 9);
            }

        }

        private void EnableDefs()
        {
            byte[] buffer = new byte[8];
            if (serialPort1.IsOpen)
            {
                buffer[0] = (byte)'!';
                buffer[1] = (byte)'A';
                buffer[2] = (byte)'T';
                buffer[3] = (byte)'T';
                buffer[4] = (byte)'E';
                buffer[5] = (byte)'D';
                buffer[6] = (byte)'D';
                buffer[7] = 1;

                serialPort1.Write(buffer, 0, 8);
            }
        }

        private void DisableDefs()
        {
            byte[] buffer = new byte[8];
            if (serialPort1.IsOpen)
            {
                buffer[0] = (byte)'!';
                buffer[1] = (byte)'A';
                buffer[2] = (byte)'T';
                buffer[3] = (byte)'T';
                buffer[4] = (byte)'E';
                buffer[5] = (byte)'D';
                buffer[6] = (byte)'D';
                buffer[7] = 0;

                serialPort1.Write(buffer, 0, 8);
            }

        }

        private void channel1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(1, (byte)trackBar2.Value);
        }

        private void channel0ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(0, (byte)trackBar1.Value);
        }

        private void channel2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(2, (byte)trackBar3.Value);
        }

        private void channel3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(3, (byte)trackBar4.Value);
        }

        private void channel4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(4, (byte)trackBar5.Value);
        }

        private void channel5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(5, (byte)trackBar6.Value);
        }

        private void channel6ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(6, (byte)trackBar7.Value);
        }

        private void channel7ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(7, (byte)trackBar8.Value);
        }

        private void channel8ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(8, (byte)trackBar9.Value);
        }

        private void channel9ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(9, (byte)trackBar10.Value);
        }

        private void channel10ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(10, (byte)trackBar11.Value);
        }

        private void channel11ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            writeDef(11, (byte)trackBar12.Value);
        }

        private void enableDefinedValuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableDefinedValuesToolStripMenuItem.Checked = !enableDefinedValuesToolStripMenuItem.Checked;

            if (enableDefinedValuesToolStripMenuItem.Checked)
            {
                EnableDefs();
            }
            else
            {
                DisableDefs();
            }
        }

        private void clearWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Console.Clear();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            serialPort1.DtrEnable = true;
            Thread.Sleep(250);
            serialPort1.DtrEnable = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            byte[] buffer = new byte[20];
            if (serialPort1.IsOpen)
            {
                buffer[0] = (byte)'!';
                buffer[1] = (byte)'A';
                buffer[2] = (byte)'T';
                buffer[3] = (byte)'T';
                buffer[4] = (byte)'W';
                buffer[5] = (byte)'R';
                buffer[6] = (byte)'F';
                buffer[7] = (byte)((int)numericUpDown13.Value - 1);
                buffer[8] = (byte)trackBar1.Value;
                buffer[9] = (byte)trackBar2.Value;
                buffer[10] = (byte)trackBar3.Value;
                buffer[11] = (byte)trackBar4.Value;
                buffer[12] = (byte)trackBar5.Value;
                buffer[13] = (byte)trackBar6.Value;
                buffer[14] = (byte)trackBar7.Value;
                buffer[15] = (byte)trackBar8.Value;
                buffer[16] = (byte)trackBar9.Value;
                buffer[17] = (byte)trackBar10.Value;
                buffer[18] = (byte)trackBar11.Value;
                buffer[19] = (byte)trackBar12.Value;

                serialPort1.Write(buffer, 0, 20);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                byte[] buffer = new byte[7];
                buffer[0] = (byte)'!';
                buffer[1] = (byte)'A';
                buffer[2] = (byte)'T';
                buffer[3] = (byte)'T';
                buffer[4] = (byte)'P';
                buffer[5] = (byte)'L';
                buffer[6] = (byte)'Y';
                serialPort1.Write(buffer, 0, 7);
            }
        }
    }
}
