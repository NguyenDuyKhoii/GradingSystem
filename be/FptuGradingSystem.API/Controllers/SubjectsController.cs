using FptuGradingSystem.Application.Features.Subjects.Commands;
using FptuGradingSystem.Application.Features.Subjects.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Controllers
{
    [Authorize]
    public class SubjectsController : ApiControllerBase
    {
        [HttpPost]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<ActionResult<int>> Create(CreateSubjectCommand command)
        {
            return await Mediator.Send(command);
        }

        [HttpGet]
        public async Task<ActionResult<List<SubjectDto>>> GetAll()
        {
            return await Mediator.Send(new GetSubjectsQuery());
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<IActionResult> Update(int id, UpdateSubjectCommand command)
        {
            if (id != command.Id)
                return BadRequest("Route id does not match command id.");

            await Mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<IActionResult> Delete(int id)
        {
            await Mediator.Send(new DeleteSubjectCommand(id));
            return NoContent();
        }
    }
}
