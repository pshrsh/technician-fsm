using System;
using System.Collections.Generic;
using System.Linq;
using FSM.Application.Interfaces;
using FSM.Application.DTOs; // Add this
using FSM.Domain.Entities;
using FSM.Domain.Enums;
using TaskEntity = FSM.Domain.Entities.Task;

namespace FSM.Application.Algorithms
{
    public class GreedyScheduler : IInitialScheduler
    {
        public SchedulerResult GenerateInitialSchedule(List<Technician> technicians, List<TaskEntity> tasks)
        {
            var schedules = InitializeSchedules(technicians);
            var unassigned = new List<TaskEntity>();

            // Sort: Urgent first, then by earliest deadline
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
                    
                    // 1. Skill Check
                    if (!tech.HasSkill(task.RequiredSkills)) continue;

                    var lastTask = schedule.Tasks.LastOrDefault();
                    double startLat = lastTask?.Latitude ?? tech.BaseLatitude;
                    double startLon = lastTask?.Longitude ?? tech.BaseLongitude;
                    
                    // 2. Time Check
                    DateTime availableTime = lastTask?.ActualEndTime ?? schedule.Date.Add(tech.ShiftStart);
                    
                    double distKm = CalculateDistance(startLat, startLon, task.Latitude, task.Longitude);
                    double travelMinutes = (distKm / tech.EstimatedTravelSpeedKmH) * 60;

                    DateTime arrivalTime = availableTime.AddMinutes(travelMinutes);
                    DateTime startTask = arrivalTime < task.TimeWindowStart ? task.TimeWindowStart : arrivalTime;
                    DateTime finishTime = startTask.Add(task.Duration);

                    DateTime shiftEnd = schedule.Date.Add(tech.ShiftEnd);

                    // Constraints
                    if (finishTime > shiftEnd) continue; // Shift Over
                    if (startTask > task.TimeWindowEnd) continue; // Window Missed

                    if (distKm < bestScore)
                    {
                        bestScore = distKm;
                        bestSchedule = schedule;
                        bestStartTime = startTask;
                    }
                }

                if (bestSchedule != null)
                {
                    task.AssignedTechnicianId = bestSchedule.TechnicianId;
                    task.ActualStartTime = bestStartTime;
                    task.ActualEndTime = bestStartTime.Add(task.Duration);
                    task.Status = FSM.Domain.Enums.TaskStatus.Scheduled;

                    bestSchedule.Tasks.Add(task);
                    bestSchedule.TotalDistance += bestScore;
                }
                else
                {
                    task.Status = FSM.Domain.Enums.TaskStatus.Pending;
                    unassigned.Add(task);
                }
            }

            // FIX: Return the composite result
            return new SchedulerResult 
            { 
                Schedules = schedules, 
                UnscheduledTasks = unassigned 
            };
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
                    Tasks = new List<TaskEntity>()
                });
            }
            return list;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = lat2 - lat1;
            var dLon = lon2 - lon1;
            return Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;
        }
    }
}