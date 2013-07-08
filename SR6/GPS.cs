using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using tmagpsapi;
using System.Windows.Forms;
using System.IO;

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


        /// <summary>
        /// 
        /*
Global Positioning System Fix Data 



Name 

Example Data 

Description 

Sentence Identifier $GPGGA Global Positioning System Fix Data 
Time 170834 17:08:34 Z 
Latitude 4124.8963, N 41d 24.8963' N or 41d 24' 54" N 
Longitude 08151.6838, W 81d 51.6838' W or 81d 51' 41" W 
Fix Quality:
 - 0 = Invalid
 - 1 = GPS fix
 - 2 = DGPS fix 1 Data is from a GPS fix 
Number of Satellites 05 5 Satellites are in view 
Horizontal Dilution of Precision (HDOP) 1.5 Relative accuracy of horizontal position 
Altitude 280.2, M 280.2 meters above mean sea level 
Height of geoid above WGS84 ellipsoid -34.0, M -34.0 meters 
Time since last DGPS update blank No last update 
DGPS reference station id blank No station id 
Checksum *75 Used by program to check for transmission errors 


Courtesy of Brian McClure, N8PQI. 

Global Positioning System Fix Data. Time, position and fix related data for a GPS receiver. 

eg2. $--GGA,hhmmss.ss,llll.ll,a,yyyyy.yy,a,x,xx,x.x,x.x,M,x.x,M,x.x,xxxx 

hhmmss.ss = UTC of position 
 llll.ll = latitude of position
 a = N or S
 yyyyy.yy = Longitude of position
 a = E or W 
 x = GPS Quality indicator (0=no fix, 1=GPS fix, 2=Dif. GPS fix) 
 xx = number of satellites in use 
 x.x = horizontal dilution of precision 
 x.x = Antenna altitude above mean-sea-level
 M = units of antenna altitude, meters 
 x.x = Geoidal separation
 M = units of geoidal separation, meters 
 x.x = Age of Differential GPS data (seconds) 
 xxxx = Differential reference station ID 


eg3. $GPGGA,hhmmss.ss,llll.ll,a,yyyyy.yy,a,x,xx,x.x,x.x,M,x.x,M,x.x,xxxx*hh
1    = UTC of Position
2    = Latitude
3    = N or S
4    = Longitude
5    = E or W
6    = GPS quality indicator (0=invalid; 1=GPS fix; 2=Diff. GPS fix)
7    = Number of satellites in use [not those in view]
8    = Horizontal dilution of position
9    = Antenna altitude above/below mean sea level (geoid)
10   = Meters  (Antenna height unit)
11   = Geoidal separation (Diff. between WGS-84 earth ellipsoid and
       mean sea level.  -=geoid is below WGS-84 ellipsoid)
12   = Meters  (Units of geoidal separation)
13   = Age in seconds since last update from diff. reference station
14   = Diff. reference station ID#
15   = Checksum

*/
        /// </summary>
        /// <param name="Position"></param>
        void gps_SuccessfulFix(NMEA_Position Position)
        {
            lastPosition = Position;
            if (Position != null)
            {
                numsats = Position.NumberOfSats;
                
                latitude = Position.toNavalLatitude;
                longitude = Position.toNavalLongitude;
                sumLongitude += ToDecimal(longitude);
                sumLatitude += ToDecimal(latitude);
                n_for_average++;
                if (n_for_average >= averageOver)
                {
                    averageLatitude = sumLatitude / averageOver;
                    averageLongitude = sumLongitude / averageOver;
                    sumLongitude = 0d;
                    sumLatitude = 0d;
                    n_for_average = 0;
                    
                    OnGetAveragePosition( new PositionEventArgs( ToNaval(averageLatitude), ToNaval(averageLongitude) ) );
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

        public string ToNaval( double degrees ) {
            int iDegrees;
            int iMinutes;
            double seconds;
            double minutes;

            iDegrees = (int)Math.Floor( degrees );
            minutes = (degrees - iDegrees)*60;
            iMinutes = (int)Math.Floor( minutes );
            seconds = (minutes - iMinutes)*60;

            return ( iDegrees + "°" + iMinutes + @"'" + seconds + "\""+"("+degrees+")" );
        }

        public double ToDecimal(string NavalPosition)
        {
            var degreesplit = NavalPosition.Split("°".ToCharArray());
            double degrees = 0d;
            double.TryParse(degreesplit[0], out degrees);
            var minutesplit = degreesplit[1].Split(@"'".ToCharArray());
            double minutes = 0d;
            double.TryParse(minutesplit[0], out minutes);
            double seconds = 0d;
            while (!String.IsNullOrWhiteSpace(minutesplit[1]) && !Char.IsDigit(minutesplit[1][0]))
            {
                minutesplit[1] = minutesplit[1].Substring(1);
            }
            double.TryParse(minutesplit[1], out seconds);
            minutes += seconds / 60.0;
            degrees += minutes / 60.0;
            

            return (degrees);



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
               // return ("<" + lastPosition.toNavalLatitude + ", " + lastPosition.toNavalLongitude + " from " + numsats + " satellites >");
                string result = "raw=" + lastPosition.Latitude + "\n   decimal=" + lastPosition.LatitudeDecimal + "d\n    Naval=" + lastPosition.toNavalLatitude + "\n     string=" + lastPosition.ToString();
                return (result);
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
