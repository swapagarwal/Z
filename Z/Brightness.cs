using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Z
{
    [Serializable()]
    class ApplicationBrightness
    {
        public string ApplicationName;
        public double Brightness;
        public DateTime TimeStamp;
        public double Weight = 1.0;

        public bool ExactlySame(ApplicationBrightness Item)
        {
            return ApplicationName == Item.ApplicationName && Brightness == Item.Brightness;
        }
    }
    
    [Serializable()]
    class BrightnessHistory
    {
        private List<ApplicationBrightness> ApplicationBrightnessList = new List<ApplicationBrightness>();
        private ApplicationBrightness LastUsedBrightnessData = new ApplicationBrightness();
        private bool Dirty = false;
        private DateTime LastUserActivity = DateTime.MinValue;
        private static int Threshold = 30; // seconds
        private static double MinTimeOfDayWeight = 0.001;
        private static double MaxTimeOfDayWeight = 1.0;
        private static double MinTimeWeight = 0.001;
        private static double MaxTimeWeight = 1.0;

        private void ReinforcementLearning(ApplicationBrightness Item)
        {
            List<double> Factors = new List<double>();
            foreach (ApplicationBrightness Instance in ApplicationBrightnessList)
            {
                double val = Math.Abs(Item.TimeStamp.TimeOfDay.Subtract(Instance.TimeStamp.TimeOfDay).TotalHours);
                Factors.Add(Math.Pow(Math.Min(val, 24 - val), 2));
            }
            Factors = Factors.Select(x => Math.Exp(-x)).ToList();
            double MaxWeight = 0;
            int i = 0;
            foreach (ApplicationBrightness Instance in ApplicationBrightnessList)
            {
                Instance.Weight -= Math.Abs(Item.Brightness - Instance.Brightness) * Factors[i];
                MaxWeight = Math.Max(MaxWeight, Instance.Brightness);
                i++;
            }
            if (MaxWeight > 0)
            {
                foreach (ApplicationBrightness Instance in ApplicationBrightnessList)
                {
                    Instance.Weight /= MaxWeight;
                }
            }
        }

        private List<double> GetTimeOfDayDifferenceWeights(ApplicationBrightness Item)
        {
            List<double> Weights = new List<double>();
            // y = c - m * x
            double MaxTimeDifference = 12; // hours
            double slope = (MaxTimeOfDayWeight - MinTimeOfDayWeight) / MaxTimeDifference;
            foreach (ApplicationBrightness Instance in ApplicationBrightnessList)
            {
                double val = Math.Abs(Item.TimeStamp.TimeOfDay.Subtract(Instance.TimeStamp.TimeOfDay).TotalHours);
                Weights.Add(MaxTimeOfDayWeight - slope * Math.Min(val, 24 - val));
            }
            double ratio = 1.0 / Weights.Max();
            Weights = Weights.Select(x => x * ratio).ToList();
            Debug.Assert(Weights.TrueForAll(x => x > 0 && x <= 1));
            return Weights;
        }

        private List<double> GetTimeDifferenceWeights(ApplicationBrightness Item)
        {
            List<double> Weights = new List<double>();
            // y = c - m * x * x
            double MaxTimeDifference = 30; // days
            double slope = (MaxTimeWeight - MinTimeWeight) / Math.Pow(MaxTimeDifference, 2);
            foreach (ApplicationBrightness Instance in ApplicationBrightnessList)
            {
                double val = Math.Abs(Item.TimeStamp.Subtract(Instance.TimeStamp).TotalDays);
                Weights.Add(MaxTimeWeight - slope * Math.Pow(val, 2));
            }
            double ratio = 1.0 / Weights.Max();
            Weights = Weights.Select(x => x * ratio).ToList();
            Debug.Assert(Weights.TrueForAll(x => x > 0 && x <= 1));
            return Weights;
        }

        private void RecalculateWeights(ApplicationBrightness Item)
        {
            ReinforcementLearning(Item);
        }

        public void AddBrightness(ApplicationBrightness Item)
        {
            if (Dirty && !LastUsedBrightnessData.ExactlySame(Item))
            {
                ApplicationBrightnessList.Add(Item);
                RecalculateWeights(Item);
                Dirty = false;
            }
            else if (!Dirty)
            {
                ApplicationBrightnessList.Add(Item);
            }

            if (!LastUsedBrightnessData.ExactlySame(Item))
            {
                LastUserActivity = Item.TimeStamp;
            }
            LastUsedBrightnessData = Item;
        }

        public ApplicationBrightness GetBrightness(ApplicationBrightness Item)
        {
            if (Math.Abs((Item.TimeStamp - LastUserActivity).TotalSeconds) < Threshold)
            {
                return Item;
            }
            ApplicationBrightness Data = new ApplicationBrightness();
            Data.ApplicationName = Item.ApplicationName;
            Data.Brightness = 0;
            Data.TimeStamp = DateTime.Now;
            Data.Weight = Item.Weight;

            List<double> TimeWeights = GetTimeDifferenceWeights(Item);
            List<double> TimeOfDayWeights = GetTimeOfDayDifferenceWeights(Item);
            List<double> NetWeights = new List<double>();

            int i = 0;
            foreach(ApplicationBrightness Instance in ApplicationBrightnessList)
            {
                NetWeights.Add(Instance.Weight * TimeWeights[i] * TimeOfDayWeights[i]);
                i++;
            }

            double TotalWeight = NetWeights.Sum();
            NetWeights = NetWeights.Select(x => x / TotalWeight).ToList();

            i = 0;
            foreach (ApplicationBrightness Instance in ApplicationBrightnessList)
            {
                Data.Brightness += Instance.Brightness * NetWeights[i];
                i++;
            }
            Dirty = true;
            LastUsedBrightnessData = Item;
            return Data;
        }
    }

    class BrightnessModel
    {
        private Dictionary<string, BrightnessHistory> BrightnessHistoryData = new Dictionary<string, BrightnessHistory>();
        public string FileName = Environment.ExpandEnvironmentVariables("brightness_data.dat");

        public BrightnessModel()
        {
            ReadFromFile();
        }

        public void WriteToFile()
        {
            BasicTools.WriteToFile(FileName, BrightnessHistoryData);
        }

        public void ReadFromFile()
        {
            try
            {
                BrightnessHistoryData = BasicTools.ReadFromFile(FileName) as Dictionary<string, BrightnessHistory>;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                BrightnessHistoryData = new Dictionary<string, BrightnessHistory>();
            }
        }

        private string GetKey(ApplicationBrightness Item)
        {
            return Item.ApplicationName;
        }

        public void AddBrightness(ApplicationBrightness Item)
        {
            string Key = GetKey(Item);

            if (Key == null)
            {
                return;
            }

            if (!BrightnessHistoryData.ContainsKey(Key))
            {
                BrightnessHistoryData.Add(Key, new BrightnessHistory());
            }

            BrightnessHistoryData[Key].AddBrightness(Item);
            WriteToFile();
        }

        public ApplicationBrightness GetBrightness(ApplicationBrightness Item)
        {
            string Key = GetKey(Item);
            if (!BrightnessHistoryData.ContainsKey(Key))
            {
                return Item;
            }
            ApplicationBrightness Data = BrightnessHistoryData[Key].GetBrightness(Item);
            WriteToFile();
            return Data;
        }
    }
}
