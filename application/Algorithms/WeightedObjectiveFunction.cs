using System;
using System.Collections.Generic;
using System.Linq;
using FSM.Application.Interfaces;
using FSM.Domain.Entities;
using FSM.Domain.Enums;
using FSM.Domain.Interfaces;

namespace FSM.Application.Algorithms
{
    public class WeightedObjectiveFunction : IAlgorithmGoal
    {
        // what is most important to the business
        private readonly double _w1_TravelTime = 1.0;   // Cost per minute of travel
        private readonly double _w2_Delay = 10.0;       // Cost per minute of delay
        private readonly double _w3_UrgentMissed = 500.0; // penalty for missing an urgent task
        private readonly double _w4_Overtime = 20.0;    // Cost per minute of overtime

        public double CalculateScore(IEnumerable<TechnicianSchedule> solution)
        {
            var breakdown = GetDetailedScore(solution);
            return breakdown.FinalWeightedScore;
        }

        public ScoreBreakdown GetDetailedScore(IEnumerable<TechnicianSchedule> solution)
        {
            double totalTravelMinutes = 0;
            double totalDelayMinutes = 0;
            double totalOvertimeMinutes = 0;
            int missedUrgentTasks = 0;

            foreach (var schedule in solution)
            {
                var tech = schedule.Technician;
                
                // Calculate Travel Time
                // we assume the Schedule object has been updated with totals by the scheduler
                double travelDistanceKm = schedule.TotalDistance; 
                double travelMinutes = (travelDistanceKm / tech.EstimatedTravelSpeedKmH) * 60;
                totalTravelMinutes += travelMinutes;

                // Calculate Overtime
                var lastTask = schedule.Tasks.OrderBy(t => t.SequenceIndex).LastOrDefault();
                if (lastTask != null && lastTask.ActualEndTime.HasValue)
                {
                    DateTime shiftEndDateTime = schedule.Date.Add(tech.ShiftEnd);
                    if (lastTask.ActualEndTime.Value > shiftEndDateTime)
                    {
                        totalOvertimeMinutes += (lastTask.ActualEndTime.Value - shiftEndDateTime).TotalMinutes;
                    }
                }

                // Calculate Delays
                foreach (var task in schedule.Tasks)
                {
                    if (task.ActualStartTime.HasValue && task.ActualStartTime.Value > task.TimeWindowEnd)
                    {
                        totalDelayMinutes += (task.ActualStartTime.Value - task.TimeWindowEnd).TotalMinutes;
                    }
                }
            }

            // calculate missed urgent tasks
            // assume the penalty is applied by the engine if tasks are left in the as unnasigned
            // we are calculating how did the technician perform (lower is better)
            double cost = 
                (totalTravelMinutes * _w1_TravelTime) +
                (totalDelayMinutes * _w2_Delay) +
                (missedUrgentTasks * _w3_UrgentMissed) +
                (totalOvertimeMinutes * _w4_Overtime);

            return new ScoreBreakdown
            {
                TotalTravelTime = totalTravelMinutes,
                TotalDelay = totalDelayMinutes,
                OvertimeHours = totalOvertimeMinutes / 60.0,
                CompletedUrgentTasks = 0, // penalize misses 
                FinalWeightedScore = cost
            };
        }
    }
}