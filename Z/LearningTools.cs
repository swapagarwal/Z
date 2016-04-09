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

namespace Z
{
    static class LearningTools
    {
        private static VolumeModel VolumeData = new VolumeModel();
        private static BrightnessModel BrightnessData = new BrightnessModel();
        private static byte[] bLevels = GetBrightnessLevels();
        
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

            Debug.Assert(DefaultDevice.AudioMeterInformation.MasterPeakValue > 0);

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
                    MessageBox.Show("Something happened!");
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
            ManagementScope s = new ManagementScope("root\\WMI");
            SelectQuery q = new SelectQuery("WmiMonitorBrightness");
            ManagementObjectSearcher mos = new ManagementObjectSearcher(s, q);
            byte[] BrightnessLevels = new byte[0];
            try
            {
                ManagementObjectCollection moc = mos.Get();
                foreach (ManagementObject o in moc)
                {
                    BrightnessLevels = (byte[])o.GetPropertyValue("Level");
                    break;
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

        public static int GetBrightness()
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
            return (int)curBrightness;
        }

        public static void SetBrightness(byte targetBrightness)
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
    }

    static class BasicTools
    {
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
            catch (Exception excpt)
            {
                Debug.WriteLine(excpt.Message);
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
            uint lpdwProcessId;
            GetWindowThreadProcessId(hWnd, out lpdwProcessId);

            IntPtr hProcess = OpenProcess(0x0410, false, lpdwProcessId);

            StringBuilder text = new StringBuilder(1000);
            //StringBuilder text2 = new StringBuilder(1000);
            GetModuleBaseName(hProcess, IntPtr.Zero, text, text.Capacity);
            //GetModuleFileNameEx(hProcess, IntPtr.Zero, text2, text2.Capacity);

            CloseHandle(hProcess);

            return text.ToString();// + " # " + text2.ToString();
        }
    }
}
