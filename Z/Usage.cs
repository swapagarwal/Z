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
        static string name = "";
        static string text = "";
        static int duration = 0;
        static int queueSize = 10;
        static Dictionary<string, int> All_Program = new Dictionary<string, int>();
        static Queue<Dictionary<string, int>> All_Data = new Queue<Dictionary<string, int>>();
        static Dictionary<string, int> Prediction_Data = new Dictionary<string, int>();
        static Dictionary<string, int> fileData = new Dictionary<string, int>();
        static string FileName = "usageData.txt";

        public static void CurrentApplication()
        {
            string curr_name = BasicTools.GetTopWindowName();
            string curr_text = BasicTools.GetTopWindowText();
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
                try
                {
                    if (!All_Program.ContainsKey(name))
                        All_Program.Add(name, duration);
                    else
                        All_Program[name] += duration;
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                

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
            BasicTools.CreateChart(All_Program, "Usage.png");
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
            Prediction_Data = new Dictionary<string, int>();
            double val = 0;
            foreach (var i in All_Data)
            {
                val++;
                foreach (var j in i)
                {
                    if (!Prediction_Data.ContainsKey(j.Key))
                        Prediction_Data.Add(j.Key, 0);
                    else
                        Prediction_Data[j.Key] += (int)(((double)j.Value)/val);
                }
            }
            Console.WriteLine("Number of ELement in prediction data  " + Prediction_Data.Count);
            BasicTools.CreateChart(Prediction_Data);
            foreach(var i in Prediction_Data)
            {
                Console.WriteLine(i.Key + "   " + i.Value);
            }
        }

        public static void WriteToFile()
        {
            BasicTools.WriteToFile(FileName, All_Program);
        }

        public static void ReadFromFile()
        {
            try
            {
                fileData = BasicTools.ReadFromFile(FileName) as Dictionary<string, int>;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                fileData = new Dictionary<string, int>();
            }
        }
    }
}
