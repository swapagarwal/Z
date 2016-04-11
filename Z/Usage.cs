﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        static int daily = 0;
        static int weekly = 0;
        static Dictionary<string, int> All_Program = new Dictionary<string, int>();
        static Queue<Dictionary<string, int>> All_Data = new Queue<Dictionary<string, int>>();
        static Queue<Dictionary<string, int>> All_Data_Daily = new Queue<Dictionary<string, int>>();
        static Queue<Dictionary<string, int>> All_Data_weekly = new Queue<Dictionary<string, int>>();
        static Dictionary<string, int> Prediction_Data = new Dictionary<string, int>();
        static Dictionary<string, int> fileData = new Dictionary<string, int>();
        static string FileName = "usageData.txt";
        static string ImageName = "Hourly-";

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
            if(All_Data_Daily.Count == queueSize * 24)
            {
                All_Data_Daily.Dequeue();
            }
            if(All_Data_weekly.Count== queueSize * 168)
            {
                All_Data_weekly.Dequeue();
            }
            All_Data.Enqueue(All_Program);
            All_Data_Daily.Enqueue(All_Program);
            All_Data_weekly.Enqueue(All_Program);
            BasicTools.CreateChart(All_Program, ImageName+weekly+daily+".png");
            showPrediction();
            daily++;
            weekly++;
            if (daily == 24)
            {
                dailyResult();
                daily = 0;
            }
            if(weekly == 168)
            {
                weeklyResult();
                weekly = 0;
            }
            All_Program = new Dictionary<string, int>();
        }

        public static void dailyResult()
        {

        }

        public static void weeklyResult()
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
                Debug.WriteLine(ex.Message);
                fileData = new Dictionary<string, int>();
            }
        }
    }
}
