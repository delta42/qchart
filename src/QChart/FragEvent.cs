using System.Collections.Generic;
using System.Diagnostics;

namespace QChart
{
    class FragEvent : Dictionary<string, string>
    {
        public string Name;
        public string Team;
        public int Frags;
        public int Suicides;
        public int Teamkills;
        public int Matchtime;

        public FragEvent(string stFragEventLogLine)
        {
            // Extract key-value pairs for last line to get main info first
            string[] kvps = stFragEventLogLine.Split(';');
            foreach (string kvp in kvps)
            {
                string[] kvpArr = kvp.Split('=');
                Debug.Assert(kvpArr.Length == 2);
                this.Add(kvpArr[0], kvpArr[1]);
            }

            Name = this["name"];
            Team = this["team"];
            Frags = int.Parse(this["frags"]);
            Suicides = int.Parse(this["suicides"]);
            Teamkills = int.Parse(this["teamkills"]);
            // This is a floating point number of seconds, we store it in milliseconds
            Matchtime = (int)(double.Parse(this["matchtime"]) * 1000 + 0.5f);
        }
    }
}
