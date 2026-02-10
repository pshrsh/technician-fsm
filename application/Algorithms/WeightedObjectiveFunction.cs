using System;
using System.Collections.Generic;
using System.Linq;
using FSM.Application.Interfaces;
using FSM.Application.Utilities;
using FSM.Domain.Entities;
using FSM.Domain.Interfaces;

namespace FSM.Application.Algorithms
{
    public class WeightedObjectiveFunction : IAlgorithmGoal
    {
        // Penalty Weights
        private readonly double _w_Distance = 1.0;      // 1 point per km
        private readonly double _w_Lateness = 1000.0;   // 1000 points per HOUR late
        private readonly double _w_Waiting = 10.0;      // 10 points per hour waiting

        public double CalculateScore(IEnumerable<TechnicianSchedule> solution)
        {
            // Simply reuse the detailed logic to avoid duplication
            return GetDetailedScore(solution).FinalWeightedScore;
        }

        public ScoreBreakdown GetDetailedScore(IEnumerable<TechnicianSchedule> solution)
        {
            double totalCost = 0;
            double totalTravel = 0;
            double totalDelay = 0;

            foreach (var schedule in solution)
            {
                if (schedule.Tasks == null || !schedule.Tasks.Any()) continue;

                var tech = schedule.Technician;
                TimeSpan currentTime = tech.ShiftStart;
                double currentLat = tech.BaseLatitude;
                double currentLon = tech.BaseLongitude;

                foreach (var task in schedule.Tasks)
                {
                    // Adjust for break before starting new task
                    currentTime = BreakTimeHelper.AdjustForBreak(currentTime);

                    // travel
                    double dist = GeoUtils.CalculateDistance(currentLat, currentLon, task.Latitude, task.Longitude);
                    double travelHours = dist / tech.EstimatedTravelSpeedKmH;
                    
                    totalCost += (dist * _w_Distance);
                    totalTravel += (travelHours * 60); // Store in minutes

                    // Add travel time with break consideration
                    currentTime = BreakTimeHelper.AddTimeWithBreak(currentTime, TimeSpan.FromHours(travelHours));

                    // time Windows
                    // if early then wait
                    if (task.WindowStart.HasValue && currentTime < task.WindowStart.Value)
                    {
                        double waitHours = (task.WindowStart.Value - currentTime).TotalHours;
                        totalCost += (waitHours * _w_Waiting);
                        currentTime = task.WindowStart.Value;
                    }

                    // if late then penalty
                    if (task.WindowEnd.HasValue && currentTime > task.WindowEnd.Value)
                    {
                        double lateHours = (currentTime - task.WindowEnd.Value).TotalHours;
                        totalCost += (lateHours * _w_Lateness);
                        totalDelay += (lateHours * 60); // Store in minutes
                    }

                    // work - add task duration with break consideration
                    currentTime = BreakTimeHelper.CalculateEndTimeWithBreak(currentTime, task.Duration);
                    currentLat = task.Latitude;
                    currentLon = task.Longitude;
                }
            }

            return new ScoreBreakdown
            {
                FinalWeightedScore = totalCost,
                TotalTravelTime = totalTravel,
                TotalDelay = totalDelay,
                CompletedUrgentTasks = 0, // Not currently tracking urgency counts
                OvertimeHours = 0         // Not currently tracking overtime
            };
        }

        private static class GeoUtils 
        {
            public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
            {
                var R = 6371; 
                var dLat = (lat2 - lat1) * (Math.PI / 180);
                var dLon = (lon2 - lon1) * (Math.PI / 180);
                var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                        Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                return R * c;
            }
        }
    }
}