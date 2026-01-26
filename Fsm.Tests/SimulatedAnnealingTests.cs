using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSM.Application.Algorithms;
using FSM.Domain.Entities;
using FSM.Domain.Enums;
using TaskEntity = FSM.Domain.Entities.Task;

namespace Fsm.Tests
{
    public class SimulatedAnnealingTests
    {
        [Fact]
        public async System.Threading.Tasks.Task OptimizeSchedule_ShouldImproveOrMaintainScore()
        {
            // 1. ARRANGE
            var objective = new WeightedObjectiveFunction();
            var engine = new SimulatedAnnealingEngine(objective);
            
            // Create a sub-optimal initial schedule (Force a bad order or assignment)
            var tech = new Technician 
            { 
                Id = 1, 
                Name = "Tech 1",
                BaseLatitude = 0, BaseLongitude = 0,
                ShiftStart = TimeSpan.FromHours(8), 
                ShiftEnd = TimeSpan.FromHours(17),
                EstimatedTravelSpeedKmH = 60,
                Skills = SkillSet.General
            };

            var t1 = new TaskEntity { Id = 1, Latitude = 0, Longitude = 10, Duration = TimeSpan.FromMinutes(30), RequiredSkills = SkillSet.General, TimeWindowStart = DateTime.Today.AddHours(9), TimeWindowEnd = DateTime.Today.AddHours(17) };
            var t2 = new TaskEntity { Id = 2, Latitude = 0, Longitude = 0.1, Duration = TimeSpan.FromMinutes(30), RequiredSkills = SkillSet.General, TimeWindowStart = DateTime.Today.AddHours(9), TimeWindowEnd = DateTime.Today.AddHours(17) };

            // Schedule: Base -> Far (10) -> Close (0.1) 
            // Optimal would be: Base -> Close (0.1) -> Far (10)
            var initialSchedule = new List<TechnicianSchedule>
            {
                new TechnicianSchedule
                {
                    Technician = tech,
                    TechnicianId = tech.Id,
                    Tasks = new List<TaskEntity> { t1, t2 }
                }
            };

            double initialScore = objective.CalculateScore(initialSchedule);

            // 2. ACT
            var optimizedSchedule = await engine.OptimizeScheduleAsync(initialSchedule, CancellationToken.None);
            double finalScore = objective.CalculateScore(optimizedSchedule);

            // 3. ASSERT
            // Lower score is better (distance/time minimized)
            Assert.True(finalScore <= initialScore, $"Optimization failed. Initial: {initialScore}, Final: {finalScore}");
        }
    }
}   