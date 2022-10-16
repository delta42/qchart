using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

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

            string mvdFilePath = "";
            if (args.Length > 1)
            {
                ErrorBox("More than one command-line argument specified; only one or zero are allowed.");
                return;
            }
            else if (args.Length == 1)
            {
                mvdFilePath = args[0];
            }

            Application.Run(new FormMain(mvdFilePath));
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
