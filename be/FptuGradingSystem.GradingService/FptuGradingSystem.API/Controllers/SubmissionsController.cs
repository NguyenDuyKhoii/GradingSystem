using FptuGradingSystem.Application.Common.Models;
using FptuGradingSystem.Application.Features.Submissions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Controllers
{
    [Authorize]
    [Route("api/submissions")]
    public class SubmissionsController : ApiControllerBase
    {
        [HttpGet("class/{classId}")]
        [HttpGet("/api/exam-classes/{classId}/submissions")]
        public async Task<ActionResult<PaginatedList<SubmissionDto>>> GetByClass(
            int classId,
            [FromQuery] string? searchQuery = null,
            [FromQuery] string? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await Mediator.Send(new GetSubmissionsQuery(
                classId,
                searchQuery,
                status,
                pageNumber,
                pageSize));
        }

        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetSubmissionContent(
            int id, 
            [FromServices] FptuGradingSystem.GrpcService.DocumentReader.DocumentReaderClient grpcClient)
        {
            var submission = await Mediator.Send(new FptuGradingSystem.Application.Features.Grades.Queries.GetSubmissionWithGradeQuery(id));
            if (submission == null)
            {
                return NotFound(new { message = "Submission not found." });
            }

            try
            {
                var response = await grpcClient.ReadDocumentAsync(new FptuGradingSystem.GrpcService.ReadDocumentRequest
                {
                    FilePath = submission.FilePath,
                    FileType = submission.FileType
                });

                if (!response.Success)
                {
                    return BadRequest(new { message = response.ErrorMessage });
                }

                return Ok(new { content = response.Content });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new 
                { 
                    message = "Error communicating with gRPC Document Reader service.", 
                    detail = ex.Message 
                });
            }
        }
    }
}
