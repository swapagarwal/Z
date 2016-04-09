﻿using System;
using System.Collections.Generic;
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
    }
    
    [Serializable()]
    class BrightnessHistory
    {
        private List<ApplicationBrightness> ApplicationBrightnessList = new List<ApplicationBrightness>();
        private ApplicationBrightness LastUsedBrightnessData = new ApplicationBrightness();
        private DateTime LastCalculatedTime = DateTime.MinValue;
        private static int Threshold = 30; // seconds

        private void ReinforcementLearning(ApplicationBrightness Item)
        {
            List<double> Factors = new List<double>();
            foreach (ApplicationBrightness Instance in ApplicationBrightnessList)
            {
                double val = Math.Abs(Item.TimeStamp.TimeOfDay.Subtract(Instance.TimeStamp.TimeOfDay).TotalSeconds);
                Factors.Add(Math.Pow(Math.Max(1, Math.Min(val, 86400 - val)), 2));
            }
            double ratio = 144.0 / Factors.Max();
            Factors = Factors.Select(x => Math.Exp(-x * ratio)).ToList();
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
            foreach (ApplicationBrightness Instance in ApplicationBrightnessList)
            {
                double val = Math.Abs(Item.TimeStamp.TimeOfDay.Subtract(Instance.TimeStamp.TimeOfDay).TotalSeconds);
                Weights.Add(1.0 / Math.Max(1, Math.Min(val, 86400 - val)));
            }
            double ratio = 1.0 / Weights.Max();
            Weights = Weights.Select(x => x * ratio).ToList();
            return Weights;
        }

        private List<double> GetTimeDifferenceWeights(ApplicationBrightness Item)
        {
            List<double> Weights = new List<double>();
            foreach (ApplicationBrightness Instance in ApplicationBrightnessList)
            {
                Weights.Add(Math.Abs(Item.TimeStamp.Subtract(Instance.TimeStamp).TotalSeconds));
            }
            Weights = Weights.Select(x => 1.0 / Math.Log(Math.Max(Math.E, x))).ToList();
            double ratio = 1.0 / Weights.Max();
            Weights = Weights.Select(x => x * ratio).ToList();
            return Weights;
        }

        private void RecalculateWeights(ApplicationBrightness Item)
        {
            ReinforcementLearning(Item);
        }

        public void AddBrightness(ApplicationBrightness Item)
        {
            if(Item.TimeStamp.Subtract(LastCalculatedTime).TotalSeconds < Threshold)
            {
                RecalculateWeights(Item);
            }
            ApplicationBrightnessList.Add(Item);
            LastUsedBrightnessData = Item;
            LastCalculatedTime = DateTime.MinValue;
        }

        public ApplicationBrightness GetBrightness(ApplicationBrightness Item)
        {
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
            LastUsedBrightnessData = Data;
            LastCalculatedTime = DateTime.Now;
            return Data;
        }
    }

    class BrightnessModel
    {
        private Dictionary<string, BrightnessHistory> BrightnessHistoryData = new Dictionary<string, BrightnessHistory>();
        public string FileName = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\brightness_data.dat");

        public BrightnessModel()
        {
            ReadFromFile();
        }

        public void WriteToFile()
        {
            Stream FileStream = File.Open(FileName, FileMode.Truncate);
            BinaryFormatter Serializer = new BinaryFormatter();
            try
            {
                Serializer.Serialize(FileStream, BrightnessHistoryData);
            }
            catch(SerializationException e)
            {
                Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                FileStream.Close();
            }
        }

        public void ReadFromFile()
        {
            Stream FileStream = File.Open(FileName, FileMode.OpenOrCreate);
            BinaryFormatter Serializer = new BinaryFormatter();
            try
            {
                BrightnessHistoryData = Serializer.Deserialize(FileStream) as Dictionary<string, BrightnessHistory>;
            }
            catch
            {
                Console.WriteLine("Failed to deserialize!");
                BrightnessHistoryData = new Dictionary<string, BrightnessHistory>();
            }
            finally
            {
                FileStream.Close();
            }
        }

        private string GetKey(ApplicationBrightness Item)
        {
            return Item.ApplicationName;
        }

        public void AddBrightness(ApplicationBrightness Item)
        {
            string Key = GetKey(Item);
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
            ApplicationBrightness Data = BrightnessHistoryData[Key].GetBrightness(Item);
            WriteToFile();
            return Data;
        }
    }
}