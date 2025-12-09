using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSM.Application.Algorithms;
using FSM.Domain.Entities;
using FSM.Infrastructure.Persistence; // Ensure you add the reference to Infrastructure
using TaskEntity = FSM.Domain.Entities.Task; // Alias to avoid confusion with System.Threading.Tasks.Task

namespace FSM.Application.Services
{
    public class FsmService
    {
        private readonly JsonRepository<Technician> _techRepo;
        private readonly JsonRepository<TaskEntity> _taskRepo;
        
        // Algorithms
        private readonly GreedyScheduler _greedy;
        private readonly SimulatedAnnealingEngine _optimizer;
        private readonly WeightedObjectiveFunction _objective;

        public List<Technician> Technicians { get; private set; }
        public List<TaskEntity> Tasks { get; private set; }

        public FsmService()
        {
            _techRepo = new JsonRepository<Technician>("technicians.json");
            _taskRepo = new JsonRepository<TaskEntity>("tasks.json");

            // Initialize Algorithms
            _objective = new WeightedObjectiveFunction();
            _greedy = new GreedyScheduler();
            _optimizer = new SimulatedAnnealingEngine(_objective);

            LoadData();
        }

        public void LoadData()
        {
            Technicians = _techRepo.Load();
            Tasks = _taskRepo.Load();
            
            // Seed data if empty (for first run)
            if (!Technicians.Any())
            {
                Technicians = FSM.Api.Program.GetDummyTechnicians(); // Reusing your existing dummy generator
                _techRepo.Save(Technicians);
            }
        }

        public void AddTask(TaskEntity task)
        {
            // Simple ID generation
            task.Id = Tasks.Any() ? Tasks.Max(t => t.Id) + 1 : 100;
            Tasks.Add(task);
            _taskRepo.Save(Tasks);
        }

        public async Task<List<TechnicianSchedule>> RunOptimizationAsync()
        {
            Console.WriteLine("Generating Initial Schedule (Greedy)...");
            var initialSolution = _greedy.GenerateInitialSchedule(Technicians, Tasks);
            
            Console.WriteLine("Optimizing (Simulated Annealing)...");
            var finalSolution = await _optimizer.OptimizeScheduleAsync(initialSolution, CancellationToken.None);
            
            return finalSolution;
        }
    }
}