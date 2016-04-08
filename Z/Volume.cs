using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

namespace Z
{
    [Serializable()]
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

    [Serializable()]
    class VolumeInstance
    {
        public DateTime TimeStamp = DateTime.MinValue;
        public string DeviceName;
        public float MasterVolume;
        public HashSet<ApplicationVolume> Applications = new HashSet<ApplicationVolume>();
        
        public string GetApplicationString()
        {
            string ApplicationString = "";

            List<ApplicationVolume> AppList = Applications.OrderBy(o => o.ApplicationName).ToList();

            foreach (ApplicationVolume App in AppList)
            {
                ApplicationString += App.ApplicationName + ", ";
            }

            return ApplicationString;
        }

        public bool ExactlySame(VolumeInstance x)
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

                List<ApplicationVolume> a = Applications.OrderBy(o=>o.ApplicationName).ToList();
                List<ApplicationVolume> b = x.Applications.OrderBy(o => o.ApplicationName).ToList();

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

    [Serializable()]
    class VolumeInstances
    {
        private List<VolumeInstance> VolumeInstanceList = new List<VolumeInstance>();
        private bool Dirty = false;
        private VolumeInstance LastUsedVolumeData = new VolumeInstance();

        public void AddVolume(VolumeInstance Item)
        {
            if (Dirty || !Item.ExactlySame(LastUsedVolumeData))
            {
                Dirty = false;
                VolumeInstanceList.Add(Item);
                LastUsedVolumeData = Item;
            }
        }

        public VolumeInstance GetVolume(VolumeInstance Item)
        {
            VolumeInstance Data = new VolumeInstance();

            /*
            * Complete this
            *
            */

            Dirty = true;
            return Data;
        }
    }

    class VolumeModel
    {
        private Dictionary<string, VolumeInstances> VolumeHistory = new Dictionary<string, VolumeInstances>();        
        public string FileName = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\volume_data.dat");

        public VolumeModel()
        {
            ReadFromFile();
        }

        public void WriteToFile()
        {
            Stream FileStream = File.Open(FileName, FileMode.Truncate);
            BinaryFormatter Serializer = new BinaryFormatter();
            Serializer.Serialize(FileStream, VolumeHistory);
            FileStream.Close();
        }

        public void ReadFromFile()
        {
            Stream FileStream = File.Open(FileName, FileMode.OpenOrCreate);
            try
            {
                
                BinaryFormatter Serializer = new BinaryFormatter();
                VolumeHistory = Serializer.Deserialize(FileStream) as Dictionary<string, VolumeInstances>;
                FileStream.Close();
            }
            catch (Exception ex)
            {
                VolumeHistory = new Dictionary<string, VolumeInstances>();
                FileStream.Close();
            }
        }

        private string GetKey(VolumeInstance Item)
        {
            string Key = "";

            Key += Item.DeviceName + ", ";
            Key += Item.GetApplicationString();

            return Key;
        }

        public void AddVolume(VolumeInstance Item)
        {
            string Key = GetKey(Item);

            if(!VolumeHistory.ContainsKey(Key))
            {
                VolumeHistory.Add(Key, new VolumeInstances());
            }

            VolumeHistory[Key].AddVolume(Item);
            WriteToFile();
        }

        public VolumeInstance GetVolume(VolumeInstance Item)
        {
            string Key = GetKey(Item);
            VolumeInstance Data = VolumeHistory[Key].GetVolume(Item);
            WriteToFile();
            return Data;
        }
    }
}
