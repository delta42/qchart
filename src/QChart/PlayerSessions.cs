using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QChart
{
    class PlayerSessions : List<PlayerSession>
    {
        public List<Team> Teams = new List<Team>();
        public int MaxFrags = 0;
        public DateTime LastEventTime = new DateTime(0);
        public string MatchDate;
        public string Map;
        public int TimeLimit;
        public GameFragEvents GameFragEvents;

        public void AddSessionFromEventLog(string filePath)
        {
            PlayerSession session = new PlayerSession(filePath, TimeLimit);
            this.Add(session);

            if (session.MaxFrags > MaxFrags)
            {
                MaxFrags = session.MaxFrags;
            }
            if (session.LastEventTime > LastEventTime)
            {
                LastEventTime = session.LastEventTime;
            }

            // If it's a new team, add it to the list. Otherwise increment frag count
            Team currentTeam = Teams.Where(team => team.Name == session.TeamName).ToList().FirstOrDefault();
            if (currentTeam == null)
            {
                currentTeam = new Team(session.TeamName, session.Frags);
                Teams.Add(currentTeam);
            }
            else
            {
                currentTeam.IncrementFrags(session.Frags);
            }
            // Propagate to session so that we can then sort by final team score
            session.Team = currentTeam;
        }

        public void FinalizeSessions()
        {
            // Sort the teams by winning team first (see Team.CompareTo())
            Teams.Sort();

            // Sort all player sessions by winning team and then final frag count (see PlayerSession.CompareTo())
            Sort();

            // Create an array containing all the players' frag events so that we can keep track of the ongoing team
            // score differential
            GameFragEvents = new GameFragEvents();
            foreach (PlayerSession session in this)
            {
                for (int i = 0; i < session.TimeArray.Length; i++)
                {
                    GameFragEvent gameFragEvent = new GameFragEvent();
                    gameFragEvent.Time = session.TimeArray[i];
                    gameFragEvent.Frags = session.FragArray[i];
                    gameFragEvent.TeamName = session.TeamName;
                    gameFragEvent.PlayerName = session.PlayerName;

                    GameFragEvents.Add(gameFragEvent);
                }
            }

            GameFragEvents.FinalizeFrageEvents(this);
        }
    }

    class GameFragEvents : List<GameFragEvent>
    {
        public DateTime[] TimeArray;
        public int[] FragDiffArray;
        public int FragDiffMin = 0;
        public int FragDiffMax = 0;

        public void FinalizeFrageEvents(PlayerSessions playerSessions)
        {
            // Now that we have all base frag events, we sort them chronologically
            this.Sort();

            // Now traverse the whole event list and form Team1Scores and Team2Scores, but we first inizialize the
            // initial score stuctures.
            Dictionary<string, int> team1Scores = new Dictionary<string, int>();
            Dictionary<string, int> team2Scores = new Dictionary<string, int>();
            foreach (PlayerSession session in playerSessions)
            {
                if (session.TeamName == playerSessions.Teams[0].Name)
                {
                    team1Scores[session.PlayerName] = 0;
                }
                else
                {
                    team2Scores[session.PlayerName] = 0;
                }
            }
            foreach (GameFragEvent gameFragEvent in this)
            {
                // We always start with the previous score state, knowing that in this event,
                // exactly one score will be changing.
                gameFragEvent.Team1Scores = new Dictionary<string, int>(team1Scores);
                gameFragEvent.Team2Scores = new Dictionary<string, int>(team2Scores);

                if (gameFragEvent.TeamName == playerSessions.Teams[0].Name)
                {
                    gameFragEvent.Team1Scores[gameFragEvent.PlayerName] = gameFragEvent.Frags;
                }
                else
                {
                    gameFragEvent.Team2Scores[gameFragEvent.PlayerName] = gameFragEvent.Frags;
                }

                team1Scores = new Dictionary<string, int>(gameFragEvent.Team1Scores);
                team2Scores = new Dictionary<string, int>(gameFragEvent.Team2Scores);
            }

#if DEBUG
            // Sanity check
            foreach (GameFragEvent g in this)
            {
                string log = $"{g.Time,10} ";

                log += "Team1 (";
                int team1Score = 0;
                foreach (var (player, score) in g.Team1Scores)
                {
                    log += $"{player,10}:{score,-3} ";
                    team1Score += score;
                }
                log += $" team:{team1Score,-3}) ";

                log += "Team2 (";
                int team2Score = 0;
                foreach (var (player, score) in g.Team2Scores)
                {
                    log += $"{player,10}:{score,-3} ";
                    team2Score += score;
                }
                log += $" team:{team2Score,-3}) ";

                Debug.WriteLine(log);
            }
#endif  

            // Now we do one last pass and create a Frag diff array to be used in a chart series.
            // We add the (0,0) point for continutity in the graph.
            TimeArray = new DateTime[this.Count + 1];
            FragDiffArray = new int[this.Count + 1];

            TimeArray[0] = new DateTime(0);
            FragDiffArray[0] = 0;

            int index = 1;

            foreach (GameFragEvent gameFragEvent in this)
            {
                int team1Score = 0;
                int team2Score = 0;

                foreach (var (player, score) in gameFragEvent.Team1Scores)
                {
                    team1Score += score;
                }
                foreach (var (player, score) in gameFragEvent.Team2Scores)
                {
                    team2Score += score;
                }

                TimeArray[index] = gameFragEvent.Time;
                FragDiffArray[index] = team1Score - team2Score;

                if (FragDiffArray[index] < FragDiffMin)
                {
                    FragDiffMin = FragDiffArray[index];
                }
                if (FragDiffArray[index] > FragDiffMax)
                {
                    FragDiffMax = FragDiffArray[index];
                }

                index += 1;
            }
        }
    }

    class GameFragEvent : IComparable<GameFragEvent>
    {
        // This event symbolizes that at time Time, player PlayerName of team TeamName had Frags frags. After all
        // events are stored and sorted chronologically, Team1Scores and Team2Scores will be formed to contain
        // the exact score state for all players, at each one of the events
        public string TeamName;
        public string PlayerName;
        public int Frags;
        public DateTime Time;
        public Dictionary<string, int> Team1Scores = new Dictionary<string, int>();
        public Dictionary<string, int> Team2Scores = new Dictionary<string, int>();

        public int CompareTo(GameFragEvent compareEvent)
        {
            return this.Time.CompareTo(compareEvent.Time);
        }
    }
}
