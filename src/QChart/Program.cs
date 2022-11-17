using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mono.Options;

namespace QChart
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string mvdFilePath = null;
            bool exitWhenDone = false;
            bool keepLogs = false;
            bool showHelp = false;

            OptionSet options = new OptionSet() {
               { "f=|file=", "Specify MVD file to process", v => mvdFilePath = v },
               { "e|exitwhendone", "Exit the app once the chart has been generated", v => exitWhenDone = v != null },
               { "k|keeplogs", "Do not delete temporary logs", v => keepLogs = v != null },
               { "h|help", "Show this message and exit", v => showHelp = v != null }
            };

            List<string> extra;
            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                ErrorBox(e.Message);
                return;
            }

            if (showHelp)
            {
                ShowHelp(options);
                return;
            }

            Application.Run(new FormMain(mvdFilePath, exitWhenDone, keepLogs));
        }

        public static void ShowHelp(OptionSet options)
        {
            string msg = "";
            msg += "Usage: QChart [OPTIONS]+\n";
            msg += "Creates a chart from a Quake MVD demo file.\n";
            msg += "If no file is specified, a UI is shown to allow file selection.\n";
            msg += "\n";
            msg += "Options:\n";

            StringBuilder sb = new StringBuilder();
            TextWriter textWriter = new StringWriter(sb);
            options.WriteOptionDescriptions(textWriter);

            msg += sb.ToString();

            msg += "\n";
            msg += "Note that the exitwhendone option is only applicable when a file has been specified.\n";
            msg += "If log files are kept, they will be in /Users/[USER]/AppData/Local/Temp.\n";

            InfoBox(msg);
        }

        public static void InfoBox(string caption)
        {
            MessageBox.Show(caption, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ErrorBox(string caption)
        {
            MessageBox.Show(caption, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
