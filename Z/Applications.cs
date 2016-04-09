using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                if(ApplicationName.Contains(substring))
                {
                    ApplicationList.Add(ApplicationName);
                }
            }

            return ApplicationList;
        }
    }

    class ApplicationInstance
    {
        string LastUsedApplication;
        DateTime TimeStamp = DateTime.MinValue;
        bool NetworkStatus;
        double Weight = 1.0;
        HashSet<string> ProcessList = new HashSet<string>();
    }

    class ApplicationModel
    {
        Dictionary<string, ApplicationInstance> ApplicationMap = new Dictionary<string, ApplicationInstance>();
    }
}
