using System;
using System.Diagnostics;
using System.Timers;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
using System.Collections.Generic;

namespace Z
{
    public partial class Form1 : Form
    {
        InstalledApplicationList UserApplications = new InstalledApplicationList();
        byte[] bLevels; //array of valid level values

        public Form1()
        {
            InitializeComponent();

            float minutes = 0.1F;
            int Interval = (int)(minutes * 60 * 1000);

            System.Timers.Timer ProcessTimer = new System.Timers.Timer();
            ProcessTimer.Elapsed += new ElapsedEventHandler(ProcessData);
            ProcessTimer.Interval = Interval;
            ProcessTimer.Start();

            // attach event handler for Click event 
            // (assuming ButtonClickHandler is an existing method in the class)
            
            dataGridView1.DefaultCellStyle.SelectionBackColor = dataGridView1.DefaultCellStyle.BackColor;
            dataGridView1.DefaultCellStyle.SelectionForeColor = dataGridView1.DefaultCellStyle.ForeColor;
            dataGridView1.CellClick += new DataGridViewCellEventHandler(dataGridView1_CellClick);
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            UserApplications.StartApplication(dataGridView1.Rows[e.RowIndex].Cells[0].Value as string);
        }

        private void ProcessData(object source, ElapsedEventArgs e)
        {
            LearningTools.ProcessVolume(volume_mode.Checked);
            bLevels = GetBrightnessLevels();
            //textBox2.Text = GetBrightness().ToString();
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
                int vol = int.Parse(textBox1.Text);
                SetVolume(vol);
            }
            catch
            {
                SetVolume(50);
            }
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            int iPercent;
            try
            {
                iPercent = Convert.ToInt16(textBox2.Text);
            }
            catch
            {
                iPercent = 50;
            }
            if (iPercent >= 0 && iPercent <= bLevels[bLevels.Count() - 1])
            {
                byte level = 100;
                foreach (byte item in bLevels)
                {
                    if (item >= iPercent)
                    {
                        level = item;
                        break;
                    }
                }
                SetBrightness(level);
            }
            else
            {
                textBox2.Text = "Something happened!";
            }
        }

        //get the actual percentage of brightness
        static int GetBrightness()
        {
            //define scope (namespace)
            ManagementScope s = new ManagementScope("root\\WMI");

            //define query
            SelectQuery q = new SelectQuery("WmiMonitorBrightness");

            //output current brightness
            ManagementObjectSearcher mos = new ManagementObjectSearcher(s, q);

            ManagementObjectCollection moc = mos.Get();

            //store result
            byte curBrightness = 0;
            foreach (ManagementObject o in moc)
            {
                curBrightness = (byte)o.GetPropertyValue("CurrentBrightness");
                break; //only work on the first object
            }

            moc.Dispose();
            mos.Dispose();

            return (int)curBrightness;
        }

        //array of valid brightness values in percent
        static byte[] GetBrightnessLevels()
        {
            //define scope (namespace)
            ManagementScope s = new ManagementScope("root\\WMI");

            //define query
            SelectQuery q = new SelectQuery("WmiMonitorBrightness");

            //output current brightness
            ManagementObjectSearcher mos = new ManagementObjectSearcher(s, q);
            byte[] BrightnessLevels = new byte[0];

            try
            {
                ManagementObjectCollection moc = mos.Get();

                //store result


                foreach (ManagementObject o in moc)
                {
                    BrightnessLevels = (byte[])o.GetPropertyValue("Level");
                    break; //only work on the first object
                }

                moc.Dispose();
                mos.Dispose();

            }
            catch (Exception)
            {
                Debug.WriteLine("Sorry, Your System does not support this brightness control...");
            }

            return BrightnessLevels;
        }

        static void SetBrightness(byte targetBrightness)
        {
            //define scope (namespace)
            ManagementScope s = new ManagementScope("root\\WMI");

            //define query
            SelectQuery q = new SelectQuery("WmiMonitorBrightnessMethods");

            //output current brightness
            ManagementObjectSearcher mos = new ManagementObjectSearcher(s, q);

            ManagementObjectCollection moc = mos.Get();

            foreach (ManagementObject o in moc)
            {
                o.InvokeMethod("WmiSetBrightness", new Object[] { uint.MaxValue, targetBrightness }); //note the reversed order - won't work otherwise!
                break; //only work on the first object
            }

            moc.Dispose();
            mos.Dispose();
        }

        #region DLL Imports

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleBaseName(IntPtr hWnd, IntPtr hModule, StringBuilder lpFileName, int nSize);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleFileNameEx(IntPtr hWnd, IntPtr hModule, StringBuilder lpFileName, int nSize);

        #endregion

        public static string GetTopWindowText()
        {
            IntPtr hWnd = GetForegroundWindow();
            int length = GetWindowTextLength(hWnd);
            StringBuilder text = new StringBuilder(length + 1);
            GetWindowText(hWnd, text, text.Capacity);
            return text.ToString();
        }

        public static string GetTopWindowName()
        {
            IntPtr hWnd = GetForegroundWindow();
            uint lpdwProcessId;
            GetWindowThreadProcessId(hWnd, out lpdwProcessId);

            IntPtr hProcess = OpenProcess(0x0410, false, lpdwProcessId);

            StringBuilder text = new StringBuilder(1000);
            StringBuilder text2 = new StringBuilder(1000);
            GetModuleBaseName(hProcess, IntPtr.Zero, text, text.Capacity);
            GetModuleFileNameEx(hProcess, IntPtr.Zero, text2, text2.Capacity);

            CloseHandle(hProcess);

            return text.ToString() + " # " + text2.ToString();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var x = Process.GetProcesses();

            System.Timers.Timer myTimer = new System.Timers.Timer();
            myTimer.Elapsed += new ElapsedEventHandler(DisplayTopWindow);
            myTimer.Interval = 1000; // ms
            myTimer.Start();
        }

        public static void DisplayTopWindow(object source, ElapsedEventArgs e)
        {
            // will run every second
            Console.WriteLine(GetTopWindowName());
            Console.WriteLine(GetTopWindowText());
        }

        private void application_searcher_TextChanged(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();

            if (application_searcher.Text == "")
            {
                /*
                * Sort by prediction
                * 
                */
            }
            else
            {
                List<string> Applications = UserApplications.SearchApplications(application_searcher.Text);

                foreach (string ApplicationName in Applications)
                {
                    dataGridView1.Rows.Add();
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[0].Value = ApplicationName;
                }
            }
        }
    }
}
