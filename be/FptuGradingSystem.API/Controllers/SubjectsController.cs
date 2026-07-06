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
    }
}
