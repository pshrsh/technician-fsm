using System;
using System.Collections.Generic;
using FSM.Domain.Enums;

namespace FSM.Domain.Entities
{
    public class Technician
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string BaseAddress { get; set; }
        public double BaseLatitude { get; set; } 
        public double BaseLongitude { get; set; } 

        public SkillSet Skills { get; set; } 

        public TimeSpan ShiftStart { get; set; } 
        public TimeSpan ShiftEnd { get; set; } 
        
        public int MaxConcurrentTasks { get; set; } = 1; 
        public double EstimatedTravelSpeedKmH { get; set; } 
        
        public decimal HourlyCost { get; set; } 

        public virtual ICollection<TechnicianSchedule> Schedules { get; set; } = new List<TechnicianSchedule>();

        // --- NEW CONSTRUCTOR (Fixes the defaults) ---
        public Technician()
        {
            // Default: Works 08:00 to 18:00
            ShiftStart = new TimeSpan(8, 0, 0);
            ShiftEnd = new TimeSpan(18, 0, 0);
            
            // Default: Can do ALL types of tasks
            Skills = SkillSet.All; 
            
            EstimatedTravelSpeedKmH = 60;
        }
        // --------------------------------------------

        public bool HasSkill(SkillSet requiredSkill)
        {
            return (Skills & requiredSkill) == requiredSkill;
        }

        public TimeSpan GetShiftDuration()
        {
            return ShiftEnd - ShiftStart;
        }
    }
}