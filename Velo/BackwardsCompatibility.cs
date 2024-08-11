namespace Velo
{
    public struct CategoryBWC1
    {
        public byte MapId;
        public byte TypeId;

        public Category Get()
        {
            return new Category { MapId = MapId, TypeId = TypeId };
        }
    }

    public struct RunInfoBWC1
    {
        public ulong PlayerId;
        public long CreateTime;

        public int Id;
        public int RunTime;
        public CategoryBWC1 Category;
        public byte WasWR;
        public byte HasComments;
        public int Place;
        public int Dist;
        public int GroundDist;
        public int SwingDist;
        public int ClimbDist;
        public short AvgSpeed;
        public short Grapples;
        public short Jumps;
        public short BoostUsed;

        public RunInfo Get()
        {
            return new RunInfo
            {
                PlayerId = PlayerId,
                CreateTime = CreateTime,
                Id = Id,
                RunTime = RunTime,
                Category = Category.Get(),
                WasWR = WasWR,
                HasComments = HasComments,
                Unused = 0,
                Place = Place,
                Dist = Dist,
                GroundDist = GroundDist,
                SwingDist = SwingDist,
                ClimbDist = ClimbDist,
                AvgSpeed = AvgSpeed,
                Grapples = Grapples,
                Jumps = Jumps,
                BoostUsed = BoostUsed
            };
        }
    }
}
