using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//
using System.Net.NetworkInformation;
using System.Runtime.InteropServices; //DLLImport
using System.IO; //Path.Combine
using Ini; //Add "Ini.cs"

namespace BandwidthMonitor
{
    public partial class Form1 : Form
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private NetworkInterface[] nicArr;
        private NetworkInterface nic;
        private double ElapsedTime, BytesSent = 0, BytesReceived = 0;
        private long Freq, Tic = 0, Toc, Run = -1;
        private string FN_ini = Path.Combine(Directory.GetCurrentDirectory(), "config.ini");
        private int chart_x_interval = 20;

        private void mainMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateNetworkInterface();
        }

        private void cmbInterface_SelectionChangeCommitted(object sender, EventArgs e)
        {
            timer.Enabled = false;
            Run = -1;
            nic = nicArr[cmbInterface.SelectedIndex];
            timer.Enabled = true;
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void UpdateNetworkInterface()
        {
            Run += 1;

            IPv4InterfaceStatistics interfaceStats = nic.GetIPv4Statistics();

            QueryPerformanceCounter(out Toc);
            ElapsedTime = (double)(Toc - Tic) / Freq;
            QueryPerformanceCounter(out Tic);
            double PreviousBytesSent = BytesSent;
            double PreviousBytesReceived = BytesReceived;
            BytesSent = interfaceStats.BytesSent;
            BytesReceived = interfaceStats.BytesReceived;

            if (Run > 0)
            {
                int bytesSentSpeed = (int)((double)(BytesSent - PreviousBytesSent) / ElapsedTime / 1024);
                int bytesReceivedSpeed = (int)((double)(BytesReceived - PreviousBytesReceived) / ElapsedTime / 1024);

                //lblSpeed.Text = "Connection speed = " + (nic.Speed / 1000000).ToString() + " MB/s";
                //lblInterfaceType.Text = "Interface type = " + nic.NetworkInterfaceType.ToString();
                lblTotal.Text = "Up/Down traffic: " + ((double)interfaceStats.BytesSent / 1048576).ToString("F3") + " / " + ((double)interfaceStats.BytesReceived / 1048576).ToString("F3") + " MiB";
                lblSpeed.Text = "Up/Down speed: " + (bytesSentSpeed * 8).ToString() + " / " + (bytesReceivedSpeed * 8).ToString() + " Kibit/s";
                chart.Series["UpSpeed"].Points.AddXY(Run, bytesSentSpeed * 8);
                chart.Series["DownSpeed"].Points.AddXY(Run, bytesReceivedSpeed * 8);
                chart.ChartAreas["ChartArea1"].AxisX.Minimum = Run - chart_x_interval;
                chart.ChartAreas["ChartArea1"].AxisX.Maximum = Run;
            }
            else
            {
                chart.Series["UpSpeed"].Points.Clear();
                chart.Series["DownSpeed"].Points.Clear();

                chart.Series["UpSpeed"].Points.AddXY(0, 0);
                chart.Series["DownSpeed"].Points.AddXY(0, 0);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int ini_width = 400, ini_height = 200, ini_interval = 500, ini_chart_max_y = 1200, ini_combo_visible = 1;
            string ini_default_interface, ini_color_back = "#252526";

            //BackColor = Color.Lime;
            //TransparencyKey = Color.Lime;
            //FormBorderStyle = FormBorderStyle.None;

            cmbInterface.DropDownStyle = ComboBoxStyle.DropDownList;
            nicArr = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < nicArr.Length; i++)
                cmbInterface.Items.Add(nicArr[i].Name);
            cmbInterface.SelectedIndex = 0;
            ini_default_interface = cmbInterface.SelectedText;

            if (File.Exists(FN_ini))
            {
                try
                {
                    IniFile ini = new IniFile(FN_ini);
                    ini_width = int.Parse(ini.IniReadValue("settings", "width"));
                    ini_height = int.Parse(ini.IniReadValue("settings", "height"));
                    ini_interval = int.Parse(ini.IniReadValue("settings", "interval"));
                    ini_chart_max_y = int.Parse(ini.IniReadValue("settings", "chart_max_y"));
                    ini_default_interface = ini.IniReadValue("settings", "default_interface");
                    ini_combo_visible = int.Parse(ini.IniReadValue("settings", "combo_visible"));
                    ini_color_back = ini.IniReadValue("settings", "color_back");
                }
                catch
                {
                    //File.Delete(FN_ini);
                }
            }

            if (cmbInterface.FindStringExact(ini_default_interface) != -1)
                cmbInterface.SelectedIndex = cmbInterface.FindStringExact(ini_default_interface);
            nic = nicArr[cmbInterface.SelectedIndex];

            if (ini_combo_visible == 0)
                cmbInterface.Visible = false;

            if (QueryPerformanceFrequency(out Freq) == false)
                throw new Win32Exception();

            this.Icon = BandwidthMonitor.Properties.Resources.icon; //Add existing "icon.ico" resource
            notifyIcon.Icon = BandwidthMonitor.Properties.Resources.icon;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = contextMenuStrip;

            this.Size = new Size(ini_width, ini_height);
            cmbInterface.Size = new Size(120, cmbInterface.Size.Height);
            cmbInterface.Location = new Point(5, 5);
            lblTotal.Location = new Point(cmbInterface.Location.X + cmbInterface.Size.Width + 5, 5);
            lblSpeed.Location = new Point(cmbInterface.Location.X + cmbInterface.Size.Width + 5, 20);
            chart.Size = new Size(this.Size.Width - 25, this.Size.Height - 85);
            chart.Location = new Point(5, 40);

            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = System.Drawing.ColorTranslator.FromHtml(ini_color_back);
            lblTotal.ForeColor = Color.White;
            lblSpeed.ForeColor = Color.White;
            lblTotal.Text = "";
            lblSpeed.Text = "";
            cmbInterface.BackColor = this.BackColor;
            cmbInterface.ForeColor = Color.White;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width, Screen.PrimaryScreen.WorkingArea.Height - this.Height);

            chart.BackColor = System.Drawing.ColorTranslator.FromHtml(ini_color_back);
            chart.Series.Clear();
            chart.Series.Add("UpSpeed");
            chart.Series.Add("DownSpeed");

            chart.Series["UpSpeed"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            chart.Series["UpSpeed"].Color = Color.Red;
            chart.Series["UpSpeed"].BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
            chart.Series["UpSpeed"].BorderWidth = 2;

            chart.Series["DownSpeed"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            chart.Series["DownSpeed"].Color = Color.Green;
            chart.Series["DownSpeed"].BorderWidth = 2;

            chart.Legends["Legend1"].Enabled = false;

            chart.ChartAreas["ChartArea1"].Position.Auto = false;
            chart.ChartAreas["ChartArea1"].Position.Width = 95;
            chart.ChartAreas["ChartArea1"].Position.Height = 100;
            chart.ChartAreas["ChartArea1"].BackColor = System.Drawing.ColorTranslator.FromHtml(ini_color_back);

            chart.ChartAreas["ChartArea1"].AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chart.ChartAreas["ChartArea1"].AxisX.MajorGrid.LineColor = Color.Gray;
            chart.ChartAreas["ChartArea1"].AxisX.MajorTickMark.Enabled = false;
            chart.ChartAreas["ChartArea1"].AxisX.IsMarginVisible = false;
            chart.ChartAreas["ChartArea1"].AxisX.LabelStyle.Enabled = false;
            chart.ChartAreas["ChartArea1"].AxisX.Interval = (int)chart_x_interval / 4;
            chart.ChartAreas["ChartArea1"].AxisX.LineColor = Color.Gray;

            chart.ChartAreas["ChartArea1"].AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chart.ChartAreas["ChartArea1"].AxisY.MajorGrid.LineColor = Color.Gray;
            chart.ChartAreas["ChartArea1"].AxisY.MajorTickMark.Enabled = false;
            chart.ChartAreas["ChartArea1"].AxisY.Minimum = 0;
            chart.ChartAreas["ChartArea1"].AxisY.Maximum = ini_chart_max_y;
            chart.ChartAreas["ChartArea1"].AxisY.Interval = (int)ini_chart_max_y / 4;
            chart.ChartAreas["ChartArea1"].AxisY.LabelAutoFitMinFontSize = 7;
            chart.ChartAreas["ChartArea1"].AxisY.LabelAutoFitMaxFontSize = 7;
            chart.ChartAreas["ChartArea1"].AxisY.LineColor = Color.Gray;
            chart.ChartAreas["ChartArea1"].AxisY.LabelStyle.ForeColor = Color.Gray;

            timer.Interval = ini_interval;
            timer.Enabled = true;
        }
    }
}
