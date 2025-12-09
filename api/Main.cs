using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FSM.Application.Services;
using FSM.Domain.Entities;
using FSM.Domain.Enums;

namespace FSM.Api
{
    class Program
    {
        // Keep these public static so the Service can borrow them for seeding if needed
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
                Console.WriteLine($"Database: {service.Tasks.Count} Tasks, {service.Technicians.Count} Technicians");
                Console.WriteLine("-----------------------");
                Console.WriteLine("1. List All Tasks");
                Console.WriteLine("2. Add New Task");
                Console.WriteLine("3. Run Scheduler");
                Console.WriteLine("4. Exit");
                Console.Write("Select: ");
                
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        foreach (var t in service.Tasks)
                        {
                            Console.WriteLine($"[ID: {t.Id}] {t.ClientName} - {t.Priority} (Window: {t.TimeWindowStart.Hour}-{t.TimeWindowEnd.Hour})");
                        }
                        Console.ReadKey();
                        break;

                    case "2":
                        AddNewTaskUI(service);
                        break;

                    case "3":
                        var results = await service.RunOptimizationAsync();
                        PrintResults(results);
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
            var t = new FSM.Domain.Entities.Task();
            
            Console.Write("Client Name: ");
            t.ClientName = Console.ReadLine();
            
            Console.Write("Duration (hours): ");
            if(double.TryParse(Console.ReadLine(), out double dur)) t.Duration = TimeSpan.FromHours(dur);
            
            // Simplified Inputs for Demo
            t.Priority = TaskPriority.Regular; 
            t.RequiredSkills = SkillSet.General; 
            t.TimeWindowStart = DateTime.Today.AddHours(9); 
            t.TimeWindowEnd = DateTime.Today.AddHours(17);
            
            // Hardcoded location for simplicity (Tel Aviv center)
            t.Latitude = 32.0853; 
            t.Longitude = 34.7818;

            service.AddTask(t);
            Console.WriteLine("Task Saved!");
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