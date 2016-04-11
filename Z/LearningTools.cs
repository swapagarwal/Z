using System;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using System.IO;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.UI.DataVisualization.Charting;
using System.Net;
using System.Net.NetworkInformation;

namespace Z
{
    static class LearningTools
    {
        private static VolumeModel VolumeData = new VolumeModel();
        private static BrightnessModel BrightnessData = new BrightnessModel();
        private static byte[] bLevels = GetBrightnessLevels();
        private static ApplicationModel ApplicationData = new ApplicationModel();
        private static string LastUsedApplication = "";
        private static string SecondLastUsedApplication = "";
        private static InstalledApplicationList UserApplications = new InstalledApplicationList();

        public static ApplicationInstance GetApplicationSnapShot()
        {
            ApplicationInstance ApplicationSnapShot = new ApplicationInstance();

            ApplicationSnapShot.LastUsedApplication = LastUsedApplication;
            ApplicationSnapShot.SecondLastUsedApplication = SecondLastUsedApplication;
            ApplicationSnapShot.NetworkStatus = BasicTools.CheckNetworkStatus();
            ApplicationSnapShot.PluggedInStatus = BasicTools.CheckPluggedIn();
            ApplicationSnapShot.TimeStamp = DateTime.Now;
            ApplicationSnapShot.ProcessList = BasicTools.GetProcessList();

            return ApplicationSnapShot;
        }

        public static List<KeyValuePair<string, double>> GetApplicationPredictions()
        {
            return ApplicationData.PredictApplications(GetApplicationSnapShot());
        }

        public static void ProcessApplication(string ApplicationName, List<KeyValuePair<string, double>> DemoteList)
        {
            ApplicationInstance ApplicationSnapshot = GetApplicationSnapShot();
            ApplicationData.AddApplicationInstance(ApplicationName, ApplicationSnapshot);

            if (DemoteList.Count > 0)
            {
                // ApplicationData.ReinforcedLearning(ApplicationName, DemoteList, ApplicationSnapshot);
            }

            UserApplications.StartApplication(ApplicationName);
            SecondLastUsedApplication = LastUsedApplication;
            LastUsedApplication = ApplicationName;
        }

        public static VolumeInstance GetVolumeSnapshot()
        {
            VolumeInstance Data = new VolumeInstance();

            MMDeviceEnumerator MMDE = new MMDeviceEnumerator();
            MMDevice DefaultDevice = MMDE.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            Data.DeviceName = DefaultDevice.FriendlyName;
            Data.MasterVolume = DefaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
            Data.TimeStamp = DateTime.Now;

            if (DefaultDevice.AudioMeterInformation.MasterPeakValue > 0)
            {
                for (int i = 0; i < DefaultDevice.AudioSessionManager.Sessions.Count; i++)
                {
                    var Session = DefaultDevice.AudioSessionManager.Sessions[i];

                    if (Session.AudioMeterInformation.MasterPeakValue > 0)
                    {
                        ApplicationVolume SessionInfo = new ApplicationVolume();
                        Process ApplicationProcess = Process.GetProcessById((int)Session.GetProcessID);
                        SessionInfo.ApplicationName = ApplicationProcess.ProcessName;
                        SessionInfo.Volume = Session.SimpleAudioVolume.Volume;
                        Data.Applications.Add(SessionInfo);
                    }
                }
            }

            return Data;
        }

        public static void SetVolume(VolumeInstance Data)
        {
            MMDeviceEnumerator MMDE = new MMDeviceEnumerator();
            MMDevice DefaultDevice = MMDE.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            Debug.Assert(Data.DeviceName == DefaultDevice.FriendlyName);
            DefaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = (float)Data.MasterVolume;
            Data.TimeStamp = DateTime.Now;

            if (DefaultDevice.AudioMeterInformation.MasterPeakValue > 0)
            {
                for (int i = 0; i < DefaultDevice.AudioSessionManager.Sessions.Count; i++)
                {
                    var Session = DefaultDevice.AudioSessionManager.Sessions[i];
                    Process ApplicationProcess = Process.GetProcessById((int)Session.GetProcessID);
                    string ApplicationName = ApplicationProcess.ProcessName;

                    foreach (ApplicationVolume Application in Data.Applications)
                    {
                        if (ApplicationName == Application.ApplicationName)
                        {
                            Session.SimpleAudioVolume.Volume = (float)Application.Volume;
                            break;
                        }
                    }
                }
            }
        }
        
        public static void ProcessVolume(bool AutomaticVolumeMode)
        {
            VolumeInstance VolumeSnapshot = GetVolumeSnapshot();

            if (AutomaticVolumeMode)
            {
                SetVolume(VolumeData.GetVolume(VolumeSnapshot));
            }

            VolumeData.AddVolume(VolumeSnapshot);
        }

        public static ApplicationBrightness GetBrightnessSnapshot()
        {
            ApplicationBrightness Data = new ApplicationBrightness();
            Data.ApplicationName = BasicTools.GetTopWindowName();
            Data.Brightness = GetBrightness();
            Data.TimeStamp = DateTime.Now;
            Data.Weight = 1.0;
            return Data;
        }

        public static void SetBrightness(ApplicationBrightness Data)
        {
            Data.TimeStamp = DateTime.Now;
            if (Data.ApplicationName == BasicTools.GetTopWindowName())
            {
                int iPercent = Convert.ToInt16(Data.Brightness);
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
                    Debug.WriteLine("Something happened!");
                }
            }
        }

        public static void ProcessBrightness(bool AutomaticBrightnessMode)
        {
            ApplicationBrightness BrightnessSnapshot = GetBrightnessSnapshot();
            if (AutomaticBrightnessMode)
            {
                SetBrightness(BrightnessData.GetBrightness(BrightnessSnapshot));
            }
            BrightnessData.AddBrightness(BrightnessSnapshot);
        }

        public static byte[] GetBrightnessLevels()
        {
            byte[] BrightnessLevels = new byte[0];

            try
            {
                ManagementScope s = new ManagementScope("root\\WMI");
                SelectQuery q = new SelectQuery("WmiMonitorBrightness");
                ManagementObjectSearcher mos = new ManagementObjectSearcher(s, q);
                
                ManagementObjectCollection moc = mos.Get();
                foreach (ManagementObject o in moc)
                {
                    BrightnessLevels = (byte[])o.GetPropertyValue("Level");
                    break;
                }
                moc.Dispose();
                mos.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return BrightnessLevels;
        }

        public static int GetBrightness()
        {
            try
            {
                ManagementScope s = new ManagementScope("root\\WMI");
                SelectQuery q = new SelectQuery("WmiMonitorBrightness");
                ManagementObjectSearcher mos = new ManagementObjectSearcher(s, q);
                ManagementObjectCollection moc = mos.Get();
                byte curBrightness = 0;
                foreach (ManagementObject o in moc)
                {
                    curBrightness = (byte)o.GetPropertyValue("CurrentBrightness");
                    break;
                }
                moc.Dispose();
                mos.Dispose();
                return curBrightness;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return 0;
            }
        }

        public static void SetBrightness(byte targetBrightness)
        {
            try
            {
                ManagementScope s = new ManagementScope("root\\WMI");
                SelectQuery q = new SelectQuery("WmiMonitorBrightnessMethods");
                ManagementObjectSearcher mos = new ManagementObjectSearcher(s, q);
                ManagementObjectCollection moc = mos.Get();
                foreach (ManagementObject o in moc)
                {
                    o.InvokeMethod("WmiSetBrightness", new Object[] { uint.MaxValue, targetBrightness });
                    break;
                }
                moc.Dispose();
                mos.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

    static class BasicTools
    {
        public static string FolderPath = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Z\\");

        public static bool CheckNetworkStatus()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        public static bool CheckPluggedIn()
        {
            return (SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online);
        }

        public static List<string> GetProcessList()
        {
            List<string> ProcessList = new List<string>();

            /*
            *  Complete This
            */

            return ProcessList;
        }

        public static void RecursiveDirectoryrSearch(string DirectoryPath, ref List<string> FileList)
        {
            try
            {
                foreach (string FilePath in Directory.GetFiles(DirectoryPath))
                {
                    FileList.Add(FilePath);
                }

                foreach (string FolderPath in Directory.GetDirectories(DirectoryPath))
                {
                    RecursiveDirectoryrSearch(FolderPath, ref FileList);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void LaunchApplication(string ApplicationPath)
        {
            Process proc = new Process();
            Console.WriteLine(ApplicationPath);
            proc.StartInfo.FileName = ApplicationPath;
            proc.Start();
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
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);

            Process TopWindowProcess = Process.GetProcessById((int)pid);

            try
            {
                return TopWindowProcess.MainModule.FileVersionInfo.FileDescription;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return "";
            }
            
        }

        public static void WriteToFile(string FileName, object Object)
        {
            string FilePath = FolderPath + FileName;
            Directory.CreateDirectory(FolderPath);

            Stream FileStream = File.Open(FilePath, FileMode.Create);
            BinaryFormatter Serializer = new BinaryFormatter();
            Serializer.Serialize(FileStream, Object);
            FileStream.Close();
        }

        public static object ReadFromFile(string FileName)
        {
            string FilePath = FolderPath + FileName;
            Directory.CreateDirectory(FolderPath);
            object Object;

            Stream FileStream = File.Open(FilePath, FileMode.OpenOrCreate);
            try
            {

                BinaryFormatter Serializer = new BinaryFormatter();
                Object = Serializer.Deserialize(FileStream);
                FileStream.Close();
                return Object;
            }
            catch (Exception ex)
            {
                FileStream.Close();
                throw ex;
            }
        }

        public static void CreateChart(Dictionary<string, int> PlotData, string FileName = "data.png")
        {
            string FilePath = Environment.ExpandEnvironmentVariables(FolderPath + FileName);
            Chart chart = new Chart();
            chart.ChartAreas.Add(new ChartArea());
            
            List<string> Labels = PlotData.Keys.ToList();
            List<int> DataCount = PlotData.Values.ToList();


            chart.Series.Add(new Series("Data"));
            chart.Series["Data"].ChartType = SeriesChartType.Pie;
            chart.Series["Data"]["PieLabelStyle"] = "Outside";
            chart.Series["Data"]["PieLineColor"] = "Black";
            chart.Series["Data"].Points.DataBindXY(Labels, DataCount);

            MemoryStream ms = new MemoryStream();
            chart.SaveImage(ms, ChartImageFormat.Png);
            FileStream f = new FileStream(FilePath, FileMode.Create);
            ms.WriteTo(f);
            f.Close();
            ms.Close();
        }
    }
}
