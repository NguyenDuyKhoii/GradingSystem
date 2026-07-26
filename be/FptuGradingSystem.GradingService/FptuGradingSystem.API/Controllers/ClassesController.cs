using FptuGradingSystem.Application.Common.Models;
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
        public async Task<ActionResult<PaginatedList<ClassDto>>> GetAll(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool isDescending = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await Mediator.Send(new GetClassesQuery(searchTerm, sortBy, isDescending, pageNumber, pageSize));
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
