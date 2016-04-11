using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Z
{
    class Usage
    {
        #region DLL Imports
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleBaseName(IntPtr hWnd, IntPtr hModule, StringBuilder lpFileName, int nSize);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleFileNameEx(IntPtr hWnd, IntPtr hModule, StringBuilder lpFileName, int nSize);
        #endregion

        public static string GetTopWindowText()
        {
            IntPtr hWnd = GetForegroundWindow();
            int length = GetWindowTextLength(hWnd);
            StringBuilder text = new StringBuilder(length + 1);
            GetWindowText(hWnd, text, text.Capacity);
            return text.ToString();
        }

        public static string GetTopWindowName()
        {
            IntPtr hWnd = GetForegroundWindow();
            uint lpdwProcessId;
            GetWindowThreadProcessId(hWnd, out lpdwProcessId);

            IntPtr hProcess = OpenProcess(0x0410, false, lpdwProcessId);

            StringBuilder text = new StringBuilder(1000);
            StringBuilder text2 = new StringBuilder(1000);
            GetModuleBaseName(hProcess, IntPtr.Zero, text, text.Capacity);
            GetModuleFileNameEx(hProcess, IntPtr.Zero, text2, text2.Capacity);

            CloseHandle(hProcess);

            return text.ToString() + " # " + text2.ToString();
        }

        static string name = "";
        static string text = "";
        static int duration = 0;
        static Dictionary<string, int> All_Program = new Dictionary<string, int>();
        static Queue<Dictionary<string, int>> All_Data = new Queue<Dictionary<string, int>>();
        static int queueSize = 10;
        static Dictionary<string, double> Prediction_Data = new Dictionary<string, double>();

        public static void CurrentApplication()
        {
            string curr_name = GetTopWindowName();
            string curr_text = GetTopWindowText();
            if (name.Equals(""))
            {
                name = curr_name;
                text = curr_text;
            }
            else if (name.Equals(curr_name) && text.Equals(curr_text))
            {
                duration += 1;
            }
            else
            {
                if (!All_Program.ContainsKey(name))
                {
                    All_Program.Add(name, duration);
                }
                else
                {
                    All_Program[name] += duration;
                }

                //Console.WriteLine(name +  "   " + All_Program[name]);
                //Console.WriteLine(name + "   " + duration);
                duration = 0;
                name = curr_name;
                text = curr_text;
            }
        }

        public static void displayResult()
        {
            //display.Show();
            if (!All_Program.ContainsKey(name))
                All_Program.Add(name, duration);
            else
                All_Program[name] += duration;
            duration = 0;
            Console.WriteLine("Showing the Result   " + All_Data.Count);
            foreach (var k in All_Program)
            {
                Console.WriteLine(k.Key + "   " + k.Value);
            }
            if (All_Data.Count == queueSize)
            {
                All_Data.Dequeue();
            }
            All_Data.Enqueue(All_Program);
            showPrediction();
            All_Program = new Dictionary<string, int>();
        }
        public static void hideResult()
        {
            //display.Hide();
            Console.WriteLine("Hiding the result");
        }
        public static void showPrediction()
        {
            Prediction_Data = new Dictionary<string, double>();
            double val = 0;
            foreach (var i in All_Data)
            {
                val++;
                foreach (var j in i)
                {
                    if (!Prediction_Data.ContainsKey(j.Key))
                        Prediction_Data.Add(j.Key, 0);
                    else
                        Prediction_Data[j.Key] += ((double)j.Value)/val;
                }
            }
            foreach(var i in Prediction_Data)
            {
                Console.WriteLine(i.Key + "   " + i.Value);
            }
            //Console.WriteLine("Number of ELement in prediction data  " + Prediction_Data.Count);
        }
    }
}
