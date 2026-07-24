using FptuGradingSystem.Application.Common.Models;
using FptuGradingSystem.Application.Features.Submissions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Controllers
{
    [Authorize]
    public class SubmissionsController : ApiControllerBase
    {
        [HttpGet("class/{classId}")]
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
    }
}
