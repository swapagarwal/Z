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
    class InstalledApplicationList
    {
        public Dictionary<string, string> ApplicationPaths = new Dictionary<string, string>();
        
        public InstalledApplicationList()
        {
            List<string> FilePaths = new List<string>();
            BasicTools.RecursiveDirectoryrSearch(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), ref FilePaths);
            BasicTools.RecursiveDirectoryrSearch(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), ref FilePaths);

            foreach (string FilePath in FilePaths)
            {
                string ApplicationName = Path.GetFileNameWithoutExtension(FilePath);

                // Take the first shortcut found
                if (!ApplicationPaths.ContainsKey(ApplicationName))
                {
                    ApplicationPaths.Add(ApplicationName, FilePath);
                }
            }
        }

        public List<string> SearchApplications(string substring)
        {
            List<string> ApplicationList = new List<string>();

            foreach(string ApplicationName in ApplicationPaths.Keys)
            {
                if(ApplicationName.ToLower().Contains(substring.ToLower()))
                {
                    ApplicationList.Add(ApplicationName);
                }
            }

            return ApplicationList;
        }

        public void StartApplication(string ApplicationName)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ApplicationPaths[ApplicationName];
            proc.Start();
        }
    }

    [Serializable()]
    class ApplicationInstance
    {
        public string LastUsedApplication;
        public string SecondLastUsedApplication;
        public DateTime TimeStamp = DateTime.MinValue;
        public bool NetworkStatus;
        public bool PluggedInStatus;
        public double Weight = 1.0;
        public List<string> ProcessList = new List<string>();

        public bool AlmostSame(ApplicationInstance Item)
        {
            return LastUsedApplication == Item.LastUsedApplication && SecondLastUsedApplication == Item.SecondLastUsedApplication && NetworkStatus == Item.NetworkStatus && PluggedInStatus == Item.PluggedInStatus;
        }
    }

    [Serializable()]
    class ApplicationInstances
    {
        public List<ApplicationInstance> ApplicationInstanceList = new List<ApplicationInstance>();

        public void AddApplicationInstance(ApplicationInstance Item)
        {
            ApplicationInstanceList.Add(Item);
        }
    }

    class ApplicationModel
    {
        // Map application name to system states when it was started
        private Dictionary<string, ApplicationInstances> ApplicationHistoryData = new Dictionary<string, ApplicationInstances>();
        public string FileName = Environment.ExpandEnvironmentVariables("application_data.dat");
        private static double MinTimeOfDayWeight = 0.001;
        private static double MaxTimeOfDayWeight = 1.0;
        private static double MinTimeWeight = 0.001;
        private static double MaxTimeWeight = 1.0;
        private static double MinWeight = 0.5;
        private static double MaxWeight = 1.5;
        private static double NegativeReinforcementFactor = 0.05;
        private static double PositiveReinforcementFactor = 0.1;

        public ApplicationModel()
        {
            ReadFromFile();
        }

        public void WriteToFile()
        {
            BasicTools.WriteToFile(FileName, ApplicationHistoryData);
        }

        public void ReadFromFile()
        {
            try
            {
                ApplicationHistoryData = BasicTools.ReadFromFile(FileName) as Dictionary<string, ApplicationInstances>;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                ApplicationHistoryData = new Dictionary<string, ApplicationInstances>();
            }
        }

        public void AddApplicationInstance(string Application, ApplicationInstance Item)
        {
            if(!ApplicationHistoryData.ContainsKey(Application))
            {
                ApplicationHistoryData.Add(Application, new ApplicationInstances());
            }

            ApplicationHistoryData[Application].AddApplicationInstance(Item);
            WriteToFile();
        }

        public void ReinforcedLearning(KeyValuePair<string, double> OpenedApplicationName, List<KeyValuePair<string, double>> ApplicationsDemoteList, ApplicationInstance ApplicationSnapshot)
        {
            List<double> Factors = new List<double>();
            foreach (KeyValuePair<string, double> DemotedApplication in ApplicationsDemoteList)
            {
                if(!ApplicationHistoryData.ContainsKey(DemotedApplication.Key))
                {
                    continue;
                }

                ApplicationInstances ApplicationHistory = ApplicationHistoryData[DemotedApplication.Key];
                foreach (ApplicationInstance Instance in ApplicationHistory.ApplicationInstanceList)
                {
                    double val = Math.Abs(ApplicationSnapshot.TimeStamp.TimeOfDay.Subtract(Instance.TimeStamp.TimeOfDay).TotalHours);
                    Factors.Add(Math.Pow(Math.Min(val, 24 - val), 2));
                }
            }
            Factors = Factors.Select(x => Math.Exp(-x)).ToList();
            
            int i = 0;
            foreach (KeyValuePair<string, double> DemotedApplication in ApplicationsDemoteList)
            {
                if (!ApplicationHistoryData.ContainsKey(DemotedApplication.Key))
                {
                    continue;
                }

                ApplicationInstances ApplicationHistory = ApplicationHistoryData[DemotedApplication.Key];
                foreach (ApplicationInstance Instance in ApplicationHistory.ApplicationInstanceList)
                {
                    if (Instance.AlmostSame(ApplicationSnapshot))
                    {
                        double weight = Instance.Weight * (1 - Math.Abs(DemotedApplication.Value - OpenedApplicationName.Value) * Factors[i] * NegativeReinforcementFactor);
                        Instance.Weight = Math.Min(MaxWeight, Math.Max(MinWeight, weight));
                    }
                    i++;
                }
            }

            ApplicationInstances OpenedApplicationHistory = ApplicationHistoryData[OpenedApplicationName.Key];
            foreach (ApplicationInstance Instance in OpenedApplicationHistory.ApplicationInstanceList)
            {
                if (Instance.AlmostSame(ApplicationSnapshot))
                {
                    double weight = Instance.Weight * (1 + PositiveReinforcementFactor);
                    Instance.Weight = Math.Min(MaxWeight, Math.Max(MinWeight, weight));
                }
            }
        }

        private List<double> GetTimeOfDayDifferenceWeights(ApplicationInstances Items, DateTime CurrentTimeStamp)
        {
            List<double> Weights = new List<double>();
            // y = c - m * x
            double MaxTimeDifference = 12; // hours
            double slope = (MaxTimeOfDayWeight - MinTimeOfDayWeight) / MaxTimeDifference;

            foreach (ApplicationInstance Instance in Items.ApplicationInstanceList)
            {
                double val = Math.Abs(Instance.TimeStamp.TimeOfDay.Subtract(CurrentTimeStamp.TimeOfDay).TotalHours);
                Weights.Add(MaxTimeOfDayWeight - slope * Math.Min(val, 24 - val));
            }

            Debug.Assert(Weights.TrueForAll(x => x > 0 && x <= 1));
            return Weights;
        }

        private List<double> GetTimeDifferenceWeights(ApplicationInstances Items, DateTime CurrentTimeStamp)
        {
            List<double> Weights = new List<double>();
            // y = c - m * x * x
            double MaxTimeDifference = 100; // days
            double slope = (MaxTimeWeight - MinTimeWeight) / Math.Pow(MaxTimeDifference, 2);

            foreach (ApplicationInstance Instance in Items.ApplicationInstanceList)
            {
                double val = Math.Abs(CurrentTimeStamp.Subtract(Instance.TimeStamp).TotalDays);
                Weights.Add(MaxTimeWeight - slope * Math.Pow(val, 2));
            }
            
            Debug.Assert(Weights.TrueForAll(x => x > 0 && x <= 1));
            return Weights;
        }

        public List<KeyValuePair<string, double>> PredictApplications(ApplicationInstance Item) // Get Applications
        {
            Dictionary<string, double> CandidateApplications = new Dictionary<string, double>();
            Dictionary<string, string> InstalledApplications = new InstalledApplicationList().ApplicationPaths;
            Dictionary<string, double> ApplicationLastUsedCount = new Dictionary<string, double>();
            Dictionary<string, double> ApplicationJointCount = new Dictionary<string, double>();
            Dictionary<string, double> ApplicationNetworkCount = new Dictionary<string, double>();
            Dictionary<string, double> ApplicationPluggedInCount = new Dictionary<string, double>();
            Dictionary<string, double> TotalLastUsedCount = new Dictionary<string, double>();
            Dictionary<string, double> TotalJointCount = new Dictionary<string, double>();
            Dictionary<bool, double> TotalNetworkCount = new Dictionary<bool, double>();
            Dictionary<bool, double> TotalPluggedInCount = new Dictionary<bool, double>();
            
            foreach (KeyValuePair<string, ApplicationInstances> entry in ApplicationHistoryData)
            {
                string ApplicationName = entry.Key;
                ApplicationInstances ApplicationHistory = entry.Value;

                List<double> TimeWeights = GetTimeDifferenceWeights(ApplicationHistory, Item.TimeStamp);
                List<double> TimeOfDayWeights = GetTimeOfDayDifferenceWeights(ApplicationHistory, Item.TimeStamp);
                List<double> NetWeights = new List<double>();

                int i = 0;
                foreach (ApplicationInstance Instance in ApplicationHistory.ApplicationInstanceList)
                {
                    double count;
                    string LastUsedApplication = Instance.LastUsedApplication;
                    string SecondLastUsedApplication = Instance.SecondLastUsedApplication;
                    bool NetworkStatus = Instance.NetworkStatus;
                    bool PluggedInStatus = Instance.PluggedInStatus;

                    string ApplicationLastUsedKey = ApplicationName + ", " + LastUsedApplication;
                    string ApplicationJointKey = ApplicationName + ", " + LastUsedApplication + ", " + SecondLastUsedApplication;
                    string ApplicationNetworkKey = ApplicationName + ", " + NetworkStatus;
                    string ApplicationPluggedInKey = ApplicationName + ", " + PluggedInStatus;

                    ApplicationLastUsedCount.TryGetValue(ApplicationLastUsedKey, out count);
                    ApplicationLastUsedCount[ApplicationLastUsedKey] = count + Item.Weight * TimeWeights[i] * TimeOfDayWeights[i];

                    ApplicationJointCount.TryGetValue(ApplicationJointKey, out count);
                    ApplicationJointCount[ApplicationJointKey] = count + Item.Weight * TimeWeights[i] * TimeOfDayWeights[i];

                    ApplicationNetworkCount.TryGetValue(ApplicationNetworkKey, out count);
                    ApplicationNetworkCount[ApplicationNetworkKey] = count + Item.Weight * TimeWeights[i] * TimeOfDayWeights[i];

                    ApplicationPluggedInCount.TryGetValue(ApplicationPluggedInKey, out count);
                    ApplicationPluggedInCount[ApplicationPluggedInKey] = count + Item.Weight * TimeWeights[i] * TimeOfDayWeights[i];

                    TotalLastUsedCount.TryGetValue(LastUsedApplication, out count);
                    TotalLastUsedCount[LastUsedApplication] = count + Item.Weight * TimeWeights[i] * TimeOfDayWeights[i];

                    TotalJointCount.TryGetValue(LastUsedApplication + ", " + SecondLastUsedApplication, out count);
                    TotalJointCount[LastUsedApplication + ", " + SecondLastUsedApplication] = count + Item.Weight * TimeWeights[i] * TimeOfDayWeights[i];

                    TotalNetworkCount.TryGetValue(NetworkStatus, out count);
                    TotalNetworkCount[NetworkStatus] = count + Item.Weight * TimeWeights[i] * TimeOfDayWeights[i];

                    TotalPluggedInCount.TryGetValue(PluggedInStatus, out count);
                    TotalPluggedInCount[PluggedInStatus] = count + Item.Weight * TimeWeights[i] * TimeOfDayWeights[i];

                    i++;
                }
            }

            foreach (string ApplicationName in InstalledApplications.Keys)
            {
                double probability = 1.0;
                double ab, b;

                string ApplicationLastUsedKey = ApplicationName + ", " + Item.LastUsedApplication;
                string ApplicationJointKey = ApplicationName + ", " + Item.LastUsedApplication + ", " + Item.SecondLastUsedApplication;
                string ApplicationNetworkKey = ApplicationName + ", " + Item.NetworkStatus;
                string ApplicationPluggedInKey = ApplicationName + ", " + Item.PluggedInStatus;

                ApplicationLastUsedCount.TryGetValue(ApplicationLastUsedKey, out ab);
                TotalLastUsedCount.TryGetValue(Item.LastUsedApplication, out b);
                probability *= (b == 0) ? (1.0) : (ab / b);

                ApplicationJointCount.TryGetValue(ApplicationJointKey, out ab);
                TotalJointCount.TryGetValue(Item.LastUsedApplication + ", " + Item.SecondLastUsedApplication, out b);
                probability *= (b == 0) ? (1.0) : (ab / b);

                ApplicationNetworkCount.TryGetValue(ApplicationNetworkKey, out ab);
                TotalNetworkCount.TryGetValue(Item.NetworkStatus, out b);
                probability *= (b == 0) ? (1.0) : (ab / b);

                ApplicationPluggedInCount.TryGetValue(ApplicationPluggedInKey, out ab);
                TotalPluggedInCount.TryGetValue(Item.PluggedInStatus, out b);
                probability *= (b == 0) ? (1.0) : (ab / b);

                CandidateApplications.Add(ApplicationName, probability);
            }

            WriteToFile();
            return CandidateApplications.ToList().OrderBy(x => -x.Value).ToList();
        }
    }
}
