using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

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

    class VolumeHistory
    {
        public SortedSet<VolumeData> VolumeDataSet = new SortedSet<VolumeData>(Comparer<VolumeData>.Create((x, y) => y.TimeStamp.CompareTo(x.TimeStamp)));
        VolumeData LastUsedVolumeData;
        string FileName = "volume_data.dat";

        public VolumeHistory()
        {
            if (File.Exists(FileName))
            {
                ReadFromFile();
            }
            else
            {
                File.Create(FileName);
            }
        }

        public void WriteToFile()
        {
            List<VolumeData> VolumeDataList = VolumeDataSet.ToList();
            string text = JsonConvert.SerializeObject(VolumeDataList.ToArray());
            File.WriteAllText(FileName, text);
        }

        public void ReadFromFile()
        {
            string text = File.ReadAllText(FileName);
            List<VolumeData> VolumeDataList = JsonConvert.DeserializeObject<List<VolumeData>>(text);
            
            foreach (VolumeData Item in VolumeDataList)
            {
                VolumeDataSet.Add(Item);
            }

            LastUsedVolumeData = VolumeDataSet.Min;
        }
    }
}
