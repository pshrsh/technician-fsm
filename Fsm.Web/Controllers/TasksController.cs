using Microsoft.AspNetCore.Mvc;
using FSM.Application.Services;
using FSM.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks; // For async Task
using TaskEntity = FSM.Domain.Entities.Task; // Alias to avoid confusion

namespace FSM.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly FsmService _service;

        public TasksController(FsmService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<TaskEntity>> Get()
        {
            // The frontend expects a JSON list of tasks
            // The JSON serializer handles TimeSpan? (e.g., "09:00:00") automatically
            return Ok(_service.GetAllTasks());
        }

        [HttpPost]
        public ActionResult<TaskEntity> Post([FromBody] TaskEntity task)
        {
            // Validation: Ensure windows make sense if both provided
            if (task.WindowStart.HasValue && task.WindowEnd.HasValue)
            {
                if (task.WindowStart > task.WindowEnd)
                {
                    return BadRequest("Window Start must be before Window End.");
                }
            }

            _service.AddTask(task);
            return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var success = _service.DeleteTask(id);
            if (!success) return NotFound();
            return Ok();
        }

        // Endpoint to trigger the Optimizer
        [HttpPost("schedule")]
        public async Task<IActionResult> Schedule()
        {
            await _service.RunOptimizationAsync();
            return Ok(new { message = "Optimization complete" });
        }
    }
}