using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSM.Application.Interfaces;
using FSM.Domain.Entities;
using FSM.Domain.Interfaces;

namespace FSM.Application.Algorithms
{
    public class SimulatedAnnealingEngine : IOptimizationEngine
    {
        private readonly IAlgorithmGoal _objectiveFunction;
        private readonly Random _random = new Random();

        // SA Parameters
        private double _temperature = 1000.0;
        private readonly double _coolingRate = 0.995;
        private readonly double _minTemperature = 0.1;

        public SimulatedAnnealingEngine(IAlgorithmGoal objectiveFunction)
        {
            _objectiveFunction = objectiveFunction;
        }

        public async Task<List<TechnicianSchedule>> OptimizeScheduleAsync(
            List<TechnicianSchedule> currentSchedule, 
            CancellationToken cancellationToken)
        {
            // Clone the initial solution so we don't mutate the original reference immediately
            var currentSolution = CloneSolution(currentSchedule);
            var bestSolution = CloneSolution(currentSolution);

            double currentScore = _objectiveFunction.CalculateScore(currentSolution);
            double bestScore = currentScore;

            // Main SA Loop
            while (_temperature > _minTemperature && !cancellationToken.IsCancellationRequested)
            {
                // Create a neighbor solution (Mutate)
                var neighborSolution = CloneSolution(currentSolution);
                ApplyRandomMutation(neighborSolution);

                // Calculate new score
                double neighborScore = _objectiveFunction.CalculateScore(neighborSolution);

                // Acceptance Probability
                // We want to MINIMIZE the score (Cost function)
                if (AcceptanceProbability(currentScore, neighborScore, _temperature) > _random.NextDouble())
                {
                    currentSolution = neighborSolution;
                    currentScore = neighborScore;

                    // Keep track of the absolute best we've seen
                    if (currentScore < bestScore)
                    {
                        bestSolution = CloneSolution(currentSolution);
                        bestScore = currentScore;
                    }
                }

                // Cool down
                _temperature *= _coolingRate;
                
                // Yield control periodically to keep UI responsive if needed
                if (_temperature % 10 < 1) await System.Threading.Tasks.Task.Yield();
            }

            return bestSolution;
        }

        private double AcceptanceProbability(double currentScore, double neighborScore, double temp)
        {
            // If neighbor is better (lower cost), probability is 1.0
            if (neighborScore < currentScore) return 1.0;

            // If neighbor is worse, accept with probability exp(-(new - old) / temp)
            return Math.Exp(-(neighborScore - currentScore) / temp);
        }

        private void ApplyRandomMutation(List<TechnicianSchedule> solution)
        {
            // Simple mutation: Move a task from one tech to another, or swap order
            // Pick two random technicians
            var techA = solution[_random.Next(solution.Count)];
            var techB = solution[_random.Next(solution.Count)];

            if (techA.Tasks.Count == 0) return;

            // Pick a random task from Tech A
            var taskListA = techA.Tasks.ToList();
            var taskToMove = taskListA[_random.Next(taskListA.Count)];

            // Move it to Tech B
            techA.Tasks.Remove(taskToMove);
            techB.Tasks.Add(taskToMove);

            // Re-assign ID
            taskToMove.AssignedTechnicianId = techB.TechnicianId;
        }

        // Deep copy helper to avoid reference issues
        private List<TechnicianSchedule> CloneSolution(List<TechnicianSchedule> source)
        {
            // In a real production app, use a proper Mapper or serialization
            // This is a manual simplified deep clone for the algorithm logic
            var newSolution = new List<TechnicianSchedule>();
            foreach (var s in source)
            {
                var newSch = new TechnicianSchedule
                {
                    TechnicianId = s.TechnicianId,
                    Technician = s.Technician, 
                    Date = s.Date,
                    TotalDistance = s.TotalDistance,
                    Tasks = new List<FSM.Domain.Entities.Task>(s.Tasks) // Shallow copy of the list is okay for swapping logic usually, but be careful
                };
                newSolution.Add(newSch);
            }
            return newSolution;
        }
    }
}