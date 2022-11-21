using System;

namespace QChart
{
    class Team : IComparable<Team>
    {
        public string Name;
        public int Frags;

        public Team(string name, int frags)
        {
            Name = name.ToUpper();
            Frags = frags;
        }

        public void IncrementFrags(int frags)
        {
            Frags += frags;
        }

        public int CompareTo(Team compareTeam)
        {
            if (this.Frags != compareTeam.Frags)
            {
                return -this.Frags.CompareTo(compareTeam.Frags);
            }
            else
            {
                // We would not expect a match to ever finish as a tie, but we're handling it anyway
                return this.Name.CompareTo(compareTeam.Name);
            }
        }
    }
}
