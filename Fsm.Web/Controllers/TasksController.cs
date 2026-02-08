using Microsoft.AspNetCore.Mvc;
using FSM.Application.Services;
using FSM.Domain.Entities;
using TaskEntity = FSM.Domain.Entities.Task;

namespace Fsm.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly FsmService _service;

        public TasksController()
        {
            // This reuses the EXACT same logic you built for the console!
            _service = new FsmService();
        }

        [HttpGet]
        public IEnumerable<TaskEntity> GetAll()
        {
            return _service.Tasks;
        }

        [HttpPost]
        public IActionResult AddTask([FromBody] TaskEntity task)
        {
            // --- DEBUG LOGS START ---
            System.Console.WriteLine($"[API] Received AddTask Request...");
            
            if (task == null)
            {
                System.Console.WriteLine("[API] Error: Task object is NULL.");
                return BadRequest("Task is null");
            }

            System.Console.WriteLine($"[API] Name: {task.ClientName}, Lat: {task.Latitude}, Lon: {task.Longitude}");
            // --- DEBUG LOGS END ---

            // Defaults
            if (task.Duration == default) task.Duration = System.TimeSpan.FromHours(1);
            if (task.TimeWindowStart == default) task.TimeWindowStart = System.DateTime.Today.AddHours(9);
            if (task.TimeWindowEnd == default) task.TimeWindowEnd = System.DateTime.Today.AddHours(17);

            _service.AddTask(task);
            
            System.Console.WriteLine("[API] Task sent to service for saving.");
            return Ok(task);
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> RunSchedule()
        {
            var result = await _service.RunOptimizationAsync();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteTask(int id)
        {
            // Call the service to delete
            var success = _service.DeleteTask(id); 
            if (!success) return NotFound();
            return Ok();
        }
    }
}