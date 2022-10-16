using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QChart
{
    public enum FragType { Frag, Suicide, Teamkill };

    class PlayerSession : IComparable<PlayerSession>
    {
        static public int MaxPlayerNameLen = 0;
        static public int MaxTeamNameLen = 0;
        public Team Team;

        List<FragEvent> FragEvents = new List<FragEvent>();
        public string PlayerName;
        public string TeamName;
        public int Frags;
        public DateTime[] TimeArray;
        public int[] FragArray;
        public FragType[] FragTypeArray;

        public string SeriesName
        {
            get
            {
                string teamNamePadded = TeamName.PadRight(MaxTeamNameLen);
                string playerNamePadded = PlayerName.PadRight(MaxPlayerNameLen);
                string seriesName = $"{teamNamePadded} {playerNamePadded} {Frags}";
                return seriesName.ToUpper();
            }
        }

        public PlayerSession(string eventFilePath)
        {
            string[] stLineArr = File.ReadAllLines(eventFilePath);
            // Remove empty lines
            stLineArr = stLineArr.Where(x => !string.IsNullOrWhiteSpace(x.Trim())).ToArray();

            TimeArray = new DateTime[stLineArr.Length];
            FragArray = new int[stLineArr.Length];
            FragTypeArray = new FragType[stLineArr.Length];

            int lastFrags = 0;
            int lastSuicides = 0;
            int lastTeamkills = 0;
            for (int i = 0; i < stLineArr.Length; i++)
            {
                FragEvents.Add(new FragEvent(stLineArr[i]));

                TimeArray[i] = new DateTime(0).AddMilliseconds(FragEvents[i].Matchtime);
                FragArray[i] = FragEvents[i].Frags;

                // Since frags and suicides and teamkills can all happen in the same event, we simply
                // prioritize: if Frags goes up, we count it as a Frags.
                if (FragEvents[i].Frags == lastFrags + 1)
                {
                    // frags goes up by one, this is a regular frag
                    FragTypeArray[i] = FragType.Frag;
                }
                else if (FragEvents[i].Suicides == lastSuicides + 1)
                {
                    // Suicide
                    FragTypeArray[i] = FragType.Suicide;
                }
                else if (FragEvents[i].Teamkills == lastTeamkills + 1)
                {
                    // Teamkill
                    FragTypeArray[i] = FragType.Teamkill;
                }

                lastFrags = FragEvents[i].Frags;
                lastSuicides = FragEvents[i].Suicides;
                lastTeamkills = FragEvents[i].Teamkills;
            }

            PlayerName = FragEvents.Last().Name;
            TeamName = FragEvents.Last().Team.ToUpper();
            Frags = FragEvents.Last().Frags;

            // We set these up so that PlayerSession.SeriesName can do some fancy formatting
            if (PlayerName.Length > MaxPlayerNameLen)
            {
                MaxPlayerNameLen = PlayerName.Length;
            }
            if (TeamName.Length > MaxTeamNameLen)
            {
                MaxTeamNameLen = TeamName.Length;
            }
        }

        public int CompareTo(PlayerSession compareSession)
        {
            if (this.TeamName == compareSession.TeamName)
            {
                return -this.Frags.CompareTo(compareSession.Frags);
            }
            else
            {
                return -this.Team.Frags.CompareTo(compareSession.Team.Frags);
            }
        }
    }
}
