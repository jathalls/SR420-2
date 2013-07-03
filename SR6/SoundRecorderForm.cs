using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SR7_420_2
{
    /// <summary>
    /// The main form for the project which displays a control panel
    /// and a spectrogram display which runs continuously.
    /// Ketpresses or bttons can be used to perform discrete actions
    /// such as setting parameters, starting and stopping recording or
    /// setting the bat detector frequency.
    /// </summary>
    public partial class SoundRecorderForm : Form
    {
        // the following variables have default values here which are overwritten by
        //    the contents of the settings.Settings file.
        String filePath     = @"C:\BatRecordings"; // default location for recordings and log files
        String fileTemplate = @"SR7Recording"; // default file name structure for recordings
        int index           = 1; // default initial index for appending to sequential recordings
        bool addDateTime    = true; // default condition adds the date and time to recording file names
        bool addIndex       = false; // default condition to not add a sequential index number to recording file names unless needed

        bool isRecording    = false; // flag set when recording is in progress
        bool timedRecording = false; // flag set if the current recording is for a preset timed interval

        String logFileName  = "SR7Log.log"; // default name for a log file
        
        Timer timer         = new Timer();  // timer instance for use with defined duration recordings

        GPSAccess gpsMain = new GPSAccess(); // gps system access provision

        int timeRemaining { get; set; }

        /// <summary>
        /// Initializer for the form class
        /// initializes the timer for when it is needed
        /// reads Setting file definitions that do not acto on visual components
        /// and ensures that file paths and names are valid
        /// Starts the log file
        /// </summary>
        public SoundRecorderForm()
        {
            InitializeComponent();
            timer = new Timer();
            timer.Interval = 60000;
            timer.Tick += new EventHandler(Timer_Tick);
            timeRemaining = 0;

            filePath = Properties.Settings.Default.File_Path;
            if (!filePath.EndsWith(@"\")) filePath = filePath + @"\";
            try
            {
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
            }
            catch (Exception)
            {
                filePath = @".\";
            }
            fileTemplate = Properties.Settings.Default.File_Name_Template;
            index = Properties.Settings.Default.Initial_Index;
           
            addDateTime = Properties.Settings.Default.AppendDateTime;
            addIndex = Properties.Settings.Default.AppendIndex;

            
            Load += new EventHandler(SoundRecorderForm_Load);
            

            isRecording = false;

            

            


        }

        /// <summary>
        /// Event handler called when the main form is loaded
        /// reads settings that use visual components
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SoundRecorderForm_Load(object sender, EventArgs e)
        {

            try
            {
                SetCursor();
                waterfall1.Levels.Axis.Max = Properties.Settings.Default.Brightness;
                waterfall1.Levels.Axis.Min = Properties.Settings.Default.Contrast;

                fourier1.SamplingWindowStep = (uint)Properties.Settings.Default.SampleAdvance;
                switch ((string)Properties.Settings.Default.FourierWindow)
                {
                    case "Blackman":
                        fourier1.WindowType = Mitov.SignalLab.ExtendedWindowType.Blackman; break;
                    case "Bartlett":
                        fourier1.WindowType = Mitov.SignalLab.ExtendedWindowType.Bartlett; break;
                    case "Hamming":
                        fourier1.WindowType = Mitov.SignalLab.ExtendedWindowType.Hamming; break;
                    case "Hanning":
                        fourier1.WindowType = Mitov.SignalLab.ExtendedWindowType.Hanning; break;
                    case "Kaiser":
                        fourier1.WindowType = Mitov.SignalLab.ExtendedWindowType.Kaiser; break;
                    case "Rect":
                        fourier1.WindowType = Mitov.SignalLab.ExtendedWindowType.Rect; break;
                    case "CosSum":
                        fourier1.WindowType = Mitov.SignalLab.ExtendedWindowType.CosSum; break;
                    case "FlatTop":
                        fourier1.WindowType = Mitov.SignalLab.ExtendedWindowType.FlatTop; break;
                    default:
                        fourier1.WindowType = Mitov.SignalLab.ExtendedWindowType.Hamming; break;

                }

                SetSignalGeneratorSignalType(Properties.Settings.Default.LocalOscillator.ToString());

                
                if (BatDetectorTSButton.Text == "Force Bat Detector")
                {
                    BatDetectorTSButton_Click(this, new EventArgs());
                }
                SetBatDetectorLabel();
                SetFilterLabel();
                SetDeviceLabel();

                Assembly assembly = Assembly.GetExecutingAssembly();
                
                //AssemblyName assemblyName = assembly.GetName();
                this.Text = assembly.FullName;
                var parts = this.Text.Split(",".ToCharArray());
                this.Text = parts[0] + ", " + parts[1];
                //this.Text = "SR7-420-2 " + versionText;// assemblyName.Version;
                CreateLogFile();
            }
            catch (Exception ex)
            {

                File.AppendAllText(filePath + fileTemplate + "ERROR.txt",DateTime.Now.ToString()+ex.Message + ex.StackTrace);
            }



           
        }

        private void SetDeviceLabel()
        {
            lblDeviceName.Text = AudioSource.Device.DeviceName;
        }

        private void SetBatDetectorLabel()
        {
            if (BatDetectorForced)
            {
                lblBDState.Text = "Bat Detector Always ON";
            }
            else
            {
                lblBDState.Text = "Bat Detector OFF during Recording";
            }
        }

        private void SetSignalGeneratorSignalType(string signalType)
        {
            switch (signalType)
            {
                case "Tone":
                    signalGen1.SignalType = Mitov.SignalLab.SignalType.Tone;
                    break;
                case "Triangle":
                    signalGen1.SignalType = Mitov.SignalLab.SignalType.Triangle;
                    break;
                case "Square":
                    signalGen1.SignalType = Mitov.SignalLab.SignalType.Square;
                    break;
                default:
                    signalGen1.SignalType = Mitov.SignalLab.SignalType.Tone;
                    break;

            }
        }

        /// <summary>
        /// Creates a new log file and writes initial header information to it.
        /// </summary>
        private void CreateLogFile()
        {
            try
            {
                DateTime today = DateTime.Today;
                logFileName = filePath + fileTemplate+"_"+today.Year+"_"+today.Month+"_"+today.Day+"_";
                if (File.Exists(logFileName + ".log"))
                {
                    int specIndex = index;
                    while (File.Exists(logFileName + specIndex + ".log")) specIndex++;
                    logFileName = logFileName + specIndex + ".log";
                }
                else
                {
                    logFileName = logFileName + ".log";
                }

                File.AppendAllText(logFileName, this.Text+@" Bat Recording Log File
 " + AudioSource.Device.DeviceName + " " + (int)AudioSource.AudioFormat.SampleRate + @"ksps "+(int)AudioSource.AudioFormat.Bits+@" bits
" + DateTime.Now.ToShortDateString() + " started at " + DateTime.Now.ToShortTimeString() + @"

");
                if (gpsMain!=null && gpsMain.isRunning)
                {
                    try
                    {
                        File.AppendAllText(logFileName, gpsMain.getPosition() + "\n");
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception ex)
            {
                try{
                    File.AppendAllText(filePath + fileTemplate + "ERROR.txt",DateTime.Now.ToString()+ex.Message + ex.StackTrace);

                }catch(Exception){}
            }
        }


        bool isClosing = false;

        /// <summary>
        /// Closes down the system gently when the main form closes for any reason.
        /// A flag is used to ensure that the function cannot recurse.
        /// Any recordings in progress are terminated gently and the termination
        /// event is recorded in the log file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoundRecorderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isClosing)
            {
                isClosing = true;
                try
                {
                    if (isRecording)
                    {
                        ToggleRecording();
                    }

                    AudioSource.Enabled = false;
                    waterfall1.Enabled = false;
                    audioOut1.Enabled = false;
                    Properties.Settings.Default.File_Path = filePath;
                    Properties.Settings.Default.File_Name_Template = fileTemplate;
                    Properties.Settings.Default.Initial_Index = index;
                    Properties.Settings.Default.AppendDateTime = addDateTime;
                    Properties.Settings.Default.AppendIndex = addIndex;
                    Properties.Settings.Default.Brightness = waterfall1.Levels.Axis.Max;
                    Properties.Settings.Default.Contrast = waterfall1.Levels.Axis.Min;
                    Properties.Settings.Default.SampleAdvance = (int)fourier1.SamplingWindowStep;
                    Properties.Settings.Default.FourierOrder = (int)fourier1.Order;
                    Properties.Settings.Default.LocalOscillator = signalGen1.SignalType.ToString().Trim();
                   // Debug.WriteLine(signalGen1.SignalType.ToString().Trim());
                    Properties.Settings.Default.BufferSize = (int)AudioSource.AudioFormat.BufferSize;
                    Properties.Settings.Default.WordSize = (int)AudioSource.AudioFormat.Bits;
                    Properties.Settings.Default.SampleRate = (int)AudioSource.AudioFormat.SampleRate;
                    switch (fourier1.WindowType)
                    {
                        case Mitov.SignalLab.ExtendedWindowType.Bartlett:
                            Properties.Settings.Default.FourierWindow = "Bartlett"; break;
                        case Mitov.SignalLab.ExtendedWindowType.Blackman:
                            Properties.Settings.Default.FourierWindow = "Blackman"; break;
                        case Mitov.SignalLab.ExtendedWindowType.CosSum:
                            Properties.Settings.Default.FourierWindow = "CosSum"; break;
                        case Mitov.SignalLab.ExtendedWindowType.FlatTop:
                            Properties.Settings.Default.FourierWindow = "FlatTop"; break;
                        case Mitov.SignalLab.ExtendedWindowType.Hamming:
                            Properties.Settings.Default.FourierWindow = "Hamming"; break;
                        case Mitov.SignalLab.ExtendedWindowType.Hanning:
                            Properties.Settings.Default.FourierWindow = "Hanning"; break;
                        case Mitov.SignalLab.ExtendedWindowType.Kaiser:
                            Properties.Settings.Default.FourierWindow = "Kaiser"; break;
                        case Mitov.SignalLab.ExtendedWindowType.Rect:
                            Properties.Settings.Default.FourierWindow = "Rect"; break;
                        default:
                            Properties.Settings.Default.FourierWindow = "Hamming"; break;
                    }


                    Properties.Settings.Default.Save();
                    if (isRecording)
                    {
                        waveLogger1.Close();
                        waveLogger1.Enabled = false;
                        isRecording = false;
                    }
                    AudioSource.Stop();
                    //AudioSource.Close();
                    File.AppendAllText(logFileName, @"

Session Ended at " + DateTime.Now.ToShortTimeString());

                    if (gpsMain != null)
                    {
                        if (gpsMain.isRunning)
                        {
                            gpsMain.StopGPS();
                            gpsMain = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        File.AppendAllText(logFileName, @"ERROR closing form
" + ex.ToString() + @"
" + ex.Message + @"
" + ex.StackTrace);
                    }
                    catch (Exception) { }
                }
            }
            

        }



        int minutes = 0;
        int segmentSize = 5;
        bool continuousFlag = false;
        bool startTimer = false;

        /// <summary>
        /// Intercepts any key presses made within the area of the main form.
        /// SPACE toggles recording on and off
        /// UP/DOWN change the bat detector frequency in 2kHz increments
        /// RIGHT/LEFT change the local oscilator amplitude, effectively changing the audio output volume
        /// 5/KEYPAD5 starts a timed 5 minute recording
        /// BACKSPACE resets the default values for the local oscillator amplitude and frequency
        /// ,/< decreases the max level f waterfall 10fold
        /// ./> increases the max level of the waterfall 10 fold
        /// C starts contiinuous recording in 5minute blocks
        /// ESC if continuous recording, end at the end of the current block
        ///     if recording but not continuous stop recording or cancel a 5 minute record block
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoundRecorderForm_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                //minutes = 0;
                startTimer = false;
                switch (e.KeyCode)
                {
                    case Keys.Space:

                        if (!timedRecording)
                        {
                            ToggleRecording();
                        }
                        break;


                    case Keys.D4:
                    case Keys.NumPad4:
                         if (!isRecording)
                        {
                            if (e.Shift)
                            {
                                segmentSize = 4;
                                minutes = 4;
                                continuousFlag = true;
                            }
                            minutes = 4;
                            timeRemaining = 4;
                            startTimer = true;
                            continuousFlag = false;
                        }
                        break;


                    case Keys.D5:
                    case Keys.NumPad5:
                        if (!isRecording)
                        {
                            if (e.Shift)
                            {
                                segmentSize = 5;
                                minutes = 5;
                                continuousFlag = true;
                            }
                            minutes = 5;
                            timeRemaining = 5;
                            startTimer = true;
                            continuousFlag = false;
                        }
                        break;

                    case Keys.D1:
                    case Keys.NumPad1:
                        if (!isRecording)
                        {
                            if (e.Shift)
                            {
                                segmentSize = 1;
                                minutes = 1;
                                continuousFlag = true;
                            }
                            minutes = 1;
                            timeRemaining = 1;
                            startTimer = true;
                            continuousFlag = false;
                        }
                        break;

                    case Keys.Up:
                        int increment = 1000;
                        if (e.Shift) increment = 5000;
                        signalGen1.Frequency += increment;
                        if (signalGen1.Frequency > 100000)
                        {
                            signalGen1.Frequency = 100000;
                        }
                        lblHetFreq.Text = (signalGen1.Frequency / 1000).ToString() + " kHz";
                        SetCursor();
                        break;

                    case Keys.Down:
                        increment = 1000;
                        if (e.Shift) increment = 5000;
                        signalGen1.Frequency -= increment;
                        if (signalGen1.Frequency < 14000)
                        {
                            signalGen1.Frequency = 14000;
                        }
                        lblHetFreq.Text = (signalGen1.Frequency / 1000).ToString() + " kHz";
                        SetCursor();
                        break;

                    case Keys.Right:
                        signalGen1.Amplitude = signalGen1.Amplitude * 2;
                        break;

                    case Keys.Left:
                        signalGen1.Amplitude = signalGen1.Amplitude / 2;
                        break;
                    case Keys.Back:
                        signalGen1.Amplitude = 1.0d;
                        signalGen1.Frequency = 40000;
                        SetCursor();
                        break;
                    case Keys.Oemcomma:
                        if (e.Shift || e.Control)
                        {
                            if (waterfall1.Levels.Axis.Min == 0.0d)
                            {
                                waterfall1.Levels.Axis.Min = 10.0d;
                            }
                            waterfall1.Levels.Axis.Min = waterfall1.Levels.Axis.Min / 10.0d;
                        }
                        else
                        {
                            waterfall1.Levels.Axis.Max = waterfall1.Levels.Axis.Max / 10.0d;
                        }
                        break;
                    case Keys.OemPeriod:
                        if (e.Shift || e.Control)
                        {
                            if (waterfall1.Levels.Axis.Min == 0.0d)
                            {
                                waterfall1.Levels.Axis.Min = 1.0d;
                            }
                            waterfall1.Levels.Axis.Min = waterfall1.Levels.Axis.Min * 10.0d;
                        }
                        else
                        {
                            waterfall1.Levels.Axis.Max = waterfall1.Levels.Axis.Max * 10;
                        }
                        break;
                    case Keys.C:
                        if (!isRecording)
                        {
                            continuousFlag = true;
                            minutes = 5;
                            segmentSize = 5;
                            startTimer = true;
                        }
                        
                        break;
                    case Keys.Escape:
                        
                        if (isRecording)
                        {
                            if (continuousFlag)
                            {
                                continuousFlag = false;
                                FileStatusLabel.Text = "Ends after " + minutes + " minutes - ESC to stop now";
                                break;
                            }
                            minutes = 0;
                            timeRemaining = 0;
                            timer.Stop();
                            ToggleRecording();
                        }
                            
                        break;

                    default:
                        break;
                }
                if (startTimer)
                {

                    File.AppendAllText(logFileName, @"
Timed Recording " + minutes + " Mins ");
                    FileStatusLabel.Text = minutes + " Mins to Go";
                    timer.Interval = minutes > 0 ? minutes * 60000 : 60000;
                    ToggleRecording();
                    timedRecording = true;
                   
                    timer.Start();

                }
            }catch (Exception ex)
            {
                try{
                    File.AppendAllText(logFileName,@"Key Command ERROR
"+ex.ToString()+@"
"+ex.Message+@"
"+ex.StackTrace);
                }
                catch (Exception ex2)
                {
                    File.AppendAllText(logFileName, @"
KeyCommandException error
" + ex2.Message + Environment.NewLine + ex2.StackTrace);
                }
            }
        }

        /// <summary>
        /// Deprecated
        /// </summary>
        private void SetCursor()
        {
            try
            {
                waterfall1.Cursors[0].Position.Y = signalGen1.Frequency / 1000;
                waterfall1.Cursors[1].Position.Y = waterfall1.Cursors[0].Position.Y + 5;
                waterfall1.Cursors[2].Position.Y = waterfall1.Cursors[0].Position.Y - 5;
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFileName, @"
Set Cursor error
" + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            
        }

        /// <summary>
        /// handles the timer time-out event
        /// The timer is set to intervals of one minute and this gets called
        /// each time.  The time remaining is displayed and the timer is restarted
        /// until the total programmed time (stored in the variable 'minutes'
        /// reaches zero.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            try
            {
                //minutes--;
                //timeRemaining--;
                //if (minutes <= 0 && timeRemaining<=0)
                //{
                    ToggleRecording(); // to stop the recording
                    timedRecording = false;
                    FileStatusLabel.Text = "Hit Space to Start Recording";
                    (sender as Timer).Stop();
                    if (continuousFlag)
                    {
                        minutes = segmentSize;
                        ToggleRecording();
                        timedRecording = true;
                        FileStatusLabel.Text = "Continuous timed recording - ESC to stop " + minutes + " Mins to Go";
                        (sender as Timer).Start();
                    }
                    else
                    {
                        timer.Interval = 60000;
                    }
                /*}
                else
                {
                    if (continuousFlag)
                    {
                        FileStatusLabel.Text = "Continuous timed recording - ESC to stop " + minutes + " Mins to Go";
                    }
                    else
                    {
                        FileStatusLabel.Text = minutes + " Mins to Go";
                    }
                    (sender as Timer).Start();
                }*/
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFileName, @"
Timer timeout error
" + ex.Message + Environment.NewLine + ex.StackTrace);
            }

        }

        DateTime recordingStartedAt = DateTime.Now;

        /// <summary>
        /// Toggles the recording state, making suitable entries inthe
        /// log file at the same time.
        /// </summary>
        void ToggleRecording()
        {
            try
            {
                AudioSource.Enabled = false;
                String fullFileName = "";

                if (isRecording)
                {// then stop recording
                    waveLogger1.Close();
                    waveLogger1.Enabled = false;
                    isRecording = false;
                    if (timer != null)
                    {
                        timer.Stop();
                    }
                    timedRecording = false;
                    textBox1.Text = "";
                    FileStatusLabel.Text = "Hit Space to Start Recording";
                    File.AppendAllText(logFileName, @" to " + DateTime.Now.ToLongTimeString() + @" for "+(DateTime.Now-recordingStartedAt).ToString()+@"
");
                    if (gpsMain.isRunning)
                    {
                        File.AppendAllText(logFileName, " "+gpsMain.getPosition() + "\n");
                        GPSPositionLabel.Text = gpsMain.getPosition();

                    }
                    else
                    {
                        GPSPositionLabel.Text = "GPS Off";
                    }
                    ButtonsEnabled(true);
                    BatDetector(BAT_DETECTOR.ON);
                }
                else
                {// then start recording
                    try
                    {
                        if (!BatDetectorForced)
                        {
                            BatDetector(BAT_DETECTOR.OFF);
                        }
                        ButtonsEnabled(false);
                        fullFileName = filePath + fileTemplate;
                        if (addDateTime)
                        {
                            DateTime now = DateTime.Now;
                            fullFileName = fullFileName + "_" + now.Year + "_" + now.Month + "_" + now.Day + "_" + now.Hour + "-" + now.Minute;
                            if (File.Exists(fullFileName + ".wav"))
                            {
                                int specIndex = 1;
                                while (File.Exists(fullFileName + "_" + specIndex + ".wav")) specIndex++;
                                fullFileName = fullFileName + "_" + specIndex;
                            }
                        }
                        if (addIndex)
                        {
                            while (File.Exists(fullFileName + "_" + index + ".wav")) index++;
                            fullFileName = fullFileName + "_" + index;
                        }
                        fullFileName = fullFileName + ".wav";
                        if (gpsMain.isRunning)
                        {
                            File.AppendAllText(logFileName, " "+gpsMain.getPosition() + "\n");
                        }
                        recordingStartedAt = DateTime.Now;
                        File.AppendAllText(logFileName, @"
Recording " + fullFileName + " from " + recordingStartedAt.ToLongTimeString());
                        waveLogger1.FileName = fullFileName;
                        waveLogger1.Enabled = true;
                        textBox1.Text = fullFileName;
                        isRecording = true;
                        if (continuousFlag)
                        {
                            FileStatusLabel.Text = "Continuous timed recording - ESC to stop";
                        }
                        else
                        {
                            FileStatusLabel.Text = "Hit Space to Stop Recording";
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(logFileName, @"
Starting recording error
" + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(logFileName, @"ERROR
" + ex.ToString() + @"
" + ex.Message + @"
" + ex.StackTrace);
                }
                catch (Exception) { }
            }
            finally
            {
                AudioSource.Enabled = true;
            }
        }

        private void BatDetector(BAT_DETECTOR bAT_DETECTOR)
        {
            
            if (bAT_DETECTOR == BAT_DETECTOR.ON)
            {
                if (!signalGen1.Enabled)
                {
                    audioOut1.Enabled = true;
                    signalGen1.Enabled = true;
                    multiply1.InputPins[0].Connect(AudioSource.OutputPin);
                }
            }
            else
            {
                if (signalGen1.Enabled)
                {
                    multiply1.InputPins[0].Disconnect();
                    signalGen1.Enabled = false;
                    audioOut1.Enabled = false;
                }
            }

        }

        /// <summary>
        /// disables or re-enables the on-screen buttons when recording is started dor stopped
        /// to prevent critical parameters changing during a single recording.
        /// </summary>
        /// <param name="newState"></param>
        private void ButtonsEnabled(bool newState)
        {
            preferencesToolStfripButton.Enabled = newState;
            DevicesToolStripButton.Enabled = newState;
            SettingsToolStripButton.Enabled = newState;
            SpeakersToolStripButton.Enabled = newState;
            HelpToolStripButton.Enabled = newState;
            ToggleFilterToolStripButton.Enabled = newState;
            BatDetectorTSButton.Enabled = newState;

            
        }

        /// <summary>
        /// Creates and initialises the Preferences dialog and if the OK button
        /// is pressed stores the resulting values in the Settings file and
        /// enacts them in the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreferencesButton_Click(object sender, EventArgs e)
        {
        }




       

        private void preferencesToolStfripButton_Click(object sender, EventArgs e)
        {

            try
            {
                AudioSource.Enabled = false;
                using (Preferences preferences = new Preferences())
                {
                    preferences.gpsAccess = this.gpsMain;
                    
                    preferences.tbFileSavePath.Text = filePath;
                    preferences.tbFileNameTemplate.Text = fileTemplate;
                    preferences.nudInitialIndex.Value = index;
                    preferences.cbAppendDateTime.Checked = addDateTime;
                    preferences.cbAppendIndex.Checked = addIndex;

                    preferences.nudBrightness.Value = (decimal)waterfall1.Levels.Axis.Max;
                    preferences.nudContrast.Value = (decimal)waterfall1.Levels.Axis.Min;
                    preferences.cbColour.Checked = !waterfall1.Levels.GrayScale;

                    preferences.cbxFourierWindow.Items.Clear();

                    preferences.cbxFourierWindow.Items.Add(Mitov.SignalLab.ExtendedWindowType.Bartlett);
                    preferences.cbxFourierWindow.Items.Add(Mitov.SignalLab.ExtendedWindowType.Blackman);
                    preferences.cbxFourierWindow.Items.Add(Mitov.SignalLab.ExtendedWindowType.CosSum);
                    preferences.cbxFourierWindow.Items.Add(Mitov.SignalLab.ExtendedWindowType.FlatTop);
                    preferences.cbxFourierWindow.Items.Add(Mitov.SignalLab.ExtendedWindowType.Hamming);
                    preferences.cbxFourierWindow.Items.Add(Mitov.SignalLab.ExtendedWindowType.Hanning);
                    preferences.cbxFourierWindow.Items.Add(Mitov.SignalLab.ExtendedWindowType.Kaiser);
                    preferences.cbxFourierWindow.Items.Add(Mitov.SignalLab.ExtendedWindowType.Rect);

                    preferences.cbxFourierWindow.SelectedItem = fourier1.WindowType;
                    preferences.FourierOrderNumericUpDown.Value = (decimal)fourier1.Order;
                    preferences.nudSampleAdvance.Value = (decimal)fourier1.SamplingWindowStep;


                    preferences.LocalOscillatorComboBox.SelectedItem = signalGen1.SignalType.ToString();

                    preferences.BufferSizeComboBox.SelectedItem = AudioSource.AudioFormat.BufferSize.ToString();
                    preferences.cbxSampleRate.SelectedItem = AudioSource.AudioFormat.SampleRate.ToString();
                    preferences.cbxWordSize.SelectedItem = AudioSource.AudioFormat.Bits.ToString();


                    var result = preferences.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        try
                        {
                            String newPath = preferences.tbFileSavePath.Text;
                            if (!String.IsNullOrWhiteSpace(newPath))
                            {
                                if (!newPath.EndsWith(@"\")) newPath = newPath + @"\";
                                if (!Directory.Exists(newPath))
                                {
                                    Directory.CreateDirectory(newPath);
                                }
                                if (newPath != filePath)
                                {
                                    
                                    filePath = newPath;
                                    CreateLogFile();
                                    
                                }
                            }
                        }
                        catch (Exception) { }

                        try
                        {
                            String newTemplate = preferences.tbFileNameTemplate.Text;
                            if (!String.IsNullOrWhiteSpace(newTemplate))
                            {
                                fileTemplate = newTemplate;
                            }
                        }
                        catch (Exception ex) { File.AppendAllText(filePath + fileTemplate + "ERROR.txt",DateTime.Now.ToString()+ex.Message + ex.StackTrace); }

                        try
                        {
                            index = (int)preferences.nudInitialIndex.Value;
                        }
                        catch (Exception ex) {
                            File.AppendAllText(filePath + fileTemplate + "ERROR.txt",DateTime.Now.ToString()+ex.Message + ex.StackTrace);
                        }

                        addDateTime = preferences.cbAppendDateTime.Checked;
                        addIndex = preferences.cbAppendIndex.Checked;
                        waterfall1.Levels.Axis.Max = (double)preferences.nudBrightness.Value;
                        waterfall1.Levels.Axis.Min = (double)preferences.nudContrast.Value;
                        waterfall1.Levels.GrayScale = !preferences.cbColour.Checked;
                        fourier1.WindowType = (Mitov.SignalLab.ExtendedWindowType)(preferences.cbxFourierWindow.SelectedItem ?? Mitov.SignalLab.ExtendedWindowType.Hamming);
                        fourier1.SamplingWindowStep = (uint)preferences.nudSampleAdvance.Value;
                        fourier1.Order = (int)preferences.FourierOrderNumericUpDown.Value;

                        SetSignalGeneratorSignalType((String)preferences.LocalOscillatorComboBox.SelectedItem??"");

                        uint uval = 1024;
                        uint.TryParse((string)preferences.BufferSizeComboBox.SelectedItem, out uval);
                        AudioSource.AudioFormat.BufferSize = uval;
                        uval = 24;
                        uint.TryParse((string)preferences.cbxWordSize.SelectedItem, out uval);
                        AudioSource.AudioFormat.Bits = uval;
                        uval = 192000;
                        uint.TryParse((string)preferences.cbxSampleRate.SelectedItem, out uval);
                        AudioSource.AudioFormat.SampleRate = uval;
                        this.gpsMain = preferences.gpsAccess;
                        if (gpsMain.isRunning)
                        {
                            GPSPositionLabel.Text = gpsMain.getPosition();
                        }
                        else
                        {
                            GPSPositionLabel.Text = "GPS Off";
                        }

                    }
                    
                }
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(filePath + fileTemplate + "ERROR.txt",DateTime.Now.ToString()+ex.Message + ex.StackTrace);
                }
                catch (Exception) { }
            }
            finally
            {
                AudioSource.Enabled = true;
                waterfall1.Focus();
            }
        }

        private void DevicesToolStripButton_Click(object sender, EventArgs e)
        {

            try
            {
                AudioSource.Enabled = false;

                AudioSource.Device.ShowDeviceSelctDialog();
                AudioSource.Start();
                File.AppendAllText(logFileName, @"
SR6 Bat Recording Log File
 " + AudioSource.Device.DeviceName + " " + (int)AudioSource.AudioFormat.SampleRate + @"ksps " + (int)AudioSource.AudioFormat.Bits + @" bits
" + DateTime.Now.ToShortDateString() + " started at " + DateTime.Now.ToShortTimeString() + @"

");
                
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(filePath + fileTemplate + "ERROR.txt",DateTime.Now.ToString()+ex.Message + ex.StackTrace);
                }
                catch (Exception) { }
            }
            finally
            {
                SetDeviceLabel();
                AudioSource.Enabled = true;
                waterfall1.Focus();
            }
        }

        

        private enum BAT_DETECTOR { ON, OFF };
        private bool BatDetectorForced = false;

        

        private void SpeakersToolStripButton_Click(object sender, EventArgs e)
        {

            try
            {
                AudioSource.Enabled = false;
                audioOut1.Enabled = false;

                audioOut1.Device.ShowDeviceSelctDialog();


                audioOut1.Enabled = true;
                AudioSource.Enabled = true;
                waterfall1.Focus();
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(filePath + fileTemplate + "ERROR.txt",DateTime.Now.ToString()+ex.Message + ex.StackTrace);
                }
                catch (Exception) { }
                finally
                {
                    audioOut1.Enabled = true;
                    AudioSource.Enabled = true;
                    waterfall1.Focus();
                }
            }
        }

        private void HelpToolStripButton_Click(object sender, EventArgs e)
        {
            try
            {
                AudioSource.Enabled = false;
                using (HelpForm helpForm = new HelpForm())
                {
                    helpForm.ShowDialog();
                }
            }
            catch (Exception ex) { File.AppendAllText(filePath + fileTemplate + "ERROR.txt",DateTime.Now.ToString()+ex.Message + ex.StackTrace); }
            finally
            {
                AudioSource.Enabled = true;
                waterfall1.Focus();
            }

        }

        private void ToggleFilterToolStripButton_Click(object sender, EventArgs e)
        {
            try
            {
                AudioSource.Enabled = false;
                if (iir1.Enabled)
                {
                    iir1.OutputPin.Disconnect();
                    multiply1.OutputPin.Disconnect();
                    iir1.Enabled = false;
                    multiply1.OutputPin.Connect(realToAudio1.InputPins[0]);
                    ToggleFilterToolStripButton.BackColor = Color.Pink;
                }
                else
                {
                    multiply1.OutputPin.Disconnect();
                    multiply1.OutputPin.Connect(iir1.InputPin);
                    iir1.OutputPin.Connect(realToAudio1.InputPins[0]);
                    iir1.Enabled = true;
                    ToggleFilterToolStripButton.BackColor = Color.LimeGreen;
                }
            }
            catch (Exception ex) { File.AppendAllText(filePath + fileTemplate + "ERROR.txt",DateTime.Now.ToString()+ex.Message + ex.StackTrace); }
            finally
            {
                SetFilterLabel();
                AudioSource.Enabled = true;
                waterfall1.Focus();
            }
        }

        private void SetFilterLabel()
        {
            if (iir1.Enabled)
            {
                lblFilterState.Text = "FIR Filter Enabled";
            }
            else
            {
                lblFilterState.Text = "FIR Filter Disabled";
            }
        }

        private void BatDetectorTSButton_Click(object sender, EventArgs e)
        {
            if (BatDetectorTSButton.Text == "Force Bat Detector")
            {
                BatDetectorForced = true;
                BatDetector(BAT_DETECTOR.ON);
                BatDetectorTSButton.Text = "BD Off When Recording";
            }else{
                BatDetectorForced=false;
                BatDetector(BAT_DETECTOR.ON);
                BatDetectorTSButton.Text="Force Bat Detector";
            }
            SetBatDetectorLabel();
        }

        

        

        

        

        
    }
}
