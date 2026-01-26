using Xunit; // The testing framework
using System;
using System.Collections.Generic;
using System.Linq;
using FSM.Application.Algorithms;
using FSM.Domain.Entities;
using FSM.Domain.Enums;
using TaskEntity = FSM.Domain.Entities.Task;

namespace Fsm.Tests
{
    public class GreedySchedulerTests
    {
        [Fact]
        public void GenerateInitialSchedule_ShouldAssignTask_WhenTechnicianIsAvailableAndQualified()
        {
            // 1. ARRANGE (Setup the scenario)
            var scheduler = new GreedyScheduler();
            
            var technicians = new List<Technician>
            {
                new Technician 
                { 
                    Id = 1, 
                    Name = "Test Tech", 
                    Skills = SkillSet.Electric, // Has Electric skill
                    BaseLatitude = 0, BaseLongitude = 0,
                    ShiftStart = TimeSpan.FromHours(8), 
                    ShiftEnd = TimeSpan.FromHours(17),
                    EstimatedTravelSpeedKmH = 60
                }
            };

            var tasks = new List<TaskEntity>
            {
                new TaskEntity
                {
                    Id = 100,
                    Priority = TaskPriority.High,
                    RequiredSkills = SkillSet.Electric, // Requires Electric
                    Duration = TimeSpan.FromHours(1),
                    TimeWindowStart = DateTime.Today.AddHours(9),
                    TimeWindowEnd = DateTime.Today.AddHours(12),
                    Latitude = 0.1, Longitude = 0.1 // Close by
                }
            };

            // 2. ACT (Run the function)
            var result = scheduler.GenerateInitialSchedule(technicians, tasks);

            // 3. ASSERT (Verify the result)
            // We expect 1 successful schedule
            Assert.Single(result.Schedules); 
            // We expect the technician to have 1 task
            Assert.Single(result.Schedules[0].Tasks);
            // We expect 0 unscheduled tasks
            Assert.Empty(result.UnscheduledTasks);
        }

        [Fact]
        public void GenerateInitialSchedule_ShouldFail_WhenTechnicianLacksSkill()
        {
            // 1. ARRANGE
            var scheduler = new GreedyScheduler();
            
            var technicians = new List<Technician>
            {
                new Technician 
                { 
                    Id = 1, 
                    Name = "Plumber John", 
                    Skills = SkillSet.Plumbing, // ONLY Plumbing
                    BaseLatitude = 0, BaseLongitude = 0,
                    ShiftStart = TimeSpan.FromHours(8), ShiftEnd = TimeSpan.FromHours(17),
                    EstimatedTravelSpeedKmH = 60
                }
            };

            var tasks = new List<TaskEntity>
            {
                new TaskEntity
                {
                    Id = 100,
                    RequiredSkills = SkillSet.Electric, // Requires Electric
                    Duration = TimeSpan.FromHours(1),
                    TimeWindowStart = DateTime.Today.AddHours(9),
                    TimeWindowEnd = DateTime.Today.AddHours(12),
                    Latitude = 0.1, Longitude = 0.1
                }
            };

            // 2. ACT
            var result = scheduler.GenerateInitialSchedule(technicians, tasks);

            // 3. ASSERT
            // The task should be in the "Unscheduled" list because skills didn't match
            Assert.Single(result.UnscheduledTasks);
            Assert.Empty(result.Schedules[0].Tasks);
        }
    }
}