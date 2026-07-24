using FptuGradingSystem.Application.Features.Classes.Commands;
using FptuGradingSystem.Application.Features.Classes.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Controllers
{
    [Authorize]
    [Route("api/classes")]
    [Route("api/master-classes")]
    public class ClassesController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<ClassDto>>> GetAll()
        {
            return await Mediator.Send(new GetClassesQuery());
        }

        [HttpPost]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<ActionResult<int>> Create(CreateClassCommand command)
        {
            try
            {
                var id = await Mediator.Send(command);
                return Ok(id);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<IActionResult> Update(int id, UpdateClassCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("Route ID does not match command ID.");
            }

            try
            {
                await Mediator.Send(command);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await Mediator.Send(new DeleteClassCommand(id));
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
