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
        public int averageOver { get; set; }
        private int n_for_average;
        public string lastCompleteAveragePosition { get; set; }
        int portnum = 1;
        int baudrate = 4800;

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
            averageOver = 1;
            lastCompleteAveragePosition = "";
            n_for_average = 0;
            
            
            
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

        double sumLongitude = 0d;
        double sumLatitude = 0d;
        private NMEA_Position lastPosition = null;
        double averageLongitude = 0d;
        double averageLatitude = 0d;

        void gps_SuccessfulFix(NMEA_Position Position)
        {
            lastPosition = Position;
            if (Position != null)
            {
                numsats = Position.NumberOfSats;
                sumLongitude += Position.LongitudeDecimal;
                sumLatitude += Position.LatitudeDecimal;
                n_for_average++;
                if (n_for_average >= averageOver)
                {
                    averageLatitude = sumLatitude / averageOver;
                    averageLongitude = sumLongitude / averageOver;
                    sumLongitude = 0d;
                    sumLatitude = 0d;
                    n_for_average = 0;
                }
            }
            /*
            if (!String.IsNullOrWhiteSpace(Position.LongitudeDecimal.ToString()))
            {
                
                longitude = Position.LongitudeDecimal.ToString();
                if(!String.IsNullOrWhiteSpace(Position.LatitudeDecimal.ToString()))
                sumLongitude += Position.LatitudeDecimal;
                

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
            
            numsats = Position.NumberOfSats;*/
           
            
        }

        /// <summary>
        /// if port and baudrate are not specified use the last used settings
        /// </summary>
        public void StartGPS( ) {
            StartGPS( portnum, baudrate );
        }

        public void StartGPS(int portNum,int baudrate)
        {
            this.portnum = portNum; // remember these values for next time
            this.baudrate = baudrate;
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
            //return ("< "+latitude+", "+longitude + " from " + numsats + " satellites >");
            if (lastPosition != null)
            {
                return ("<" + lastPosition.toNavalLatitude + ", " + lastPosition.toNavalLongitude + " from " + numsats + " satellites >");
            }
            return ("");
        }

        private readonly object GetAveragePositionEventLock = new object( );
        private EventHandler GetAveragePositionEvent;
        
        /// <summary>
        /// Event raised after the <see cref="Text" /> property value has changed.
        /// </summary>
        public event EventHandler GetAveragePosition {
            add {
                lock ( GetAveragePositionEventLock ) {
                    GetAveragePositionEvent += value;
                }
            }
            remove {
                lock ( GetAveragePositionEventLock ) {
                    GetAveragePositionEvent -= value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="TextChanged" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
        protected virtual void OnGetAveragePosition( PositionEventArgs e ) {
            EventHandler handler = null;

            lock ( GetAveragePositionEventLock ) {
                handler = GetAveragePositionEvent;

                if ( handler == null )
                    return;
            }

            handler( this, e );
        }
    }

    /// <summary>
    /// Provides arguments for an event.
    /// </summary>
    [Serializable]
    public class PositionEventArgs : EventArgs {
        public new static readonly PositionEventArgs Empty = new PositionEventArgs("","" );

        #region Public Properties
        public String Latitude = "";
        public String Longitude="";
        #endregion

        #region Private / Protected
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a new instance of the <see cref="CustomEventArgs" /> class.
        /// </summary>
        public PositionEventArgs(String Latitude,String Longitude ) {
            this.Latitude = Latitude;
            this.Longitude = Longitude;
        }
        #endregion

        public String ToString( ) {
            return ( Latitude + ", " + Longitude );
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
