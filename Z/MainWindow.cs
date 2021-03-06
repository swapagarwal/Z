﻿using System;
using System.Diagnostics;
using System.Timers;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Z
{
    public partial class MainWindow : Form
    {
        InstalledApplicationList UserApplications = new InstalledApplicationList();
        List<KeyValuePair<string, double>> DisplayedResults = new List<KeyValuePair<string, double>>();

        int UsageCounter = 0;

        public MainWindow()
        {
            InitializeComponent();

            float minutes = 0.1F;
            int Interval = (int)(minutes * 60 * 1000);

            System.Timers.Timer ProcessTimer = new System.Timers.Timer();
            ProcessTimer.Elapsed += new ElapsedEventHandler(ProcessData);
            ProcessTimer.Interval = Interval;
            ProcessTimer.Start();

            System.Timers.Timer UsageCollectionTimer = new System.Timers.Timer();
            UsageCollectionTimer.Elapsed += new ElapsedEventHandler(DisplayUsage);
            UsageCollectionTimer.Interval = 1000;
            UsageCollectionTimer.Start();

            dataGridView1.DefaultCellStyle.SelectionBackColor = dataGridView1.DefaultCellStyle.BackColor;
            dataGridView1.DefaultCellStyle.SelectionForeColor = dataGridView1.DefaultCellStyle.ForeColor;
            dataGridView1.CellClick += new DataGridViewCellEventHandler(dataGridView1_CellClick);

            DisplaySortedPredictions();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            KeyValuePair<string, double> ClickedApplication = new KeyValuePair<string, double>();

            if (application_searcher.Text == "")
            {
                ClickedApplication = DisplayedResults[e.RowIndex];
            }
            else
            {
                ClickedApplication = new KeyValuePair<string, double>(dataGridView1.Rows[e.RowIndex].Cells[0].Value as string, 1);
            }

            List<KeyValuePair<string, double>> DemoteList = new List<KeyValuePair<string, double>>();

            if (application_searcher.Text == "" && e.RowIndex > 0)
            {
                for (int i = 0; i < e.RowIndex; i++)
                {
                    DemoteList.Add(DisplayedResults[i]);
                }
            }

            LearningTools.ProcessApplication(ClickedApplication, DemoteList);

            DisplaySortedPredictions();
        }

        private void ProcessData(object source, ElapsedEventArgs e)
        {
            LearningTools.ProcessVolume(volume_mode.Checked);
            LearningTools.ProcessBrightness(brightness_mode.Checked);
            BasicTools.GetTopWindowName();
        }

        private void DisplaySortedPredictions()
        {
            dataGridView1.Rows.Clear();
            DisplayedResults = LearningTools.GetApplicationPredictions();

            List<KeyValuePair<string, double>> Applications = UserApplications.SearchApplications(application_searcher.Text, LearningTools.GetApplicationPredictions());

            foreach (KeyValuePair<string, double> Application in Applications)
            {
                string ApplicationName = Application.Key;
                dataGridView1.Rows.Add();
                dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[0].Value = ApplicationName;
            }
        }

        private void application_searcher_TextChanged(object sender, EventArgs e)
        {
            DisplaySortedPredictions();
        }
        
        public void DisplayUsage(object sender, EventArgs e)
        {
            if (UsageCounter == 0)
            {
                Usage.intialise();
            }
            Usage.CurrentApplication();
            UsageCounter++;
            if (UsageCounter == 11)
            {
                Usage.dailyResult();
                UsageCounter = 1;
            }
        }
    }
}
