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
        Dictionary<string, string> Applications = new Dictionary<string, string>();
        
        public InstalledApplicationList()
        {
            List<string> FilePaths = new List<string>();
            BasicTools.RecursiveDirectoryrSearch(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), ref FilePaths);
            BasicTools.RecursiveDirectoryrSearch(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), ref FilePaths);

            foreach (string FilePath in FilePaths)
            {
                string ApplicationName = Path.GetFileNameWithoutExtension(FilePath);

                // Take the first shortcut found
                if (!Applications.ContainsKey(ApplicationName))
                {
                    Applications.Add(ApplicationName, FilePath);
                }
            }
        }
    }
}
