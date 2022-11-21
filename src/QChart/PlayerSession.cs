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
        public int Frags;           // Frag count at end of match
        public int MaxFrags;        // Max frag count during game, may be more than Frags!
        public DateTime[] TimeArray;
        public int[] FragArray;
        public FragType[] FragTypeArray;
        public DateTime LastEventTime;

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

        public PlayerSession(string eventFilePath, int timeLimit)
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
            MaxFrags = 0;
            for (int i = 0; i < stLineArr.Length; i++)
            {
                FragEvent fragEvent = new FragEvent(stLineArr[i]);

                // Sometimes we find additional events after match time where players have 0 frags. This is possibly a
                // problem with MVDparser. Regardless, we initially deal with this by ignoring events past Matchtime.
                // However this obviously did not work in cases of overtime. Hence now we halt processing events if
                // they are both past MatchTime *and* 0 frags. Of course it's still technically possible a very
                // poor/unlucky player legitimately has 0 frags after Matchtime, and in this case our solution fails.
                // TODO: Study MVDparser closely and see why these extraneous 0 frag events even exist.
                if (fragEvent.Matchtime > timeLimit && fragEvent.Frags == 0)
                {
                    Array.Resize(ref TimeArray, i);
                    Array.Resize(ref FragArray, i);
                    Array.Resize(ref FragTypeArray, i);
                    break;
                }

                FragEvents.Add(fragEvent);

                TimeArray[i] = new DateTime(0).AddMilliseconds(FragEvents[i].Matchtime);
                FragArray[i] = FragEvents[i].Frags;

                // Since frags and suicides and teamkills can all happen in the same event, we simply
                // prioritize: if Frags goes up, we count it as a Frag.
                if (FragEvents[i].Frags == lastFrags + 1)
                {
                    // Frags goes up by one, this is a regular Frag
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

                // Keep track of MaxFrags
                if (FragEvents[i].Frags > MaxFrags)
                {
                    MaxFrags = FragEvents[i].Frags;
                }
            }

            PlayerName = FragEvents.Last().Name;
            TeamName = FragEvents.Last().Team.ToUpper();
            Frags = FragEvents.Last().Frags;
            LastEventTime = new DateTime(0).AddMilliseconds(FragEvents.Last().Matchtime);

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
                if (this.Team.Frags != compareSession.Team.Frags)
                {
                    return -this.Team.Frags.CompareTo(compareSession.Team.Frags);
                }
                else
                {
                    // We would not expect a match to ever finish as a tie, but we're handling it anyway
                    return this.TeamName.CompareTo(compareSession.TeamName);
                }
            }
        }
    }
}
