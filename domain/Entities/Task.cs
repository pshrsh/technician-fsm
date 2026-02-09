using System;
using System.Text.Json.Serialization;
using FSM.Domain.Enums;

namespace FSM.Domain.Entities
{
    public class Task
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = string.Empty;
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; } 

        public TaskPriority Priority { get; set; } 
        public SkillSet RequiredSkills { get; set; }
        public FSM.Domain.Enums.TaskStatus Status { get; set; } 

        public TimeSpan Duration { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan? WindowStart { get; set; } 
        public TimeSpan? WindowEnd { get; set; }
        // Algorithm Fields
        public int SequenceIndex { get; set; }
        
        // These are calculated AFTER optimization
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }

        public int? AssignedTechnicianId { get; set; }
        
        [JsonIgnore]
        public Technician? AssignedTechnician { get; set; }
    }
}