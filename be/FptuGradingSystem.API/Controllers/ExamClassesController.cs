using FptuGradingSystem.Application.Features.ExamClasses.Commands;
using FptuGradingSystem.Application.Features.ExamClasses.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Controllers
{
    [Authorize]
    public class ExamClassesController : ApiControllerBase
    {
        [HttpPost]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<ActionResult<int>> Create(CreateExamClassCommand command)
        {
            return await Mediator.Send(command);
        }

        [HttpGet]
        public async Task<ActionResult<List<ExamClassDto>>> GetAll([FromQuery] string? semester = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            int? lecturerId = null;
            if (roleClaim == "Lecturer" && int.TryParse(userIdClaim, out var parsedId))
            {
                lecturerId = parsedId;
            }

            return await Mediator.Send(new GetExamClassesQuery(lecturerId, semester));
        }
    }
}
