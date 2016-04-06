using System;
using System.Diagnostics;
using System.Timers;
using System.Windows.Forms;
using System.Management;

namespace Z
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
            int Interval = 5 * 60 * 1000;

            System.Timers.Timer ProcessTimer = new System.Timers.Timer();
            ProcessTimer.Elapsed += new ElapsedEventHandler(ProcessData);
            ProcessTimer.Interval = Interval;
            ProcessTimer.Start();
        }

        private void ProcessData(object source, ElapsedEventArgs e)
        {
            LearningTools.ProcessVolume();
        }

        private void SetVolume(int level)
        {
            try
            {
                //Instantiate an Enumerator to find audio devices
                NAudio.CoreAudioApi.MMDeviceEnumerator MMDE = new NAudio.CoreAudioApi.MMDeviceEnumerator();

                //Get all the devices, no matter what condition or status
                NAudio.CoreAudioApi.MMDeviceCollection DevCol = MMDE.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.All, NAudio.CoreAudioApi.DeviceState.All);
                
                //Loop through all devices
                foreach (NAudio.CoreAudioApi.MMDevice dev in DevCol)
                {
                    try
                    {
                        if (dev.State == NAudio.CoreAudioApi.DeviceState.Active)
                        {
                            var newVolume = (float)Math.Max(Math.Min(level, 100), 0) / (float)100;

                            //Set at maximum volume
                            dev.AudioEndpointVolume.MasterVolumeLevelScalar = newVolume;

                            dev.AudioEndpointVolume.Mute = level == 0;

                            //Get its audio volume
                            //_log.Info("Volume of " + dev.FriendlyName + " is " + dev.AudioEndpointVolume.MasterVolumeLevelScalar.ToString());
                        }
                        else
                        {
                            //_log.Debug("Ignoring device " + dev.FriendlyName + " with state " + dev.State);
                        }
                    }
                    catch (Exception ex)
                    {
                        //Do something with exception when an audio endpoint could not be muted
                        //_log.Warn(dev.FriendlyName + " could not be muted with error " + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                //When something happend that prevent us to iterate through the devices
                //_log.Warn("Could not enumerate devices due to an excepion: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int vol = Int32.Parse(textBox1.Text);
                SetVolume(vol);
            }
            catch
            {
                SetVolume(50);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // AlwaysRunning();
        }
    }
}
