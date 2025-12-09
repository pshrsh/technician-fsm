using System;
using System.Collections.Generic;
using System.Linq;
using FSM.Application.Interfaces;
using FSM.Domain.Entities;
using FSM.Domain.Enums;
using Task = FSM.Domain.Entities.Task; 

namespace FSM.Application.Algorithms
{
    public class GreedyScheduler : IInitialScheduler
    {
        public List<TechnicianSchedule> GenerateInitialSchedule(List<Technician> technicians, List<Task> tasks)
        {
            var schedules = InitializeSchedules(technicians);
            var unassignedTasks = new List<Task>();

            // Sort tasks: Urgent first, then by earliest deadline
            var sortedTasks = tasks
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.TimeWindowEnd)
                .ToList();

            foreach (var task in sortedTasks)
            {
                TechnicianSchedule bestSchedule = null;
                double bestScore = double.MaxValue; 
                DateTime bestStartTime = DateTime.MaxValue;

                foreach (var schedule in schedules)
                {
                    var tech = schedule.Technician;
                    if (!tech.HasSkill(task.RequiredSkills)) continue;

                    var lastTask = schedule.Tasks.LastOrDefault();
                    
                    double startLat = lastTask?.Latitude ?? tech.BaseLatitude;
                    double startLon = lastTask?.Longitude ?? tech.BaseLongitude;
                    DateTime availableTime = lastTask?.ActualEndTime ?? DateTime.Today.Add(tech.ShiftStart);

                    // Simple Distance Calc
                    double distanceKm = CalculateDistance(startLat, startLon, task.Latitude, task.Longitude);
                    double travelMinutes = (distanceKm / tech.EstimatedTravelSpeedKmH) * 60;
                    
                    DateTime arrivalTime = availableTime.AddMinutes(travelMinutes);
                    DateTime startTaskTime = arrivalTime < task.TimeWindowStart ? task.TimeWindowStart : arrivalTime;
                    DateTime finishTime = startTaskTime.Add(task.Duration);

                    // Constraints
                    DateTime shiftEnd = DateTime.Today.Add(tech.ShiftEnd);
                    if (finishTime > shiftEnd) continue;
                    if (startTaskTime > task.TimeWindowEnd) continue;

                    double score = distanceKm; 
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestSchedule = schedule;
                        bestStartTime = startTaskTime;
                    }
                }

                if (bestSchedule != null)
                {
                    task.AssignedTechnicianId = bestSchedule.TechnicianId;
                    task.ActualStartTime = bestStartTime;
                    task.ActualEndTime = bestStartTime.Add(task.Duration);
                    task.SequenceIndex = bestSchedule.Tasks.Count + 1;
                    task.Status = FSM.Domain.Enums.TaskStatus.Scheduled;

                    bestSchedule.Tasks.Add(task);
                    bestSchedule.TotalDistance += bestScore;
                }
                else
                {
                    unassignedTasks.Add(task);
                }
            }
            return schedules;
        }

        private List<TechnicianSchedule> InitializeSchedules(List<Technician> technicians)
        {
            var list = new List<TechnicianSchedule>();
            foreach (var tech in technicians)
            {
                list.Add(new TechnicianSchedule
                {
                    TechnicianId = tech.Id,
                    Technician = tech,
                    Date = DateTime.Today,
                    Tasks = new List<Task>()
                });
            }
            return list;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Simple Euclidean
            var dLat = lat2 - lat1;
            var dLon = lon2 - lon1;
            return Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;
        }
    }
}