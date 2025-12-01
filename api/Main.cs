using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FSM.Application.Algorithms;
using FSM.Domain.Entities;
using FSM.Domain.Enums;
using Domain.Entities; // For Task
using Domain.Enums;    // For TaskStatus

namespace FSM.Api
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("--- Field Service Management Scheduler ---");

            // 1. Setup Dummy Data
            var technicians = GetDummyTechnicians();
            var tasks = GetDummyTasks();

            // 2. Initialize Components
            var objectiveFunc = new WeightedObjectiveFunction();
            var greedy = new GreedyScheduler();
            var optimizer = new SimulatedAnnealingEngine(objectiveFunc);

            // 3. Run Initial Greedy Solution
            Console.WriteLine("\n[1] Running Greedy Algorithm...");
            var initialSolution = greedy.GenerateInitialSchedule(technicians, tasks);
            
            PrintScore("Greedy", initialSolution, objectiveFunc);

            // 4. Run Optimization (Simulated Annealing)
            Console.WriteLine("\n[2] Running Simulated Annealing Optimization...");
            var optimizedSolution = await optimizer.OptimizeScheduleAsync(initialSolution, CancellationToken.None);

            PrintScore("Optimized", optimizedSolution, objectiveFunc);
            
            // 5. Show Routes
            Console.WriteLine("\n--- Final Routes ---");
            foreach(var sched in optimizedSolution)
            {
                Console.WriteLine($"Technician: {sched.Technician.Name}");
                foreach(var t in sched.Tasks)
                {
                    Console.WriteLine($"  - Task {t.Id} (Client: {t.ClientName})");
                }
            }
        }

        static void PrintScore(string phase, List<TechnicianSchedule> solution, WeightedObjectiveFunction scorer)
        {
            var breakdown = scorer.GetDetailedScore(solution);
            Console.WriteLine($"--- {phase} Results ---");
            Console.WriteLine($"   Total Score (Cost): {breakdown.FinalWeightedScore:F2}");
            Console.WriteLine($"   Travel Time: {breakdown.TotalTravelTime:F1} min");
            Console.WriteLine($"   Delays: {breakdown.TotalDelay:F1} min");
        }

        static List<Technician> GetDummyTechnicians()
        {
            return new List<Technician>
            {
                new Technician 
                { 
                    Id = 1, Name = "Dvir (Elec)", 
                    Skills = SkillSet.Electric | SkillSet.General,
                    BaseLatitude = 32.1, BaseLongitude = 34.8, // Tel Aviv area approx
                    ShiftStart = TimeSpan.FromHours(8), ShiftEnd = TimeSpan.FromHours(17),
                    EstimatedTravelSpeedKmH = 30
                },
                new Technician 
                { 
                    Id = 2, Name = "John (Plumb)", 
                    Skills = SkillSet.Plumbing | SkillSet.General,
                    BaseLatitude = 32.2, BaseLongitude = 34.9, 
                    ShiftStart = TimeSpan.FromHours(8), ShiftEnd = TimeSpan.FromHours(16),
                    EstimatedTravelSpeedKmH = 35
                }
            };
        }

        static List<Task> GetDummyTasks()
        {
            var today = DateTime.Today;
            return new List<Task>
            {
                new Task { Id=101, ClientName="Client A", Priority=TaskPriority.High, Duration=TimeSpan.FromHours(1), TimeWindowStart=today.AddHours(9), TimeWindowEnd=today.AddHours(12), RequiredSkills=SkillSet.Electric, Latitude=32.12, Longitude=34.82 },
                new Task { Id=102, ClientName="Client B", Priority=TaskPriority.Regular, Duration=TimeSpan.FromHours(2), TimeWindowStart=today.AddHours(10), TimeWindowEnd=today.AddHours(14), RequiredSkills=SkillSet.Plumbing, Latitude=32.22, Longitude=34.92 },
                new Task { Id=103, ClientName="Client C", Priority=TaskPriority.Urgent, Duration=TimeSpan.FromHours(0.5), TimeWindowStart=today.AddHours(8), TimeWindowEnd=today.AddHours(10), RequiredSkills=SkillSet.General, Latitude=32.11, Longitude=34.81 }
            };
        }
    }
}