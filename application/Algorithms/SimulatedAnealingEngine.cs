using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace FSM.Application.Algorithms
{
    public class SimulatedAnnealingEngine : IOptimizationEngine
    {
        private readonly IAlgorithmGoal _objectiveFunction;
        private readonly Random _random = new Random();

        private const double InitialTemperature = 1000.0;
        private const double CoolingRate = 0.995; 
        private const double AbsoluteZero = 0.1;

        public SimulatedAnnealingEngine(IAlgorithmGoal objectiveFunction)
        {
            _objectiveFunction = objectiveFunction;
        }

        public async Task<List<TechnicianSchedule>> OptimizeScheduleAsync(
            List<TechnicianSchedule> currentSchedule, 
            CancellationToken cancellationToken)
        {
            var currentSolution = DeepCloneSolution(currentSchedule);
            double currentScore = _objectiveFunction.CalculateScore(currentSolution);

            var bestSolution = DeepCloneSolution(currentSolution);
            double bestScore = currentScore;

            double temperature = InitialTemperature;

            // optimize until we reach absolute 0 or the user cancels
            while (temperature > AbsoluteZero && !cancellationToken.IsCancellationRequested)
            {
                // clone the current state to change safely
                var candidateSolution = DeepCloneSolution(currentSolution);
                
                ApplyRandomMutation(candidateSolution);

                // check the new state
                double candidateScore = _objectiveFunction.CalculateScore(candidateSolution);

                // choose which state is better
                if (AcceptanceProbability(currentScore, candidateScore, temperature) > _random.NextDouble())
                {
                    currentSolution = candidateSolution;
                    currentScore = candidateScore;

                    // Keep track of the all-time best
                    if (currentScore < bestScore)
                    {
                        bestSolution = DeepCloneSolution(currentSolution);
                        bestScore = currentScore;
                    }
                }

                // Cool down
                temperature *= CoolingRate;

                // pause/yield once in a while
                if (_random.Next(0, 100) == 0) await Task.Yield(); 
            }

            return bestSolution;
        }

        // --- Helper: The Metropolis Acceptance Criterion ---
        private double AcceptanceProbability(double currentScore, double newScore, double temperature)
        {
            // If the new solution is better (lower score), accept it 100%
            if (newScore < currentScore) return 1.0;

            // If it's worse, accept it with a probability based on Temperature
            // High Temp = High probability of accepting bad moves
            // Low Temp = Low probability
            return Math.Exp((currentScore - newScore) / temperature);
        }

        private void ApplyRandomMutation(List<TechnicianSchedule> solution)
        {
            // pick a random task and move it to a different technician
            
            // flatten all tasks to pick one
            var allTasks = solution.SelectMany(s => s.Tasks).ToList();
            if (allTasks.Count == 0) return;

            var taskToMove = allTasks[_random.Next(allTasks.Count)];
            int oldTechId = taskToMove.AssignedTechnicianId ?? 0;

            // find a compatible different technician
            var availableTechs = solution
                .Where(s => s.TechnicianId != oldTechId && 
                            s.Technician.HasSkill(taskToMove.RequiredSkills))
                .ToList();

            if (availableTechs.Count == 0) return; // No other valid tech found

            var newSchedule = availableTechs[_random.Next(availableTechs.Count)];
            var oldSchedule = solution.First(s => s.TechnicianId == oldTechId);

            // swapping mechanism
            // Remove from old
            var taskInOldList = oldSchedule.Tasks.First(t => t.Id == taskToMove.Id);
            oldSchedule.Tasks.Remove(taskInOldList);

            // add to new (basically randomly)
            // append and make the scoring function handle the delay calculation
            newSchedule.Tasks.Add(taskInOldList);

            // update the task's internal state
            taskInOldList.AssignedTechnicianId = newSchedule.TechnicianId;
            taskInOldList.AssignedTechnician = newSchedule.Technician;
            
            // theres no recalculating specific start times,  
            // in a full implementation i might re-run a "Route Sequencer" on the modified schedule
            // to update arrival Times before calculating the score.
            UpdateRouteTimes(newSchedule);
        }

        private void UpdateRouteTimes(TechnicianSchedule schedule)
        {
            // update ActualStartTime based on the new sequence
            // ensures ObjectiveFunction evaluates valid times
            if (!schedule.Tasks.Any()) return;

            DateTime currentTime = schedule.Date.Add(schedule.Technician.ShiftStart);
            double currentLat = schedule.Technician.BaseLatitude;
            double currentLon = schedule.Technician.BaseLongitude;

            foreach (var task in schedule.Tasks) // assume order is roughly maintained or simple append
            {
                // calc Travel
                double dist = EstimateDistance(currentLat, currentLon, task.Latitude, task.Longitude);
                double travelMins = (dist / schedule.Technician.EstimatedTravelSpeedKmH) * 60;
                
                currentTime = currentTime.AddMinutes(travelMins);

                // maybe wait for window
                if (currentTime < task.TimeWindowStart) currentTime = task.TimeWindowStart;

                task.ActualStartTime = currentTime;
                task.ActualEndTime = currentTime.Add(task.Duration);

                // set up for next loop
                currentTime = task.ActualEndTime.Value;
                currentLat = task.Latitude;
                currentLon = task.Longitude;
            }
            
            // update total distance for the schedule (simplified)
            schedule.TotalDistance += 10; // Placeholder update
        }

        // DO NOT modify the objects in place, or we destroy the rollback capability
        private List<TechnicianSchedule> DeepCloneSolution(List<TechnicianSchedule> source)
        {
            var newSolution = new List<TechnicianSchedule>();
            foreach (var s in source)
            {
                var newSch = new TechnicianSchedule
                {
                    Id = s.Id,
                    TechnicianId = s.TechnicianId,
                    Technician = s.Technician, // tech entity is static config
                    Date = s.Date,
                    TotalDistance = s.TotalDistance,
                    TotalDelay = s.TotalDelay,
                    Tasks = new List<Task>()
                };

                // clone Tasks
                foreach (var t in s.Tasks)
                {
                    newSch.Tasks.Add(new Task
                    {
                        Id = t.Id,
                        ClientName = t.ClientName,
                        Address = t.Address,
                        Latitude = t.Latitude,
                        Longitude = t.Longitude,
                        Duration = t.Duration,
                        TimeWindowStart = t.TimeWindowStart,
                        TimeWindowEnd = t.TimeWindowEnd,
                        Priority = t.Priority,
                        RequiredSkills = t.RequiredSkills,
                        AssignedTechnicianId = newSch.TechnicianId, // Link to new parent
                        AssignedTechnician = newSch.Technician,
                        ActualStartTime = t.ActualStartTime,
                        ActualEndTime = t.ActualEndTime,
                        Status = t.Status
                    });
                }
                newSolution.Add(newSch);
            }
            return newSolution;
        }

        private double EstimateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // simple Euclidian for speed
             var dLat = lat2 - lat1;
            var dLon = lon2 - lon1;
            return Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;
        }
    }
}