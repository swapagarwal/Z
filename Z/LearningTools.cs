﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

namespace Z
{
    class LearningTools
    {
        public static VolumeData GetVolumeSnapshot()
        {
            VolumeData Data = new VolumeData();

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

        public static void SetVolume(VolumeData Data)
        {
            MMDeviceEnumerator MMDE = new MMDeviceEnumerator();
            MMDevice DefaultDevice = MMDE.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            Debug.Assert(Data.DeviceName == DefaultDevice.FriendlyName);
            DefaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = Data.MasterVolume;
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
                        Session.SimpleAudioVolume.Volume = Application.Volume;
                        break;
                    }
                }
            }
        }
    }
}