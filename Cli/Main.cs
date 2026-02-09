using System;
using System.Collections.Generic;
using System.Threading.Tasks; 
using FSM.Application.Services;
using FSM.Domain.Entities;
using FSM.Domain.Enums;

using TaskEntity = FSM.Domain.Entities.Task;

namespace FSM.Cli
{
    class Program
    {
        public static List<Technician> GetDummyTechnicians()
        {
            return new List<Technician>
            {
                new Technician 
                { 
                    Id = 1, Name = "Dvir (Elec)", 
                    Skills = SkillSet.Electric | SkillSet.General,
                    BaseLatitude = 32.1, BaseLongitude = 34.8, 
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

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            var service = new FsmService();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== FSM SCHEDULER CLI ===");
                Console.WriteLine($"[Storage] Loaded {service.Tasks.Count} Tasks and {service.Technicians.Count} Technicians from JSON.");
                Console.WriteLine("-----------------------");
                Console.WriteLine("1. List All Tasks (From JSON)");
                Console.WriteLine("2. Add New Task (Save to JSON)");
                Console.WriteLine("3. Run Scheduler (On Loaded Tasks)");
                Console.WriteLine("4. Exit");
                Console.Write("Select: ");
                
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("\n--- Current Task List ---");
                        if (service.Tasks.Count == 0) Console.WriteLine("No tasks found.");
                        
                        foreach (var t in service.Tasks)
                        {
                            // Display logic for nullable TimeSpan
                            string start = t.WindowStart.HasValue ? t.WindowStart.Value.ToString(@"hh\:mm") : "Any";
                            string end = t.WindowEnd.HasValue ? t.WindowEnd.Value.ToString(@"hh\:mm") : "Any";
                            Console.WriteLine($"[ID: {t.Id}] {t.ClientName} - {t.Priority} (Window: {start}-{end})");
                        }
                        Console.WriteLine("\nPress any key...");
                        Console.ReadKey();
                        break;

                    case "2":
                        AddNewTaskUI(service);
                        break;

                    case "3":
                        if (service.Tasks.Count == 0)
                        {
                            Console.WriteLine("\nNo tasks to schedule! Add a task first.");
                            Console.ReadKey();
                            break;
                        }

                        Console.WriteLine("\nRunning optimization on loaded tasks...");
                        var results = await service.RunOptimizationAsync();
                        PrintResults(results);
                        
                        Console.WriteLine("\nPress any key...");
                        Console.ReadKey();
                        break;

                    case "4":
                        return;
                }
            }
        }

        static void AddNewTaskUI(FsmService service)
        {
            Console.WriteLine("\n--- New Task ---");
            var t = new TaskEntity();
            
            Console.Write("Client Name: ");
            t.ClientName = Console.ReadLine();
            
            Console.Write("Duration (hours): ");
            if(double.TryParse(Console.ReadLine(), out double dur)) t.Duration = TimeSpan.FromHours(dur);
            
            t.Priority = TaskPriority.Regular; 
            t.RequiredSkills = SkillSet.General; 
            
            // FIX: Using TimeSpan for Windows
            t.WindowStart = TimeSpan.FromHours(9); // 09:00
            t.WindowEnd = TimeSpan.FromHours(17);  // 17:00
            
            t.Latitude = 32.0853; 
            t.Longitude = 34.7818;

            service.AddTask(t);
            Console.WriteLine("Task Saved to JSON successfully!");
            System.Threading.Thread.Sleep(1000);
        }

        static void PrintResults(List<TechnicianSchedule> schedules)
        {
            Console.WriteLine("\n--- OPTIMIZED ROUTES ---");
            foreach (var s in schedules)
            {
                Console.WriteLine($"\nTechnician: {s.Technician.Name} (Count: {s.Tasks.Count})");
                foreach (var t in s.Tasks)
                {
                    Console.WriteLine($"   -> {t.ActualStartTime?.ToString("HH:mm")} : {t.ClientName}");
                }
            }
        }
    }
}