using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSM.Application.Interfaces;
using FSM.Domain.Entities;
using FSM.Domain.Interfaces;

// Alias to avoid ambiguity
using TaskEntity = FSM.Domain.Entities.Task;

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
            var currentSolution = CloneSolution(currentSchedule);
            var bestSolution = CloneSolution(currentSolution);

            double currentScore = _objectiveFunction.CalculateScore(currentSolution);
            double bestScore = currentScore;

            while (_temperature > _minTemperature && !cancellationToken.IsCancellationRequested)
            {
                var neighborSolution = CloneSolution(currentSolution);
                
                // Mutate the Lists (Move/Swap items), but DO NOT touch Task properties yet
                ApplySmartMutation(neighborSolution);

                double neighborScore = _objectiveFunction.CalculateScore(neighborSolution);

                if (AcceptanceProbability(currentScore, neighborScore, _temperature) > _random.NextDouble())
                {
                    currentSolution = neighborSolution;
                    currentScore = neighborScore;

                    if (currentScore < bestScore)
                    {
                        bestSolution = CloneSolution(currentSolution);
                        bestScore = currentScore;
                    }
                }

                _temperature *= _coolingRate;
                
                if (_temperature % 10 < 1) await System.Threading.Tasks.Task.Yield();
            }

            return bestSolution;
        }

        private double AcceptanceProbability(double currentScore, double neighborScore, double temp)
        {
            if (neighborScore < currentScore) return 1.0;
            return Math.Exp(-(neighborScore - currentScore) / temp);
        }

        private void ApplySmartMutation(List<TechnicianSchedule> solution)
        {
            if (_random.NextDouble() > 0.5) MoveTaskToQualifiedTechnician(solution);
            else SwapTaskOrderInternal(solution);
        }

        private void MoveTaskToQualifiedTechnician(List<TechnicianSchedule> solution)
        {
            var techWithTasks = solution.Where(s => s.Tasks.Count > 0).OrderBy(x => _random.Next()).FirstOrDefault();
            if (techWithTasks == null) return;

            // CAST to List to use indexer
            var sourceList = (List<TaskEntity>)techWithTasks.Tasks;
            var taskToMove = sourceList[_random.Next(sourceList.Count)];

            var validTargets = solution
                .Where(s => s.TechnicianId != techWithTasks.TechnicianId)
                // Check if target tech has the skill
                .Where(s => s.Technician != null && s.Technician.HasSkill(taskToMove.RequiredSkills))
                .ToList();

            if (validTargets.Count == 0) return;

            var targetSchedule = validTargets[_random.Next(validTargets.Count)];

            // Move the object from List A to List B
            // DO NOT update AssignedTechnicianId here. That happens in FsmService now.
            sourceList.Remove(taskToMove);
            targetSchedule.Tasks.Add(taskToMove);
        }

        private void SwapTaskOrderInternal(List<TechnicianSchedule> solution)
        {
            var candidate = solution.Where(s => s.Tasks.Count >= 2).OrderBy(x => _random.Next()).FirstOrDefault();
            if (candidate == null) return;

            // CAST to List to swap positions
            var taskList = (List<TaskEntity>)candidate.Tasks;

            int indexA = _random.Next(taskList.Count);
            int indexB = _random.Next(taskList.Count);
            while (indexA == indexB) indexB = _random.Next(taskList.Count);

            // Swap objects in the list
            // DO NOT update SequenceIndex here. That happens in FsmService now.
            var temp = taskList[indexA];
            taskList[indexA] = taskList[indexB];
            taskList[indexB] = temp;
        }

        private List<TechnicianSchedule> CloneSolution(List<TechnicianSchedule> source)
        {
            var newSolution = new List<TechnicianSchedule>();
            foreach (var s in source)
            {
                newSolution.Add(new TechnicianSchedule
                {
                    Id = s.Id,
                    TechnicianId = s.TechnicianId,
                    Technician = s.Technician,
                    Date = s.Date,
                    TotalDistance = s.TotalDistance,
                    // Create a NEW List, but share the Task Entities
                    Tasks = new List<TaskEntity>(s.Tasks) 
                });
            }
            return newSolution;
        }
    }
}