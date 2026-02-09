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

        // FIX: We ask for the SHARED service here.
        // The "Singleton" we added in Program.cs is injected automatically.
        public TasksController(FsmService service)
        {
            _service = service;
        }

        [HttpGet]
        public IEnumerable<TaskEntity> GetAll()
        {
            return _service.Tasks;
        }

        [HttpPost]
        public IActionResult AddTask([FromBody] TaskEntity task)
        {
            if (task == null) return BadRequest("Task is null");

            // Defaults: 1 Hour duration, window 09:00 - 17:00
            if (task.Duration == default) task.Duration = System.TimeSpan.FromHours(1);
            if (task.TimeWindowStart == default) task.TimeWindowStart = System.DateTime.Today.AddHours(9);
            if (task.TimeWindowEnd == default) task.TimeWindowEnd = System.DateTime.Today.AddHours(17);

            _service.AddTask(task);
            return Ok(task);
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> RunSchedule()
        {
            // DEBUG: Print count to console to prove it sees the data
            System.Console.WriteLine($"[Optimizer] Technicians: {_service.Technicians.Count}, Tasks: {_service.Tasks.Count}");
            
            var result = await _service.RunOptimizationAsync();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteTask(int id)
        {
            var success = _service.DeleteTask(id); 
            if (!success) return NotFound();
            return Ok();
        }
    }
}