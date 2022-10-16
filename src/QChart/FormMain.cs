using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace QChart
{
    public partial class FormMain : Form
    {
        PlayerSessions PlayerSessions;
        private Chart GameChart;

        public FormMain(string mvdFilePath)
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(FormMain_DragEnter);
            this.DragDrop += new DragEventHandler(FormMain_DragDrop);

            if (!string.IsNullOrEmpty(mvdFilePath))
            {
                StartWorkflow(mvdFilePath);
            }
        }

        void FormMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void FormMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                if (Path.GetExtension(files[0]).ToUpper() == ".MVD")
                {
                    StartWorkflow(files[0]);
                }
            }
        }

        private void lblSelectMVD_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select MVD File";
            dlg.Filter = "MVD files|*.mvd";
            // TODO: Save the last used folder each time and use that
            dlg.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                StartWorkflow(dlg.FileName);
            }
        }

        private void StartWorkflow(string mvdFilePath)
        {
            Application.UseWaitCursor = true;
            try
            {
                PlayerSessions = new PlayerSessions();
                if (!RunMVDParser(mvdFilePath)) return;
                if (!LoadPlayerEventLogs(mvdFilePath)) return;
                if (!LoadGameMetadata(mvdFilePath)) return;
                if (!CreateChart()) return;
                this.Controls.Remove(lblSelectMVD); // We don't need this anymore
            }
            finally
            {
                Application.UseWaitCursor = false;
            }
        }

        private bool RunMVDParser(string mvdFilePath)
        {
            // Spawn mvdparser.exe with file path as parameter
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string exeFilePath = Path.Combine(folderPath, "mvdparser.exe");

            if (!File.Exists(exeFilePath))
            {
                Program.ErrorBox($"Cannot find '{exeFilePath}'");
                return false;
            }
            // We do the dame for the 2 mandatory DAT files because it's easier for us to check here and now then let
            // mvdparser complain
            string fragfilePath = Path.Combine(folderPath, "fragfile.dat");
            if (!File.Exists(fragfilePath))
            {
                Program.ErrorBox($"Cannot find '{fragfilePath}'");
                return false;
            }
            string templateFilePath = Path.Combine(folderPath, "template.dat");
            if (!File.Exists(templateFilePath))
            {
                Program.ErrorBox($"Cannot find '{templateFilePath}'");
                return false;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = $"\"{exeFilePath}\""; 
            startInfo.Arguments = $"\"{mvdFilePath}\"";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            try
            {
                if (!process.Start())
                {
                    Program.ErrorBox("Process Start failed");
                    return false;
                }
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Program.ErrorBox($"Exception launching mvdparser: {ex.Message}");
                return false;
            }

            return true;
        }

        private bool LoadPlayerEventLogs(string mvdFilePath)
        {
            // Locate all event logs and load into data structures. Note that event indexes are not necessarily contiguous,
            // so we just look at all indexes 0 .. 31 anbd load what we find.
            for (int i = 0; i < 32; i++)
            {
                string filePath = $"{mvdFilePath}-{i}-events.log";
                if (File.Exists(filePath))
                {
                    PlayerSessions.AddSessionFromEventLog(filePath);
                }
            }

            // Sessions are loaded, sort them internally
            PlayerSessions.FinalizeSessions();

#if DEBUG
            // Sanity check
            string msg = "";
            foreach (PlayerSession playerSession in PlayerSessions)
            {
                msg += $"Team {playerSession.TeamName}, player {playerSession.PlayerName}, frags {playerSession.Frags}\n";
            }
            Debug.WriteLine(msg);
#endif

            return true;
        }

        private bool LoadGameMetadata(string mvdFilePath)
        {
            Dictionary<string, string> kvps;

            string demoLogfilePath = $"{mvdFilePath}-demo.log";
            if (!GetKeyValuePairs(demoLogfilePath, out kvps))
            {
                return false;
            }
            if (!kvps.TryGetValue("matchstartdate", out string matchStartDate))
            {
                Program.ErrorBox($"Cannot find 'matchstartdate' in '{demoLogfilePath}'");
                return false;
            }

            string mapLogfilePath = $"{mvdFilePath}-map.log";
            if (!GetKeyValuePairs(mapLogfilePath, out kvps))
            {
                return false;
            }
            if (!kvps.TryGetValue("map", out string map))
            {
                Program.ErrorBox($"Cannot find 'map' in '{demoLogfilePath}'");
                return false;
            }

            PlayerSessions.MatchDate = matchStartDate;
            PlayerSessions.Map = map.ToUpper();

            return true;
        }

        private bool GetKeyValuePairs(string filePath, out Dictionary<string, string> dict)
        {
            dict = new Dictionary<string, string>();

            if (!File.Exists(filePath))
            {
                Program.ErrorBox($"Cannot find '{filePath}'");
                return false;
            }

            string[] lines = File.ReadAllLines(filePath);
            // Remove blank lines
            lines = lines.Where(x => !string.IsNullOrWhiteSpace(x.Trim())).ToArray();
            foreach (string line in lines)
            {
                string[] kvpArr = line.Split('=');
                Debug.Assert(kvpArr.Length == 2);
                dict.Add(kvpArr[0], kvpArr[1]);
            }

            return true;
        }

        private bool CreateChart()
        {
            if (GameChart != null)
            {
                Controls.Remove(GameChart);
            }
            GameChart = new Chart();
            Controls.Add(GameChart);
            // Map to the old label's double-click since it does what we need
            GameChart.DoubleClick += new System.EventHandler(this.lblSelectMVD_DoubleClick);
            SizeChartToForm();

            GameChart.ChartAreas.Clear();
            GameChart.Series.Clear();

            // Create and associate the chart
            GameChart.ChartAreas.Add(CreateChartArea());

            List<Color> colorsTeam1 = new List<Color> { Color.DodgerBlue, Color.SkyBlue, Color.DarkTurquoise, Color.LimeGreen };
            List<Color> colorsTeam2 = new List<Color> { Color.Crimson, Color.HotPink, Color.SandyBrown, Color.OrangeRed };
            foreach (PlayerSession session in PlayerSessions)
            {
                Color color;
                if (session.TeamName == PlayerSessions.Teams[0].Name)
                {
                    color = colorsTeam1[0];
                    colorsTeam1.RemoveAt(0);
                }
                else
                {
                    color = colorsTeam2[0];
                    colorsTeam2.RemoveAt(0);
                }
                // Create and associate a new series
                GameChart.Series.Add(CreateSeries(session.SeriesName, color));
                GameChart.Series[session.SeriesName].Points.DataBindXY(session.TimeArray, session.FragArray);
                // Loop over datapoints to give them customer markers based on FragType
                for (int i = 0; i < session.FragTypeArray.Length; i++)
                {
                    MarkerStyle style = MarkerStyle.Circle;
                    int size = 5;
                    switch (session.FragTypeArray[i])
                    {
                        case FragType.Frag:
                            style = MarkerStyle.Circle;
                            size = 5;
                            break;
                        case FragType.Suicide:
                            style = MarkerStyle.Cross;
                            size = 12;
                            break;
                        case FragType.Teamkill:
                            style = MarkerStyle.Star5;
                            size = 12;
                            break;
                    }
                    GameChart.Series[session.SeriesName].Points[i].MarkerStyle = style;
                    GameChart.Series[session.SeriesName].Points[i].MarkerSize = size;
                }
            }

            // Create and associate a legend
            GameChart.Legends.Add(CreateLegend());

            // Add title and subtitle
            string gameTitle = $"TEAM {PlayerSessions.Teams[0].Name} {PlayerSessions.Teams[0].Frags} vs. TEAM {PlayerSessions.Teams[1].Name} {PlayerSessions.Teams[1].Frags}";
            Title title = new Title(gameTitle, Docking.Top, new Font("Arial", 12, FontStyle.Bold), Color.Black);
            GameChart.Titles.Add(title);

            string gameSubtitle = $"Match Date {PlayerSessions.MatchDate} / Map {PlayerSessions.Map}";
            Title subtitle = new Title(gameSubtitle, Docking.Top, new Font("Arial", 10), Color.Black);
            GameChart.Titles.Add(subtitle);

            return true;
        }

        private void SizeChartToForm()
        {
            GameChart.Width = ClientSize.Width;
            GameChart.Height = ClientSize.Height;
            GameChart.Location = new Point(0, 0);
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            SizeChartToForm();
        }

        private ChartArea CreateChartArea()
        {
            ChartArea chartArea = new ChartArea();

            chartArea.AxisX.LabelStyle.Format = "{mm:ss}";
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            chartArea.AxisX.MajorGrid.Interval = 1;
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            chartArea.AxisX.MajorTickMark.Interval = 1;
            chartArea.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chartArea.AxisX.LabelStyle.IntervalType = DateTimeIntervalType.Minutes;
            chartArea.AxisX.LabelStyle.Interval = 1;
            chartArea.AxisX.LabelStyle.IsEndLabelVisible = true;

            // Setup left hand Y-axis
            chartArea.AxisY.Interval = 5;
            chartArea.AxisY.MajorGrid.Interval = 5;
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            chartArea.AxisY.Minimum = 0;
            chartArea.AxisY.Maximum = PlayerSessions.MaxFrags;
            chartArea.AxisY.Title = "Frags";
            chartArea.AxisY.TitleFont = new Font("Arial", 12);
            chartArea.AxisY.TitleForeColor = Color.Blue;
            chartArea.AxisY.LabelStyle.ForeColor = Color.Blue;

            // Duplicate axis on right hand side
            chartArea.AxisY2.Interval = 5;
            chartArea.AxisY2.MajorGrid.Interval = 5;
            //chartArea.AxisY2.MinorGrid.Interval = 1 / 2.0;
            chartArea.AxisY2.Enabled = AxisEnabled.True;
            chartArea.AxisY2.Minimum = 0;
            chartArea.AxisY2.Maximum = PlayerSessions.MaxFrags;
            chartArea.AxisY2.Title = "";
            chartArea.AxisY2.TitleFont = new Font("Arial", 12);
            chartArea.AxisY2.TitleForeColor = Color.Blue;
            chartArea.AxisY2.LabelStyle.ForeColor = Color.Blue;

            return chartArea;
        }

        private Series CreateSeries(string stSeriesName, Color color)
        {
            Series series = new Series();

            series.Name = stSeriesName;
            series.ChartType = SeriesChartType.Line;
            series.XValueType = ChartValueType.Time;

            // Colors
            series.Color = color;
            series.MarkerColor = color;
            series.MarkerBorderColor = color;

            return series;
        }

        private Legend CreateLegend()
        {
            Legend legend = new Legend();

            legend.Font = new Font("Courier New", 10);

            legend.Title = "Players";
            legend.TitleFont = new Font("Arial", 12, FontStyle.Bold);
            legend.BorderColor = Color.Black;

            return legend;
        }
    }
}
