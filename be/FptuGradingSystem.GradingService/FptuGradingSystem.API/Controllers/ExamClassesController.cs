using FptuGradingSystem.Application.Features.ExamClasses.Commands;
using FptuGradingSystem.Application.Features.ExamClasses.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Controllers
{
    [Authorize]
    [Route("api/exam-classes")]
    public class ExamClassesController : ApiControllerBase
    {
        [HttpGet]
        public async Task<List<ExamClassDto>> GetAll(
            [FromQuery] string? semester = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool isDescending = false)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            int? lecturerId = null;

            if (roleClaim == "Lecturer" && int.TryParse(userIdClaim, out var parsedId))
            {
                lecturerId = parsedId;
            }

            return await Mediator.Send(new GetExamClassesQuery(lecturerId, semester, searchTerm, status, sortBy, isDescending));
        }

        [HttpPost]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<ActionResult<int>> Create(CreateExamClassCommand command)
        {
            var id = await Mediator.Send(command);
            return Ok(id);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<IActionResult> Update(int id, UpdateExamClassCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("Route id does not match command id.");
            }

            await Mediator.Send(command);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<IActionResult> Delete(int id)
        {
            await Mediator.Send(new DeleteExamClassCommand(id));

            return NoContent();
        }

        [HttpPost("{id}/upload-zip")]
        [Authorize(Roles = "AcademicStaff")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadZip(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    status = "Lỗi giải nén",
                    message = "Please upload a ZIP file."
                });
            }

            var extension = Path.GetExtension(file.FileName);

            if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    status = "Lỗi giải nén",
                    message = "Only .zip files are allowed."
                });
            }

            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

            var uploadRoot = Path.Combine(
                env.ContentRootPath,
                "uploads",
                "exam-classes",
                id.ToString()
            );

            Directory.CreateDirectory(uploadRoot);

            var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var zipFilePath = Path.Combine(uploadRoot, safeFileName);

            await using (var stream = new FileStream(zipFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var extractFolderPath = Path.Combine(
                uploadRoot,
                Path.GetFileNameWithoutExtension(safeFileName)
            );

            try
            {
                var result = await Mediator.Send(
                    new UploadExamClassZipCommand(id, zipFilePath, extractFolderPath)
                );

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    status = "Lỗi giải nén",
                    message = ex.Message
                });
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new
                {
                    status = "Lỗi giải nén",
                    message = ex.Message
                });
            }
        }

        [HttpGet("{id}/analytics")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<ActionResult<ExamClassAnalyticsDto>> GetAnalytics(int id)
        {
            var result = await Mediator.Send(new GetExamClassAnalyticsQuery(id));
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("{id}/export-excel")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<IActionResult> ExportExcel(int id)
        {
            var result = await Mediator.Send(new ExportExamClassExcelQuery(id));
            if (result == null) return NotFound();
            return File(result.FileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName);
        }
    }
}