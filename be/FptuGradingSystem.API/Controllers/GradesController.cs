using FptuGradingSystem.Application.Features.Grades.Commands;
using FptuGradingSystem.Application.Features.Grades.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Controllers
{
    [Authorize]
    public class GradesController : ApiControllerBase
    {
        [HttpPost("submission")]
        [Authorize(Roles = "Lecturer")]
        public async Task<ActionResult<int>> Grade(GradeSubmissionRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var gradedById))
            {
                return Unauthorized();
            }

            var command = new GradeSubmissionCommand(
                request.SubmissionId,
                gradedById,
                request.GeneralFeedback,
                request.IsDraft,
                request.Details
            );

            return await Mediator.Send(command);
        }

        [HttpGet("submission/{submissionId}")]
        public async Task<ActionResult<SubmissionGradingDto>> GetSubmissionGrade(int submissionId)
        {
            var result = await Mediator.Send(new GetSubmissionWithGradeQuery(submissionId));
            if (result == null) return NotFound();
            return result;
        }
    }

    public record GradeSubmissionRequest(
        int SubmissionId,
        string GeneralFeedback,
        bool IsDraft,
        System.Collections.Generic.List<CreateGradeDetailDto> Details);
}
