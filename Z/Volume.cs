using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;

namespace Z
{
    [Serializable()]
    class ApplicationVolume
    {
        public string ApplicationName;
        public double Volume;

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

        public ApplicationVolume DeepCopy()
        {
            return MemberwiseClone() as ApplicationVolume;
        }
    }

    [Serializable()]
    class VolumeInstance
    {
        public DateTime TimeStamp = DateTime.MinValue;
        public string DeviceName;
        public double MasterVolume;
        public double Weight = 1.0;
        public List<ApplicationVolume> Applications = new List<ApplicationVolume>();
        
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

        public VolumeInstance DeepCopy()
        {
            VolumeInstance Item = MemberwiseClone() as VolumeInstance;
            Item.Applications = new List<ApplicationVolume>();

            foreach (ApplicationVolume App in Applications)
            {
                Item.Applications.Add(App.DeepCopy());
            }

            return Item;
        }
    }

    [Serializable()]
    class VolumeInstances
    {
        private List<VolumeInstance> VolumeInstanceList = new List<VolumeInstance>();
        private VolumeInstance LastUsedVolumeData = new VolumeInstance();
        private bool Dirty = false;
        private DateTime LastUserActivity = DateTime.MinValue;
        private static int Threshold = 1 * 60;

        private void ReinforcedLearning(VolumeInstance Item)
        {
            List<double> Factors = new List<double>();

            foreach(VolumeInstance Instance in VolumeInstanceList)
            {
                double val = Math.Abs(Instance.TimeStamp.TimeOfDay.Subtract(Item.TimeStamp.TimeOfDay).TotalSeconds);
                Factors.Add(Math.Pow(Math.Max(1, Math.Min(val, 86400 - val)), 2));
            }

            double ratio = 144.0 / Factors.Max();
            Factors = Factors.Select(x => Math.Exp(-x * ratio)).ToList();

            double MaxWeight = 0;
            int i = 0;
            foreach (VolumeInstance Instance in VolumeInstanceList)
            {
                Instance.Weight -= Instance.Weight * Math.Abs(Instance.MasterVolume - Item.MasterVolume) * Factors[i];
                MaxWeight = Math.Max(MaxWeight, Instance.Weight);
                i++;
            }

            if (MaxWeight > 0)
            {
                foreach (VolumeInstance Instance in VolumeInstanceList)
                {
                    Instance.Weight = Instance.Weight / MaxWeight;
                }
            }
        }

        private List<double> GetTimeOfDayDifferenceWeights(VolumeInstance Item)
        {
            List<double> Weights = new List<double>();

            foreach (VolumeInstance Instance in VolumeInstanceList)
            {
                double val = Math.Abs(Instance.TimeStamp.TimeOfDay.Subtract(Item.TimeStamp.TimeOfDay).TotalSeconds);
                Weights.Add(1.0 / (Math.Max(1, Math.Min(val, 86400 - val))));
            }

            double ratio = 1.0 / Weights.Max();
            Weights = Weights.Select(x => x * ratio).ToList();
            return Weights;
        }

        private List<double> GetTimeDifferenceWeights(VolumeInstance Item)
        {
            List<double> Weights = new List<double>();

            foreach (VolumeInstance Instance in VolumeInstanceList)
            {
                double val = Math.Abs(Item.TimeStamp.Subtract(Instance.TimeStamp).TotalSeconds);
                Weights.Add(val);
            }

            Weights = Weights.Select(x => 1.0/Math.Log(Math.Max(Math.E, x))).ToList();
            double ratio = 1.0 / Weights.Max();
            Weights = Weights.Select(x => x * ratio).ToList();

            return Weights;
        }

        private void RecalculateWeights(VolumeInstance Item)
        {
            ReinforcedLearning(Item);
        }
        
        public void AddVolume(VolumeInstance Item)
        {
            VolumeInstanceList.Add(Item);

            if (Dirty && !LastUsedVolumeData.ExactlySame(Item))
            {
                RecalculateWeights(Item);
            }

            LastUserActivity = DateTime.Now;
            LastUsedVolumeData = Item;
            Dirty = false;
        }

        public VolumeInstance GetVolume(VolumeInstance Item)
        {
            if (Math.Abs((Item.TimeStamp - LastUserActivity).TotalSeconds) < Threshold )
            {
                return Item;
            }

            VolumeInstance Data = Item.DeepCopy();
            Data.MasterVolume = 0;

            foreach (ApplicationVolume App in Data.Applications)
            {
                App.Volume = 0;
            }

            List<double> TimeWeights = GetTimeDifferenceWeights(Item);
            List<double> TimeOfDayWeights = GetTimeOfDayDifferenceWeights(Item);
            List<double> NetWeights = new List<double>();

            int i = 0;
            foreach (VolumeInstance Instance in VolumeInstanceList)
            {
                NetWeights.Add(Instance.Weight * TimeWeights[i] * TimeOfDayWeights[i]);
                i++;
            }

            double TotalWeight = NetWeights.Sum();
            NetWeights = NetWeights.Select(x => x / TotalWeight).ToList();

            i = 0;
            foreach (VolumeInstance Instance in VolumeInstanceList)
            {
                Data.MasterVolume += Instance.MasterVolume * NetWeights[i];

                int j = 0;
                foreach(ApplicationVolume App in Instance.Applications)
                {
                    Data.Applications[j].Volume += App.Volume * NetWeights[i];
                    j++;
                }

                i++;
            }
            
            Dirty = true;
            LastUsedVolumeData = Data;
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
            }
            catch (Exception ex)
            {
                VolumeHistory = new Dictionary<string, VolumeInstances>();
            }
            finally
            {
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

            if (!VolumeHistory.ContainsKey(Key))
            {
                return Item;
            }

            VolumeInstance Data = VolumeHistory[Key].GetVolume(Item);
            WriteToFile();
            return Data;
        }
    }
}
