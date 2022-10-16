using System;

namespace QChart
{    class Team : IComparable<Team>
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
            return -this.Frags.CompareTo(compareTeam.Frags);
        }
    }
}
