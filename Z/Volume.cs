using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace Z
{
    class ApplicationVolume
    {
        public string ApplicationName;
        public float Volume;

        public bool ExactlySame(ApplicationVolume x)
        {
            if (x == null)
            {
                return false;
            }

            if (ApplicationName == x.ApplicationName && Volume == x.Volume)
            {
                return true;
            }

            return false;
        }
    }

    class Volume
    {
        public DateTime TimeStamp = DateTime.MinValue;
        public string DeviceName;
        public float MasterVolume;
        public HashSet<ApplicationVolume> Applications = new HashSet<ApplicationVolume>();
        
        public bool ExactlySame(Volume x)
        {
            if (x == null)
            {
                return false;
            }

            if (DeviceName == x.DeviceName && MasterVolume == x.MasterVolume)
            {
                if (Applications.Count != x.Applications.Count)
                {
                    return false;
                }

                List<ApplicationVolume> a = Applications.ToList();
                List<ApplicationVolume> b = x.Applications.ToList();

                for (int i = 0; i < a.Count; i++)
                {
                    if (!a[i].ExactlySame(b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }

    class VolumeModel
    {
        private SortedSet<Volume> VolumeDataSet = new SortedSet<Volume>(Comparer<Volume>.Create((x, y) => y.TimeStamp.CompareTo(x.TimeStamp)));
        private Volume LastUsedVolumeData = new Volume();
        bool Dirty = false;
        string FileName = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\volume_data.dat");

        public VolumeModel()
        {
            if (File.Exists(FileName))
            {
                ReadFromFile();
            }
            else
            {
                File.Create(FileName).Close();
            }
        }

        public void WriteToFile()
        {
            List<Volume> VolumeDataList = VolumeDataSet.ToList();
            string text = JsonConvert.SerializeObject(VolumeDataList);
            File.WriteAllText(FileName, text);

            Debug.WriteLine(text);
        }

        public void ReadFromFile()
        {
            string text = File.ReadAllText(FileName);
            List<Volume> VolumeDataList = JsonConvert.DeserializeObject<List<Volume>>(text);

            if (VolumeDataList != null)
            {
                foreach (Volume Item in VolumeDataList)
                {
                    VolumeDataSet.Add(Item);
                }

                LastUsedVolumeData = VolumeDataSet.Min;
            }
        }

        public void AddVolume(Volume Item)
        {
            if (Dirty || !Item.ExactlySame(LastUsedVolumeData))
            {
                Dirty = false;
                VolumeDataSet.Add(Item);
                LastUsedVolumeData = Item;
                WriteToFile();
            }
        }

        public Volume GetVolume(Volume Item)
        {
            Volume Data = new Volume();

            /*
            * Complete this
            *
            */

            Dirty = true;
            return Data;
        }
    }
}
