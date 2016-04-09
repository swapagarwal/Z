﻿using System;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using System.IO;
using System.Collections.Generic;
using System.Management;

namespace Z
{
    static class LearningTools
    {
        private static VolumeModel VolumeData = new VolumeModel();
        
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
                        SessionInfo.ApplicationName = ApplicationProcess.MainWindowTitle;
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
                string ApplicationName = ApplicationProcess.MainWindowTitle;

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
    }
}
