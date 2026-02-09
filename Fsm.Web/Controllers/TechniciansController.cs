using FSM.Domain.Entities;
using FSM.Application.Services; // Note: Namespace matches your Service
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Fsm.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TechniciansController : ControllerBase
    {
        private readonly FsmService _service;

        public TechniciansController(FsmService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_service.GetAllTechnicians());
        }

        [HttpPost]
        public IActionResult Add([FromBody] Technician tech)
        {
            if (tech == null) return BadRequest();

            // Set some defaults if missing
            if (tech.EstimatedTravelSpeedKmH == 0) tech.EstimatedTravelSpeedKmH = 60;
            
            var newTech = _service.AddTechnician(tech);
            return Ok(newTech);
        }
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var success = _service.DeleteTechnician(id);
            if (!success) return NotFound();
            return Ok();
        }
    }
}