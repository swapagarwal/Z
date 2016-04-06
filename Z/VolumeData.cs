using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;

namespace Z
{
    class ApplicationVolume
    {
        public string ApplicationName;
        public float Volume;
    }

    class VolumeData
    {
        public DateTime TimeStamp;
        public string DeviceName;
        public float MasterVolume;
        public List<ApplicationVolume> Applications = new List<ApplicationVolume>();
    }
}
