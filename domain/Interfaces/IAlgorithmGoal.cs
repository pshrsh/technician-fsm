using System.Collections.Generic;
using FSM.Domain.Entities; //

namespace FSM.Domain.Interfaces
{
    public interface IAlgorithmGoal
    {
        double CalculateScore(IEnumerable<TechnicianSchedule> solution);
        ScoreBreakdown GetDetailedScore(IEnumerable<TechnicianSchedule> solution);
    }

    public class ScoreBreakdown
    {
        public double TotalTravelTime { get; set; }
        public double TotalDelay { get; set; }
        public int CompletedUrgentTasks { get; set; }
        public double OvertimeHours { get; set; }
        public double FinalWeightedScore { get; set; }
    }
}