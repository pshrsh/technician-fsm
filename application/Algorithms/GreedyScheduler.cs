using System;
using System.Collections.Generic;
using System.Linq;
using FSM.Application.Interfaces;
using FSM.Application.DTOs;
using FSM.Application.Utilities;
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

            // Sort: Urgent first, then by earliest Window End (if exists)
            var sortedTasks = tasks
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.WindowEnd ?? TimeSpan.MaxValue) 
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
                    
                    // 2. Time Check with Break
                    DateTime availableTime = lastTask?.ActualEndTime ?? schedule.Date.Add(tech.ShiftStart);
                    TimeSpan availableTimeOfDay = availableTime.TimeOfDay;
                    
                    // Adjust for break if needed
                    availableTimeOfDay = BreakTimeHelper.AdjustForBreak(availableTimeOfDay);
                    availableTime = schedule.Date.Add(availableTimeOfDay);
                    
                    double distKm = CalculateDistance(startLat, startLon, task.Latitude, task.Longitude);
                    double travelMinutes = (distKm / tech.EstimatedTravelSpeedKmH) * 60;

                    DateTime arrivalTime = availableTime.AddMinutes(travelMinutes);
                    TimeSpan arrivalTimeOfDay = arrivalTime.TimeOfDay;
                    
                    // Adjust arrival time for break
                    arrivalTimeOfDay = BreakTimeHelper.AdjustForBreak(arrivalTimeOfDay);
                    arrivalTime = schedule.Date.Add(arrivalTimeOfDay);

                    DateTime windowStart = task.WindowStart.HasValue 
                        ? schedule.Date.Add(task.WindowStart.Value) 
                        : DateTime.MinValue;

                    DateTime windowEnd = task.WindowEnd.HasValue 
                        ? schedule.Date.Add(task.WindowEnd.Value) 
                        : DateTime.MaxValue;

                    // If we arrive early, we wait until the window starts
                    DateTime startTask = arrivalTime < windowStart ? windowStart : arrivalTime;
                    TimeSpan startTaskTimeOfDay = startTask.TimeOfDay;
                    
                    // Calculate finish time accounting for break
                    TimeSpan finishTimeOfDay = BreakTimeHelper.CalculateEndTimeWithBreak(startTaskTimeOfDay, task.Duration);
                    DateTime finishTime = schedule.Date.Add(finishTimeOfDay);

                    DateTime shiftEnd = schedule.Date.Add(tech.ShiftEnd);

                    // Constraints
                    if (finishTime > shiftEnd) continue; // Shift Over
                    if (startTask > windowEnd) continue; // Window Missed (Too Late)
                    // -----------------------------

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
                    
                    // Calculate end time with break consideration
                    TimeSpan startTimeOfDay = bestStartTime.TimeOfDay;
                    TimeSpan endTimeOfDay = BreakTimeHelper.CalculateEndTimeWithBreak(startTimeOfDay, task.Duration);
                    task.ActualEndTime = bestSchedule.Date.Add(endTimeOfDay);
                    
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