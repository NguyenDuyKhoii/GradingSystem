using FptuGradingSystem.Application.Features.Appeals.Commands;
using FptuGradingSystem.Application.Features.Appeals.Queries;
using FptuGradingSystem.Application.Features.Grades.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Controllers
{
    [Authorize]
    [Route("api/appeals")]
    public class AppealsController : ApiControllerBase
    {
        [HttpPost]
        [AllowAnonymous] // Allows students to submit appeal
        public async Task<ActionResult<GradeAppealDto>> SubmitAppeal([FromBody] SubmitAppealRequest request)
        {
            try
            {
                var command = new SubmitGradeAppealCommand(
                    request.ExamClassId,
                    request.SubmissionId,
                    request.StudentId,
                    request.StudentName,
                    request.Reason,
                    request.StudentEmail
                );

                var result = await Mediator.Send(command);
                return CreatedAtAction(nameof(GetAppealsByExamClass), new { examClassId = result.ExamClassId }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("exam-class/{examClassId}")]
        [Authorize(Roles = "AcademicStaff,Lecturer")]
        public async Task<ActionResult<List<GradeAppealDto>>> GetAppealsByExamClass(int examClassId)
        {
            var result = await Mediator.Send(new GetExamClassAppealsQuery(examClassId));
            return Ok(result);
        }

        [HttpPost("{id}/assign-reviewer")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<ActionResult<GradeAppealDto>> AssignReviewer(int id, [FromBody] AssignReviewerRequest request)
        {
            try
            {
                var result = await Mediator.Send(new AssignAppealReviewerCommand(id, request.ReviewerLecturerId));
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/blind-review")]
        [Authorize(Roles = "Lecturer,AcademicStaff")]
        public async Task<ActionResult<BlindReviewAppealDto>> GetBlindReview(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await Mediator.Send(new GetBlindReviewAppealQuery(id, currentUserId));
                if (result == null) return NotFound(new { message = "Không tìm thấy đơn phúc khảo." });
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpPost("{id}/submit-review")]
        [Authorize(Roles = "Lecturer")]
        public async Task<ActionResult<GradeAppealDto>> SubmitReview(int id, [FromBody] SubmitReviewRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await Mediator.Send(new SubmitReviewerGradeCommand(
                    id,
                    currentUserId,
                    request.ReviewerFeedback,
                    request.Details
                ));
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/resolve")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<ActionResult<GradeAppealDto>> ResolveAppeal(int id, [FromBody] ResolveAppealRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await Mediator.Send(new ResolveGradeAppealCommand(
                    id,
                    request.Decision,
                    request.FinalScore,
                    request.ResolutionNotes,
                    currentUserId
                ));
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public record SubmitAppealRequest(
        int ExamClassId,
        int SubmissionId,
        string StudentId,
        string StudentName,
        string Reason,
        string? StudentEmail);

    public record AssignReviewerRequest(int ReviewerLecturerId);

    public record SubmitReviewRequest(
        string ReviewerFeedback,
        List<CreateGradeDetailDto> Details);

    public record ResolveAppealRequest(
        string Decision,
        decimal? FinalScore,
        string ResolutionNotes);
}
