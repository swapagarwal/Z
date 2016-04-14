using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        static int queueSize = 100;
        static int monthly = 0;
        static int weekly = 0;
        static Dictionary<string, int> All_Program = new Dictionary<string, int>();
        static Queue<Dictionary<string, int>> All_Data = new Queue<Dictionary<string, int>>();
        static Queue<Dictionary<string, int>> All_Data_weekly = new Queue<Dictionary<string, int>>();
        static Queue<Dictionary<string, int>> All_Data_monthly = new Queue<Dictionary<string, int>>();
        static Dictionary<string, int> Prediction_Data = new Dictionary<string, int>();
        static List<Dictionary<string, int>> fileData = new List<Dictionary<string, int>>();

        static string FileName = "usageData.txt";
        static string ImageName = "Daily-";
        static string t = "";

        public static void intialise()
        {
            Console.WriteLine("reading from file");
            for (int i = fileData.Count-1; i >= 0; i--)
            {
                if (fileData.Count - i >= queueSize * 7)
                    break;
                else
                    All_Data_weekly.Enqueue(fileData[i]);
            }
            for (int i = fileData.Count-1; i >= 0; i--)
            {
                if (fileData.Count - i >= queueSize * 30)
                    break;
                else
                    All_Data_monthly.Enqueue(fileData[i]);
            }
        }

        public static void CurrentApplication()
        {
            string curr_name = BasicTools.GetTopWindowName();
            string curr_text = BasicTools.GetTopWindowText();
            if (name == "" || name == null)
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


                    name = curr_name;
                    text = curr_text;
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                
                duration = 0;
            }
        }

        public static void dailyResult()
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
            if (All_Data.Count >= queueSize)
            {
                All_Data.Dequeue();
            }
            if(All_Data_weekly.Count >= queueSize * 7)
            {
                All_Data_weekly.Dequeue();
            }
            if(All_Data_monthly.Count >= queueSize * 30)
            {
                All_Data_monthly.Dequeue();
            }
            All_Data.Enqueue(All_Program);
            All_Data_weekly.Enqueue(All_Program);
            All_Data_monthly.Enqueue(All_Program);
            t = DateTime.Now.ToFileTime().ToString();
            BasicTools.CreateChart(All_Program, ImageName + t +".png");
            fileData.Add(All_Program);
            WriteToFile();
            showPrediction();
            monthly++;
            weekly++;
            if (weekly == 7)
            {
                weeklyResult();
                weekly = 0;
            }
            if(monthly == 30)
            {
                monthlyResult();
                monthly = 0;
            }
            All_Program = new Dictionary<string, int>();
        }

        public static void weeklyResult()
        {
            
        }

        public static void monthlyResult()
        {

        }

        public static void hideResult()
        {
            //display.Hide();
            Console.WriteLine("Hiding the result");
        }
        public static void showPrediction()
        {
            Prediction_Data = new Dictionary<string, int>();
            Dictionary<string, int> totalData = new Dictionary<string, int>();
            double val = queueSize+2;
            foreach (var i in All_Data)
            {
                val--;
                foreach (var j in i)
                {
                    if (!Prediction_Data.ContainsKey(j.Key))
                    {
                        Prediction_Data.Add(j.Key, (int)(j.Value*Math.Log(val)));
                        totalData.Add(j.Key, j.Value);
                    }
                    else
                    {
                        Prediction_Data[j.Key] += (int)(j.Value* Math.Log(val));
                        totalData[j.Key] += (int)j.Value;
                    }
                        
                }
            }
            
            foreach(var i in totalData)
            {
                if (totalData[i.Key] > 0)
                {
                    Prediction_Data[i.Key]/=totalData[i.Key];
                    //Console.WriteLine(i.Key + "   " + i.Value);
                }
                
            }
            Console.WriteLine("Number of ELement in prediction data  " + Prediction_Data.Count);
            BasicTools.CreateChart(Prediction_Data,"prediction" + t + ".png");
            foreach(var i in Prediction_Data)
            {
                Console.WriteLine(i.Key + "   " + i.Value);
            }
        }

        public static void WriteToFile()
        {
            BasicTools.WriteToFile(FileName, fileData);
        }

        public static void ReadFromFile()
        {
            try
            {
                fileData = BasicTools.ReadFromFile(FileName) as List<Dictionary<string, int>>;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                fileData = new List<Dictionary<string, int>>();
            }
        }
    }
}
