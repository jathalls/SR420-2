using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using tmagpsapi;
using System.Windows.Forms;

namespace SR7_420_2
{
    public class GPSAccess
    {
    

        NMEA gps = new NMEA();
        tmaSerialport sp = new tmaSerialport();
        NMEA_GPVTG LastVTG = new NMEA_GPVTG();

        public string latitude { get; set; }
        public string longitude { get; set; }
        public int numsats { get; set; }
        private bool _isRunning;

        public bool isRunning {
            get
            {
                return (sp.IsComportOpen);
            }
            
        }

       


        public GPSAccess()
        {
            
            gps.SuccessfulFix += new NMEA.SuccessfulFixEventHandler(gps_SuccessfulFix);
            sp.ComPortClosed += new tmaSerialport.ComPortClosedEventHandler(sp_ComPortClosed);
            sp.ComPortError += new tmaSerialport.ComPortErrorEventHandler(sp_ComPortError);
            sp.ComPortOpen += new tmaSerialport.ComPortOpenEventHandler(sp_ComPortOpen);
            sp.LineRecieved += new tmaSerialport.LineRecievedEventHandler(sp_LineRecieved);
            latitude = "0";
            longitude = "0";
            numsats = 0;
            
            
            
        }
        

        void sp_LineRecieved(string Data)
        {
            var tmparr = Data.Split(",".ToCharArray());
            switch (tmparr[0])
            {
                case "$GPGGA":
                    ProcessGPGGA(Data);
                    break;
                case "$GPVTG":
                    ProcessVTG(Data);
                    break;
                default:
                    break;
            }
            //GpsData(Data);
        }

        

        private void ProcessVTG(string Data)
        {
            /*this.UIThread(delegate
            {
                gps.NMEA_Direction.VTG=Data;
            });*/
            try
            {
                gps.NMEA_Direction.VTG = Data;
            }
            catch (Exception) { }
        }

        private void ProcessGPGGA(string Data)
        {
            /*
            this.UIThread(delegate
            {
                gps.NMEA_POS.GPGGA = Data;
            });*/
            try
            {
                gps.NMEA_POS.GPGGA = Data;
            }
            catch (Exception) { }
        }

        void sp_ComPortOpen()
        {
            Debug.WriteLine("Comport opened");
        }

        void sp_ComPortError(Exception ex, string Message)
        {
            Debug.WriteLine("Comport error:-" + Message);
        }

        void sp_ComPortClosed()
        {
            Debug.WriteLine("Comport closed");
        }

        void gps_SuccessfulFix(NMEA_Position Position)
        {
            if (!String.IsNullOrWhiteSpace(Position.LongitudeDecimal.ToString()))
            {
                
                longitude = Position.LongitudeDecimal.ToString();
                //longitude = Position.toNavalLongitude;
            }
            else
            {
                longitude = "N/A";
            }
            if (!String.IsNullOrWhiteSpace(Position.LatitudeDecimal.ToString()))
            {
                //latitude = Position.LatitudeDecimal.ToString();
                latitude = Position.LatitudeDecimal.ToString();
            }
            else
            {
                latitude = "N/A";
            }
            
            numsats = Position.NumberOfSats;
           
            
        }

        public void StartGPS(int portNum,int baudrate)
        {
            //gps.NMEA_POS.GPGGA = tGPPGA.Text;
            if (sp == null) return;

            if (sp.IsComportOpen) sp.Close();
            tmaSerialport.enumBaudRates tmaBaudRate;
            switch (baudrate)
            {
                case 1200:
                    tmaBaudRate = tmaSerialport.enumBaudRates.BaudRate1200; break;
                case 2400:
                    tmaBaudRate = tmaSerialport.enumBaudRates.BaudRate2400; break;
                case 4800:
                    tmaBaudRate = tmaSerialport.enumBaudRates.BaudRate4800; break;
                case 9600:
                    tmaBaudRate = tmaSerialport.enumBaudRates.BaudRate9600; break;
                case 14400:
                    tmaBaudRate = tmaSerialport.enumBaudRates.BaudRate14400; break;
                case 19200:
                    tmaBaudRate = tmaSerialport.enumBaudRates.BaudRate19200; break;
                case 38400:
                    tmaBaudRate = tmaSerialport.enumBaudRates.BaudRate38400; break;
                case 57600:
                    tmaBaudRate = tmaSerialport.enumBaudRates.BaudRate57600; break;
                default: 
                    tmaBaudRate = tmaSerialport.enumBaudRates.BaudRate4800; break;
            }
            Debug.WriteLine("open gps on COM" + portNum + " at " + tmaBaudRate);
            sp.Openport(portNum, System.IO.Ports.Parity.None, tmagpsapi.tmaSerialport.enumDatabits.Bit8, System.IO.Ports.StopBits.One, tmaBaudRate);
        }

        public void StopGPS()
        {
            if (sp != null && sp.IsComportOpen) sp.Close();
        }

        public String getPosition()
        {
            return ("< "+latitude+", "+longitude + " from " + numsats + " satellites >");
        }
    }

    static class FormExtensions
    {
        static public void UIThread(this Form form, MethodInvoker code)
        {
            if (form.InvokeRequired)
            {
                form.Invoke(code);
                return;
            }
            code.Invoke();
        }
    }
}
