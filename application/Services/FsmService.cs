using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSM.Application.Algorithms;
using FSM.Domain.Entities;
using FSM.Infrastructure.Persistence;

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

            bool isBroken = Technicians.Any(t => t.Skills == FSM.Domain.Enums.SkillSet.None || t.ShiftEnd == TimeSpan.Zero);
            if (!Technicians.Any() || isBroken)
            {
                Technicians.Clear();
                AddTechnician(new Technician { Name = "David (Tel Aviv)", BaseLatitude = 32.0853, BaseLongitude = 34.7818 });
                AddTechnician(new Technician { Name = "Sarah (Haifa)", BaseLatitude = 32.7940, BaseLongitude = 34.9896 });
                _techRepo.Save(Technicians);
            }
        }

        public List<Technician> GetAllTechnicians() => Technicians;
        
        public Technician AddTechnician(Technician tech)
        {
            if (tech.Skills == FSM.Domain.Enums.SkillSet.None) tech.Skills = FSM.Domain.Enums.SkillSet.General;
            tech.Id = Technicians.Any() ? Technicians.Max(t => t.Id) + 1 : 1;
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

        public List<TaskEntity> GetAllTasks() => Tasks;

        public void AddTask(TaskEntity task)
        {
            task.Id = Tasks.Any() ? Tasks.Max(t => t.Id) + 1 : 1;
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
            Console.WriteLine("Generating Initial Schedule...");
            var initialResult = _greedy.GenerateInitialSchedule(Technicians, Tasks);
            
            Console.WriteLine("Optimizing Routes...");
            var finalSolution = await _optimizer.OptimizeScheduleAsync(initialResult.Schedules, CancellationToken.None);
            
            // --- COMMIT PHASE ---
            foreach (var schedule in finalSolution)
            {
                int sequence = 1;
                var tech = schedule.Technician;
                TimeSpan currentTime = tech.ShiftStart;
                double currentLat = tech.BaseLatitude;
                double currentLon = tech.BaseLongitude;

                foreach (var optimizedTask in schedule.Tasks)
                {
                    var realTask = Tasks.FirstOrDefault(t => t.Id == optimizedTask.Id);
                    if (realTask != null)
                    {
                        // 1. Calculate Travel
                        double dist = GetDist(currentLat, currentLon, realTask.Latitude, realTask.Longitude);
                        double travelHours = dist / tech.EstimatedTravelSpeedKmH;
                        currentTime = currentTime.Add(TimeSpan.FromHours(travelHours));

                        // 2. Adjust for Start Window
                        if (realTask.WindowStart.HasValue && currentTime < realTask.WindowStart.Value)
                        {
                            currentTime = realTask.WindowStart.Value;
                        }

                        // 3. Save Times
                        DateTime today = DateTime.Today;
                        realTask.ActualStartTime = today.Add(currentTime);
                        
                        currentTime = currentTime.Add(realTask.Duration);
                        realTask.ActualEndTime = today.Add(currentTime);

                        // 4. Update Properties
                        realTask.AssignedTechnicianId = schedule.TechnicianId;
                        realTask.SequenceIndex = sequence++;
                        realTask.Status = FSM.Domain.Enums.TaskStatus.Scheduled;

                        currentLat = realTask.Latitude;
                        currentLon = realTask.Longitude;
                    }
                }
            }

            _taskRepo.Save(Tasks);
            Console.WriteLine("Optimization Saved.");
            return finalSolution;
        }

        private double GetDist(double lat1, double lon1, double lat2, double lon2)
        {
             var R = 6371; 
             var dLat = (lat2 - lat1) * (Math.PI / 180);
             var dLon = (lon2 - lon1) * (Math.PI / 180);
             var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                     Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                     Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
             var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
             return R * c;
        }
    }
}