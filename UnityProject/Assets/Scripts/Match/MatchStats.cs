namespace UltimaJugada
{
    public sealed class MatchStats
    {
        public int DribbleAttempts { get; internal set; }
        public int SuccessfulDribbles { get; internal set; }
        public int PassAttempts { get; internal set; }
        public int CompletedPasses { get; internal set; }
        public int Shots { get; internal set; }
        public int PlayerGoals { get; internal set; }
        public int TeammateShots { get; internal set; }
        public int TeammateGoals { get; internal set; }
        public int Assists { get; internal set; }
        public int Interceptions { get; internal set; }
        public int Interventions { get; internal set; }
        public int PrecisionTotal { get; internal set; }
        public int PrecisionAttempts { get; internal set; }
        public int AveragePrecision => PrecisionAttempts == 0 ? 0 : PrecisionTotal / PrecisionAttempts;
    }
}
