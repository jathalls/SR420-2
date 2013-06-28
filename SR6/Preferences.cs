using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace SR7_420_2
{
    public partial class Preferences : Form
    {
        public GPSAccess gpsAccess { get; set; }

        public Preferences()
        {
            InitializeComponent();
            Load += new EventHandler(Preferences_Load);
        }

        void Preferences_Load(object sender, EventArgs e)
        {
            ShowComportSpeeds();
            ShowComPorts();
        }

        /*
        void Form1_Load(object sender, EventArgs e)
        {
            ShowComportSpeeds();
            ShowComPorts();
        }*/

        void ShowComPorts()
        {
            cboCom.Items.Clear();
            int index = 0;
            int setting = -1;
            foreach (String sp in System.IO.Ports.SerialPort.GetPortNames())
            {
                cboCom.Items.Add(sp);
                if (Properties.Settings.Default.GPSPort == sp)
                {
                    setting = index;
                }
                index++;
            }
            if (cboCom.Items.Count > 0)
            {
                if (setting >= 0)
                {
                    cboCom.SelectedIndex = setting;
                }
                else
                {
                    cboCom.SelectedIndex = cboCom.Items.Count - 1;
                }


                if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.GPSBaud))
                {
                    cboSpeed.SelectedValue = Properties.Settings.Default.GPSBaud;
                }
                else
                {
                    cboSpeed.SelectedIndex = 2;
                }
            }
        }

        void ShowComportSpeeds()
        {
            cboSpeed.Items.Clear();
            cboSpeed.Items.Add("1200");
            cboSpeed.Items.Add("2400");
            cboSpeed.Items.Add("4800");
            cboSpeed.Items.Add("9600");
            cboSpeed.Items.Add("14400");
            cboSpeed.Items.Add("19200");
            cboSpeed.Items.Add("38400");
            cboSpeed.Items.Add("57600");
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void cboCom_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void cboSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cbLogGPS_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLogGPS.Checked)
            {
                try
                {
                    if (gpsAccess == null)
                    {
                        gpsAccess = new GPSAccess();
                    }
                    if (gpsAccess != null)
                    {
                        gpsAccess.StartGPS(getPortNumber(), getBaudRate());
                        if (waitForGPS()) return;
                        if (!gpsAccess.isRunning)
                        {
                            gpsAccess.StartGPS(getPortNumber(), 4800);
                            if (waitForGPS()) return;
                            if (!gpsAccess.isRunning)
                            {
                                Cursor cursor = this.Cursor;
                                this.Cursor = Cursors.WaitCursor;
                                gpsAccess.StartGPS(getPortNumber(), 4800);
                                waitForGPS();
                                return;

                            }
                        }
                    }
                }
                catch (Exception) { }
                finally
                {
                    if (!gpsAccess.isRunning)
                    {
                        cbLogGPS.Checked = false;
                    }
                }
                        

            }
            else
            {
                gpsAccess.StopGPS();
            }
        }

        private bool waitForGPS()
        {
            Cursor cursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            DateTime timeout = DateTime.Now.AddMinutes(1);
            while (DateTime.Now < timeout)
            {
                if (gpsAccess.isRunning)
                {
                    break;
                }
            }
            this.Cursor = cursor;
            if (!gpsAccess.isRunning)
            {
                
                return (false);
            }
            return (true);
        }

        private int getBaudRate()
        {
            int result = 4800;
            String speed = "4800";

            if (!String.IsNullOrWhiteSpace((String)cboSpeed.SelectedValue))
            {
                speed = (String)cboSpeed.SelectedValue;
            }
            else
            {
                speed = Properties.Settings.Default.GPSBaud;
            }
            if (!String.IsNullOrWhiteSpace(speed))
            {
                int.TryParse(speed, out result);
            }
            return (result);
        }

        private int getPortNumber()
        {
            String portname;
            int result = 3;
            Debug.WriteLine("Selected value=<" + (String)cboCom.SelectedValue);
            Debug.WriteLine("Selected item=<" + (String)cboCom.SelectedItem);
            Debug.WriteLine("Selected text=<" + (String)cboCom.SelectedText);
            if (!String.IsNullOrWhiteSpace((String)cboCom.SelectedItem))
            {
                portname = (String)cboCom.SelectedItem;
            }
            else
            {
                portname = Properties.Settings.Default.GPSPort;
            }
            if (String.IsNullOrWhiteSpace(portname)) return (3);
            if (portname.EndsWith(":"))
            {
                portname = portname.Substring(0, portname.Length - 1);
            }
            Debug.WriteLine("Port selected = <" + portname + ">");
            if (portname.ToUpper().StartsWith("COM"))
            {
                portname = portname.Substring(3);
            }
            int.TryParse(portname, out result);
            return (result);

        }

        private void Preferences_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (cboCom.SelectedIndex >= 0)
            {
                Properties.Settings.Default.GPSPort = (String)cboCom.SelectedValue;
            }
            if (cboSpeed.SelectedIndex >= 0)
            {
                Properties.Settings.Default.GPSBaud = (String)cboSpeed.SelectedValue;
            }
            Properties.Settings.Default.Save();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }
    }
}
