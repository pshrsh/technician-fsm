using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSM.Application.Algorithms;
using FSM.Domain.Entities;
using FSM.Infrastructure.Persistence;

// Alias to distinguish between System.Threading.Tasks.Task and your Entity Task
using TaskEntity = FSM.Domain.Entities.Task; 

namespace FSM.Application.Services
{
    public class FsmService
    {
        private readonly JsonRepository<Technician> _techRepo;
        private readonly JsonRepository<TaskEntity> _taskRepo;
        
        private readonly GreedyScheduler _greedy;
        private readonly SimulatedAnnealingEngine _optimizer;
        private readonly WeightedObjectiveFunction _objective;

        public List<Technician> Technicians { get; private set; }
        public List<TaskEntity> Tasks { get; private set; }

        public FsmService()
        {
            _techRepo = new JsonRepository<Technician>("technicians.json");
            _taskRepo = new JsonRepository<TaskEntity>("tasks.json");

            _objective = new WeightedObjectiveFunction();
            _greedy = new GreedyScheduler();
            _optimizer = new SimulatedAnnealingEngine(_objective);

            LoadData();
        }

        public void LoadData()
        {
            Technicians = _techRepo.Load();
            Tasks = _taskRepo.Load();

            // --- SELF-HEALING LOGIC ---
            // Check if data is empty OR if technicians are "Broken" (No Skills / No Shift)
            bool isBroken = Technicians.Any(t => t.Skills == FSM.Domain.Enums.SkillSet.None || t.ShiftEnd == TimeSpan.Zero);

            if (!Technicians.Any() || isBroken)
            {
                Console.WriteLine("⚠️ Invalid or Empty Data detected. Resetting Technicians...");
                
                Technicians.Clear();
                
                // Add Default Technicians (With Skills & Shifts thanks to the new Constructor)
                AddTechnician(new Technician { Name = "David (Tel Aviv)", BaseLatitude = 32.0853, BaseLongitude = 34.7818 });
                AddTechnician(new Technician { Name = "Sarah (Haifa)", BaseLatitude = 32.7940, BaseLongitude = 34.9896 });
                AddTechnician(new Technician { Name = "Danny (Jerusalem)", BaseLatitude = 31.7683, BaseLongitude = 35.2137 });

                _techRepo.Save(Technicians);
            }
        }

        //  technician methods
        public List<Technician> GetAllTechnicians() => Technicians;

        public Technician AddTechnician(Technician tech)
        {
            int newId = Technicians.Any() ? Technicians.Max(t => t.Id) + 1 : 1;
            tech.Id = newId;
            Technicians.Add(tech);
            _techRepo.Save(Technicians);
            return tech;
        }
        public bool DeleteTechnician(int id)
        {
            var tech = Technicians.FirstOrDefault(t => t.Id == id);
            if (tech == null) return false;
            Technicians.Remove(tech);
            _techRepo.Save(Technicians);
            return true;
        }
        // task methods
        public List<TaskEntity> GetAllTasks() => Tasks;
        public void AddTask(TaskEntity task)
        {
            int newId = Tasks.Any() ? Tasks.Max(t => t.Id) + 1 : 1;
            task.Id = newId;
            Tasks.Add(task);
            _taskRepo.Save(Tasks);
        }

        public bool DeleteTask(int id)
        {
            var task = Tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return false;
            Tasks.Remove(task);
            _taskRepo.Save(Tasks);
            return true;
        }

        public async System.Threading.Tasks.Task<List<TechnicianSchedule>> RunOptimizationAsync()
        {
            Console.WriteLine($"[Optimizer] Technicians: {Technicians.Count}, Tasks: {Tasks.Count}");
            
            Console.WriteLine("Generating Initial Schedule...");
            var initialResult = _greedy.GenerateInitialSchedule(Technicians, Tasks);
            
            Console.WriteLine("Optimizing Routes...");
            // 1. Run the Optimizer (It only rearranges lists in memory)
            var finalSolution = await _optimizer.OptimizeScheduleAsync(initialResult.Schedules, CancellationToken.None);
            
            // 2. COMMIT PHASE: Apply the results to the Real Data
            foreach (var schedule in finalSolution)
            {
                int sequence = 1;
                foreach (var optimizedTask in schedule.Tasks)
                {
                    // Find the real task in our main list
                    var realTask = Tasks.FirstOrDefault(t => t.Id == optimizedTask.Id);
                    if (realTask != null)
                    {
                        realTask.AssignedTechnicianId = schedule.TechnicianId;
                        realTask.SequenceIndex = sequence++;
                        realTask.Status = FSM.Domain.Enums.TaskStatus.Scheduled;
                    }
                }
            }

            // 3. Save to JSON so the Frontend sees it
            _taskRepo.Save(Tasks);
            Console.WriteLine("Optimization Saved to Database.");

            return finalSolution;
        }
    }
}