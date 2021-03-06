﻿using System;
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
        private static double MinTimeOfDayWeight = 0.001;
        private static double MaxTimeOfDayWeight = 1.0;
        private static double MinTimeWeight = 0.001;
        private static double MaxTimeWeight = 1.0;

        private void ReinforcedLearning(VolumeInstance Item)
        {
            List<double> Factors = new List<double>();

            foreach(VolumeInstance Instance in VolumeInstanceList)
            {
                double val = Math.Abs(Instance.TimeStamp.TimeOfDay.Subtract(Item.TimeStamp.TimeOfDay).TotalHours);
                Factors.Add(Math.Pow(Math.Min(val, 24 - val), 2));
            }

            Factors = Factors.Select(x => Math.Exp(-x)).ToList();

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
            // y = c - m * x
            double MaxTimeDifference = 12; // hours
            double slope = (MaxTimeOfDayWeight - MinTimeOfDayWeight) / MaxTimeDifference;

            foreach (VolumeInstance Instance in VolumeInstanceList)
            {
                double val = Math.Abs(Instance.TimeStamp.TimeOfDay.Subtract(Item.TimeStamp.TimeOfDay).TotalHours);
                Weights.Add(MaxTimeOfDayWeight - slope * Math.Min(val, 24 - val));
            }

            double ratio = 1.0 / Weights.Max();
            Weights = Weights.Select(x => x * ratio).ToList();
            Debug.Assert(Weights.TrueForAll(x => x > 0 && x <= 1));
            return Weights;
        }

        private List<double> GetTimeDifferenceWeights(VolumeInstance Item)
        {
            List<double> Weights = new List<double>();
            // y = c - m * x * x
            double MaxTimeDifference = 30; // days
            double slope = (MaxTimeWeight - MinTimeWeight) / Math.Pow(MaxTimeDifference, 2);

            foreach (VolumeInstance Instance in VolumeInstanceList)
            {
                double val = Math.Abs(Item.TimeStamp.Subtract(Instance.TimeStamp).TotalDays);
                Weights.Add(MaxTimeWeight - slope * Math.Pow(val, 2));
            }
            
            double ratio = 1.0 / Weights.Max();
            Weights = Weights.Select(x => x * ratio).ToList();
            Debug.Assert(Weights.TrueForAll(x => x > 0 && x <= 1));
            return Weights;
        }

        private void RecalculateWeights(VolumeInstance Item)
        {
            ReinforcedLearning(Item);
        }
        
        public void AddVolume(VolumeInstance Item)
        {
            if (Dirty && !LastUsedVolumeData.ExactlySame(Item))
            {
                VolumeInstanceList.Add(Item);
                RecalculateWeights(Item);
                Dirty = false;
            }
            else if (!Dirty)
            {
                VolumeInstanceList.Add(Item);
            }

            if (!LastUsedVolumeData.ExactlySame(Item))
            {
                LastUserActivity = DateTime.Now;
            }
            
            LastUsedVolumeData = Item;
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
            LastUsedVolumeData = Item;
            return Data;
        }
    }

    class VolumeModel
    {
        private Dictionary<string, VolumeInstances> VolumeHistory = new Dictionary<string, VolumeInstances>();        
        public string FileName = "volume_data.dat";

        public VolumeModel()
        {
            ReadFromFile();
        }
        
        public void WriteToFile()
        {
            BasicTools.WriteToFile(FileName, VolumeHistory);
        }

        public void ReadFromFile()
        {
            try
            {
                VolumeHistory = BasicTools.ReadFromFile(FileName) as Dictionary<string, VolumeInstances>;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                VolumeHistory = new Dictionary<string, VolumeInstances>();
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
