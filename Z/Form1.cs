using System;
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
    public partial class Form1 : Form
    {
        InstalledApplicationList UserApplications = new InstalledApplicationList();
        List<KeyValuePair<string, double>> DisplayedResults = new List<KeyValuePair<string, double>>();

        int UsageCounter = 0;

        public Form1()
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
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            string ApplicationName = dataGridView1.Rows[e.RowIndex].Cells[0].Value as string;
            List<KeyValuePair<string, double>> DemoteList = new List<KeyValuePair<string, double>>();

            if (application_searcher.Text == "" && e.RowIndex > 0)
            {
                for (int i = 0; i < e.RowIndex; i++)
                {
                    DemoteList.Add(DisplayedResults[i]);
                }
            }

            LearningTools.ProcessApplication(ApplicationName, DemoteList);
        }

        private void ProcessData(object source, ElapsedEventArgs e)
        {
            LearningTools.ProcessVolume(volume_mode.Checked);
            LearningTools.ProcessBrightness(brightness_mode.Checked);
            BasicTools.GetTopWindowName();
        }
        
        private void application_searcher_TextChanged(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();

            if (application_searcher.Text == "")
            {
                DisplayedResults = LearningTools.GetApplicationPredictions();

                List<string> Applications = DisplayedResults.Select(x => x.Key).ToList();
                foreach (string ApplicationName in Applications)
                {
                    dataGridView1.Rows.Add();
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[0].Value = ApplicationName;
                }
            }
            else
            {
                List<string> Applications = UserApplications.SearchApplications(application_searcher.Text);
                DisplayedResults.Clear();

                foreach (string ApplicationName in Applications)
                {
                    dataGridView1.Rows.Add();
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[0].Value = ApplicationName;
                }
            }
        }
        
        public void DisplayUsage(object sender, EventArgs e)
        {
            Usage.CurrentApplication();
            UsageCounter++;
            if (UsageCounter == 20)
            {
                Usage.displayResult();
                UsageCounter = 0;
            }
            else if (UsageCounter == 10)
            {
                Usage.hideResult();
            }
        }
    }
}
