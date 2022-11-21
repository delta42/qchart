using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace QChart
{
    public partial class FormMain : Form
    {
        private string MvdFilePath = null;
        private bool KeepLogs = false;
        private bool ExitWhenDone = false;
        private PlayerSessions PlayerSessions;
        private Chart GameChart;

        public FormMain(string mvdFilePath, bool exitWhenDone, bool keepLogs)
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(FormMain_DragEnter);
            this.DragDrop += new DragEventHandler(FormMain_DragDrop);

            MvdFilePath = mvdFilePath;
            ExitWhenDone = exitWhenDone;
            KeepLogs = keepLogs;

            // Include app version in window title 
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            this.Text = $"{fvi.ProductName} v{fvi.ProductVersion}";
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(MvdFilePath))
            {
                StartWorkflow(MvdFilePath);
                if (ExitWhenDone)
                {
                    this.Close();
                }
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

            lblSelectMVD.Text = "Processing...";

            // The first step is to copy the MVD file to the Windows temp folder, so that all the log files
            // automatically get created there. Then at the end we can remove it, along with all the generated
            // log files.
            string tempFolderPath = Path.GetTempPath();
            string mvdFilename = Path.GetFileName(mvdFilePath);
            string mvdFilePathTemp = Path.Combine(tempFolderPath, mvdFilename);

            try
            {
                PlayerSessions = new PlayerSessions();

                if (File.Exists(mvdFilePathTemp))
                {
                    File.Delete(mvdFilePathTemp);
                }
                File.Copy(mvdFilePath, mvdFilePathTemp);

                if (!RunMVDParser(mvdFilePathTemp)) return;
                if (!LoadGameMetadata(mvdFilePathTemp)) return;
                if (!LoadPlayerEventLogs(mvdFilePathTemp)) return;
                if (!CreateChart()) return;

                this.Controls.Remove(lblSelectMVD); // We don't need this anymore
                this.Refresh();

                // Here we pass in the *original* file path, so that the image gets saved in the same folder as it
                SaveChartToFile(mvdFilePath);
            }
            finally
            {
                // Delete the MVD file copy and any generated log files. Here we eat any possible exceptions.
                // Of course, if the user asked to keep logs, we skip this step
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(tempFolderPath);
                    // This is just the MVD file
                    foreach (FileInfo file in dir.EnumerateFiles($"{mvdFilename}"))
                    {
                        file.Delete();
                    }
                    // These are all the logs
                    if (!KeepLogs)
                    {
                        foreach (FileInfo file in dir.EnumerateFiles($"{mvdFilename}*.log"))
                        {
                            file.Delete();
                        }
                        // One of the generate log files is actually a json file
                        foreach (FileInfo file in dir.EnumerateFiles($"{mvdFilename}*.json"))
                        {
                            file.Delete();
                        }
                    }
                }
                catch
                {
                    // Ignore
                }

                Application.UseWaitCursor = false;
            }
        }

        private bool RunMVDParser(string mvdFilePath)
        {
            // Spawn mvdparser.exe with file path as parameter
            string exeFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string exeFilePath = Path.Combine(exeFolderPath, "mvdparser.exe");

            if (!File.Exists(exeFilePath))
            {
                Program.ErrorBox($"Cannot find '{exeFilePath}'");
                return false;
            }
            // We do the dame for the 2 mandatory DAT files because it's easier for us to check here and now then let
            // mvdparser complain
            string fragfilePath = Path.Combine(exeFolderPath, "fragfile.dat");
            if (!File.Exists(fragfilePath))
            {
                Program.ErrorBox($"Cannot find '{fragfilePath}'");
                return false;
            }
            string templateFilePath = Path.Combine(exeFolderPath, "template.dat");
            if (!File.Exists(templateFilePath))
            {
                Program.ErrorBox($"Cannot find '{templateFilePath}'");
                return false;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = exeFilePath;
            startInfo.WorkingDirectory = exeFolderPath;
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
                string filePathEvents = $"{mvdFilePath}-{i}-events.log";
                if (File.Exists(filePathEvents))
                {
                    // There's a bug, or at the very least an "anomaly", whereby extraneous log files are generated, and experimentation
                    // has found that these are not accompanied by -player.log files. Hence we skip those.
                    string filePathPlayer = $"{mvdFilePath}-{i}-player.log";
                    if (File.Exists(filePathPlayer))
                    {
                        PlayerSessions.AddSessionFromEventLog(filePathEvents);
                    }
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

            string serverSettingsLogfilePath = $"{mvdFilePath}-serversettings.log";
            if (!GetKeyValuePairs(serverSettingsLogfilePath, out kvps))
            {
                return false;
            }
            if (!kvps.TryGetValue("timelimit", out string timeLimitString) || !int.TryParse(timeLimitString, out int timeLimit))
            {
                Program.ErrorBox($"Cannot find or parse 'timelimit' in '{serverSettingsLogfilePath}'");
                return false;
            }

            PlayerSessions.MatchDate = matchStartDate;
            PlayerSessions.Map = map.ToUpper();
            PlayerSessions.TimeLimit = timeLimit * 60 * 1000;   // Note: we use milliseconds out of convenience

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

        private void PlayerSessionError()
        {
            string s = "Unexpected MVD file contains more than 4 players per team:\n\n";

            foreach (PlayerSession session in PlayerSessions)
            {
                s += $"Team '{session.TeamName}', Player '{session.PlayerName}'\n";
            }

            Program.ErrorBox(s);
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
            GameChart.ChartAreas.Add(CreateChartArea(out int fragDiffOffset));

            List<Color> colorsTeam1 = new List<Color> { Color.DodgerBlue, Color.SkyBlue, Color.DarkTurquoise, Color.LimeGreen };
            List<Color> colorsTeam2 = new List<Color> { Color.Crimson, Color.HotPink, Color.SandyBrown, Color.OrangeRed };
            foreach (PlayerSession session in PlayerSessions)
            {
                Color color;
                if (session.TeamName == PlayerSessions.Teams[0].Name)
                {
                    if (colorsTeam1.Count > 0)
                    {
                        color = colorsTeam1[0];
                        colorsTeam1.RemoveAt(0);
                    }
                    else
                    {
                        PlayerSessionError();
                        return false;
                    }
                }
                else
                {
                    if (colorsTeam2.Count > 0)
                    {
                        color = colorsTeam2[0];
                        colorsTeam2.RemoveAt(0);
                    }
                    else
                    {
                        PlayerSessionError();
                        return false;
                    }
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

            // Create one more series describing the ongoing team score differential
            GameChart.Series.Add(CreateSeries("FragDiff", Color.LightGray));
            GameChart.Series["FragDiff"].BorderWidth = 3;
            GameChart.Series["FragDiff"].Points.DataBindXY(PlayerSessions.GameFragEvents.TimeArray, PlayerSessions.GameFragEvents.FragDiffArray);
            GameChart.Series["FragDiff"].IsVisibleInLegend = false;
            // These values are tied to the right hand aka secondary y-axis
            GameChart.Series["FragDiff"].YAxisType = AxisType.Secondary;

            // Create and associate a legend
            GameChart.Legends.Add(CreateLegend());

            // Add title and subtitle
            string gameTitle = $"TEAM {PlayerSessions.Teams[0].Name} {PlayerSessions.Teams[0].Frags} vs. TEAM {PlayerSessions.Teams[1].Name} {PlayerSessions.Teams[1].Frags}";
            Title title = new Title(gameTitle, Docking.Top, new Font("Arial", 12, FontStyle.Bold), Color.Black);
            GameChart.Titles.Add(title);

            string gameSubtitle = $"Match Date {PlayerSessions.MatchDate}       Map {PlayerSessions.Map}";
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

        private DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        private ChartArea CreateChartArea(out int fragDiffOffset)
        {
            ChartArea chartArea = new ChartArea();

            chartArea.AxisX.LabelStyle.Format = "{mm:ss}";
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            chartArea.AxisX.MajorGrid.Interval = 1;
            // We want the 20 minute mark to appear, or 25 if that ios the case, etc.
            // We approach this by taking the last possible event and then rounding up to the next highest minute.
            chartArea.AxisX.Maximum = RoundUp(PlayerSessions.LastEventTime, TimeSpan.FromMinutes(1)).ToOADate();
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Minutes;
            chartArea.AxisX.MajorTickMark.Interval = 1;
            chartArea.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chartArea.AxisX.LabelStyle.IntervalType = DateTimeIntervalType.Minutes;
            chartArea.AxisX.LabelStyle.Interval = 1;
            chartArea.AxisX.LabelStyle.IsEndLabelVisible = true;

            // Setup left hand Y-axis for player frags
            chartArea.AxisY.Interval = 5;
            chartArea.AxisY.MajorGrid.Interval = 5;
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            chartArea.AxisY.Minimum = 0;
            // Round up to the nearest multiple of 5
            chartArea.AxisY.Maximum = (int)(PlayerSessions.MaxFrags / 5.0 + 1) * 5;
            chartArea.AxisY.Title = "Individual Player Frags";
            chartArea.AxisY.TitleFont = new Font("Arial", 12);
            chartArea.AxisY.TitleForeColor = Color.Black;
            chartArea.AxisY.LabelStyle.ForeColor = Color.Black;

            // Setup right hand Y-axis for team frag differential
            chartArea.AxisY2.Interval = 5;
            chartArea.AxisY2.MajorGrid.Interval = 5;
            chartArea.AxisY2.Enabled = AxisEnabled.True;
            chartArea.AxisY2.MajorGrid.Enabled = false;
            // We make this a +/- a multiple of 20, with 0 in the middle
            fragDiffOffset = Math.Max(Math.Abs(PlayerSessions.GameFragEvents.FragDiffMin), Math.Abs(PlayerSessions.GameFragEvents.FragDiffMax));
            fragDiffOffset = (int)((fragDiffOffset + 20) / 20.0) * 20;
            // Avoid luducrous amount of labels
            if (fragDiffOffset > 200)
            {
                chartArea.AxisY2.Interval = 20;
                chartArea.AxisY2.MajorGrid.Interval = 20;
            } else if (fragDiffOffset > 100)
            {
                chartArea.AxisY2.Interval = 10;
                chartArea.AxisY2.MajorGrid.Interval = 10;
            }
            chartArea.AxisY2.Minimum = -fragDiffOffset;
            chartArea.AxisY2.Maximum = fragDiffOffset;
            chartArea.AxisY2.Title = "Team Frag Delta";
            chartArea.AxisY2.TitleFont = new Font("Arial", 12);
            chartArea.AxisY2.TitleForeColor = Color.LightGray;
            chartArea.AxisY2.LabelStyle.ForeColor = Color.LightGray;

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

        private void SaveChartToFile(string mvdFilePath)
        {
            Rectangle rc = this.RectangleToScreen(this.ClientRectangle);
            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(rc.Left, rc.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);

            string filePath = $"{mvdFilePath}-chart.png";
            bmp.Save(filePath, ImageFormat.Png);
        }
    }
}
