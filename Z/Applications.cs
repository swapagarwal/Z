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

    class ApplicationInstance
    {
        public string LastUsedApplication;
        public string SecondLastUsedApplication;
        public DateTime TimeStamp = DateTime.MinValue;
        public bool NetworkStatus;
        public bool PluggedInStatus;
        public double Weight = 1.0;
        public List<string> ProcessList = new List<string>();
    }

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
        }

        public List<string> PredictApplications(ApplicationInstance Item) // Get Applications
        {
            Dictionary<string, double> CandidateApplications = new Dictionary<string, double>();
            Dictionary<string, string> InstalledApplications = new InstalledApplicationList().ApplicationPaths;
            Dictionary<string, int> ApplicationLastUsedCount = new Dictionary<string, int>();
            Dictionary<string, int> ApplicationNetworkCount = new Dictionary<string, int>();
            Dictionary<string, int> ApplicationPluggedInCount = new Dictionary<string, int>();
            Dictionary<string, int> TotalLastUsedCount = new Dictionary<string, int>();
            Dictionary<bool, int> TotalNetworkCount = new Dictionary<bool, int>();
            Dictionary<bool, int> TotalPluggedInCount = new Dictionary<bool, int>();

            foreach (KeyValuePair<string, ApplicationInstances> entry in ApplicationHistoryData)
            {
                string ApplicationName = entry.Key;
                ApplicationInstances ApplicationHistory = entry.Value;
                foreach (ApplicationInstance Instance in ApplicationHistory.ApplicationInstanceList)
                {
                    int count;
                    string LastUsedApplication = Instance.LastUsedApplication;
                    bool NetworkStatus = Instance.NetworkStatus;
                    bool PluggedInStatus = Instance.PluggedInStatus;

                    string ApplicationLastUsedKey = ApplicationName + ", " + LastUsedApplication;
                    string ApplicationNetworkKey = ApplicationName + ", " + NetworkStatus;
                    string ApplicationPluggedInKey = ApplicationName + ", " + PluggedInStatus;

                    ApplicationLastUsedCount.TryGetValue(ApplicationLastUsedKey, out count);
                    ApplicationLastUsedCount[ApplicationLastUsedKey] = count + 1;

                    ApplicationNetworkCount.TryGetValue(ApplicationNetworkKey, out count);
                    ApplicationNetworkCount[ApplicationNetworkKey] = count + 1;

                    ApplicationPluggedInCount.TryGetValue(ApplicationPluggedInKey, out count);
                    ApplicationPluggedInCount[ApplicationPluggedInKey] = count + 1;

                    TotalLastUsedCount.TryGetValue(LastUsedApplication, out count);
                    TotalLastUsedCount[LastUsedApplication] = count + 1;
                    
                    TotalNetworkCount.TryGetValue(NetworkStatus, out count);
                    TotalNetworkCount[NetworkStatus] = count + 1;

                    TotalPluggedInCount.TryGetValue(PluggedInStatus, out count);
                    TotalPluggedInCount[PluggedInStatus] = count + 1;
                }
            }

            foreach (string ApplicationName in InstalledApplications.Keys)
            {
                double probability = 1.0;
                int ab, b;

                string ApplicationLastUsedKey = ApplicationName + ", " + Item.LastUsedApplication;
                string ApplicationNetworkKey = ApplicationName + ", " + Item.NetworkStatus;
                string ApplicationPluggedInKey = ApplicationName + ", " + Item.PluggedInStatus;

                ApplicationLastUsedCount.TryGetValue(ApplicationLastUsedKey, out ab);
                TotalLastUsedCount.TryGetValue(Item.LastUsedApplication, out b);
                probability *= (b == 0) ? (1.0) : (ab / b);

                ApplicationNetworkCount.TryGetValue(ApplicationNetworkKey, out ab);
                TotalNetworkCount.TryGetValue(Item.NetworkStatus, out b);
                probability *= (b == 0) ? (1.0) : (ab / b);

                ApplicationPluggedInCount.TryGetValue(ApplicationPluggedInKey, out ab);
                TotalPluggedInCount.TryGetValue(Item.PluggedInStatus, out b);
                probability *= (b == 0) ? (1.0) : (ab / b);

                CandidateApplications.Add(ApplicationName, probability);
            }

            return CandidateApplications.ToList().OrderBy(x => x.Value).ToList().Select(x => x.Key).ToList();
        }
    }
}
