using CSCore.CoreAudioAPI;
using System;
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
        public VolumeData GetVolumeSnapshot()
        {
            VolumeData Data = new VolumeData();

            NAudio.CoreAudioApi.MMDeviceEnumerator MMDE = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            NAudio.CoreAudioApi.MMDevice DefaultDevice = MMDE.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia);

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
                        Process Application = Process.GetProcessById((int)Session.GetProcessID);
                        SessionInfo.ApplicationName = Application.MainWindowTitle;
                        SessionInfo.Volume = Session.SimpleAudioVolume.Volume;
                        Data.Applications.Add(SessionInfo);
                    }
                }
            }

            return Data;
        }
    }
}
