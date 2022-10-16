using System.Collections.Generic;
using System.Linq;

namespace QChart
{
    class PlayerSessions : List<PlayerSession>
    {
        public List<Team> Teams = new List<Team>();
        public int MaxFrags = 0;
        public string MatchDate;
        public string Map;

        public void AddSessionFromEventLog(string filePath)
        {
            PlayerSession session = new PlayerSession(filePath);
            this.Add(session);

            if (session.Frags > MaxFrags)
            {
                MaxFrags = session.Frags;
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
            // Sort the teams by winning team first
            Teams.Sort();
            // Sort all player sessions by winning team and then final frag count
            Sort();
        }
    }
}
