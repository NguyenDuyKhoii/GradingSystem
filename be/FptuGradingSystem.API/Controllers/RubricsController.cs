using FptuGradingSystem.Application.Features.Rubrics.Commands;
using FptuGradingSystem.Application.Features.Rubrics.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Controllers
{
    [Authorize]
    public class RubricsController : ApiControllerBase
    {
        [HttpPost]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<ActionResult<int>> Create(CreateRubricCommand command)
        {
            return await Mediator.Send(command);
        }

        [HttpGet("subject/{subjectId}")]
        public async Task<ActionResult<RubricDto?>> GetBySubject(int subjectId)
        {
            var result = await Mediator.Send(new GetRubricBySubjectQuery(subjectId));
            if (result == null) return NotFound();
            return result;
        }
    }
}
