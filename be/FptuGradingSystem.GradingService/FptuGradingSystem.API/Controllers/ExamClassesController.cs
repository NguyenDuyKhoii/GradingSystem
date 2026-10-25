using FptuGradingSystem.Application.Common.Models;
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
        public async Task<PaginatedList<ExamClassDto>> GetAll(
            [FromQuery] string? semester = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool isDescending = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            int? lecturerId = null;

            if (roleClaim == "Lecturer" && int.TryParse(userIdClaim, out var parsedId))
            {
                lecturerId = parsedId;
            }

            return await Mediator.Send(new GetExamClassesQuery(lecturerId, semester, searchTerm, status, sortBy, isDescending, pageNumber, pageSize));
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
        [HttpPost("{id}/publish-grades")]
        [Authorize(Roles = "AcademicStaff")]
        public async Task<IActionResult> PublishGrades(int id)
        {
            try
            {
                var success = await Mediator.Send(new PublishExamClassGradesCommand(id));
                if (!success) return NotFound("Exam class not found.");
                return Ok(new { message = "Bảng điểm cả lớp đã được công bố và hệ thống đang gửi email hàng loạt tới sinh viên." });
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/check-plagiarism")]
        [Authorize(Roles = "AcademicStaff,Lecturer")]
        public async Task<ActionResult<PlagiarismReportDto>> CheckPlagiarism(int id, [FromQuery] decimal threshold = 70.0m)
        {
            try
            {
                var result = await Mediator.Send(new CheckPlagiarismCommand(id, threshold));
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/plagiarism-report")]
        [Authorize(Roles = "AcademicStaff,Lecturer")]
        public async Task<ActionResult<PlagiarismReportDto>> GetPlagiarismReport(int id)
        {
            var result = await Mediator.Send(new GetPlagiarismReportQuery(id));
            if (result == null) return NotFound(new { message = "Chưa có báo cáo đối soát đạo code cho lớp thi này." });
            return Ok(result);
        }

        [HttpPost("{id}/reconcile-roster")]
        [Authorize(Roles = "AcademicStaff")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<RosterReconciliationReportDto>> ReconcileRoster(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn file Excel danh sách thi FAP (.xlsx)." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return BadRequest(new { message = "Định dạng file không hợp lệ. Chỉ chấp nhận file Excel (.xlsx, .xls)." });
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var report = await Mediator.Send(new ReconcileExamClassRosterCommand(id, fileBytes));
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/roster-report")]
        [Authorize(Roles = "AcademicStaff,Lecturer")]
        public async Task<ActionResult<RosterReconciliationReportDto>> GetRosterReport(int id)
        {
            var result = await Mediator.Send(new GetRosterReconciliationReportQuery(id));
            if (result == null) return NotFound(new { message = "Chưa có báo cáo đối soát danh sách thi FAP cho lớp thi này." });
            return Ok(result);
        }
    }
}