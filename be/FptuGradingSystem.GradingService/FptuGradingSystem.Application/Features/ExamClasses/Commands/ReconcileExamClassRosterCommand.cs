using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Commands
{
    public record ReconcileExamClassRosterCommand(
        int ExamClassId,
        byte[] ExcelFileBytes) : IRequest<RosterReconciliationReportDto>;

    public record RosterReconciliationReportDto(
        int Id,
        int ExamClassId,
        DateTime ReconciledAt,
        int TotalInRoster,
        int TotalSubmissions,
        int MatchedCount,
        int AbsentCount,
        int UnrecognizedCount,
        List<RosterReconciliationItemDto> Items);

    public record RosterReconciliationItemDto(
        int Id,
        string StudentId,
        string StudentName,
        string Email,
        string Status,
        int? SubmissionId,
        string Notes);

    public class ReconcileExamClassRosterCommandHandler
        : IRequestHandler<ReconcileExamClassRosterCommand, RosterReconciliationReportDto>
    {
        private readonly IApplicationDbContext _context;

        public ReconcileExamClassRosterCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RosterReconciliationReportDto> Handle(
            ReconcileExamClassRosterCommand request,
            CancellationToken cancellationToken)
        {
            var examClass = await _context.ExamClasses
                .Include(ec => ec.Submissions)
                .FirstOrDefaultAsync(ec => ec.Id == request.ExamClassId, cancellationToken);

            if (examClass == null)
            {
                throw new KeyNotFoundException($"Exam class with ID {request.ExamClassId} not found.");
            }

            // 1. Parse Excel Roster using EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var rosterStudents = new List<RosterEntry>();

            using (var stream = new MemoryStream(request.ExcelFileBytes))
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null || worksheet.Dimension == null)
                {
                    throw new InvalidOperationException("File Excel rỗng hoặc không đúng định dạng bảng tính.");
                }

                int totalRows = worksheet.Dimension.Rows;
                int totalCols = worksheet.Dimension.Columns;

                // Find header columns
                int idCol = -1;
                int nameCol = -1;
                int emailCol = -1;

                for (int col = 1; col <= totalCols; col++)
                {
                    var header = worksheet.Cells[1, col].Text?.Trim().ToLowerInvariant() ?? string.Empty;
                    if (header.Contains("student") || header.Contains("mssv") || header.Contains("roll") || header.Contains("mã sinh viên") || header.Contains("id"))
                    {
                        if (idCol == -1) idCol = col;
                    }
                    else if (header.Contains("name") || header.Contains("tên") || header.Contains("họ và tên"))
                    {
                        if (nameCol == -1) nameCol = col;
                    }
                    else if (header.Contains("email") || header.Contains("mail"))
                    {
                        if (emailCol == -1) emailCol = col;
                    }
                }

                // Default fallbacks if header not found
                if (idCol == -1) idCol = 1;
                if (nameCol == -1) nameCol = 2;
                if (emailCol == -1) emailCol = 3;

                for (int row = 2; row <= totalRows; row++)
                {
                    var studentId = worksheet.Cells[row, idCol].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(studentId)) continue;

                    var studentName = nameCol <= totalCols ? worksheet.Cells[row, nameCol].Text?.Trim() : string.Empty;
                    var email = emailCol <= totalCols ? worksheet.Cells[row, emailCol].Text?.Trim() : string.Empty;

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        email = FptuGradingSystem.Domain.Helpers.FptuEmailHelper.GenerateFptuEmail(studentName ?? string.Empty, studentId);
                    }

                    rosterStudents.Add(new RosterEntry(studentId, studentName ?? string.Empty, email));
                }
            }

            if (rosterStudents.Count == 0)
            {
                throw new InvalidOperationException("Không tìm thấy danh sách sinh viên hợp lệ trong file Excel.");
            }

            // 2. Perform Reconciliation with existing Submissions
            var existingSubmissions = examClass.Submissions.ToList();
            var matchedSubmissionIds = new HashSet<int>();
            var reportItems = new List<RosterReconciliationItem>();

            int matchedCount = 0;
            int absentCount = 0;

            foreach (var student in rosterStudents)
            {
                var sub = existingSubmissions.FirstOrDefault(s =>
                    string.Equals(s.StudentId.Trim(), student.StudentId, StringComparison.OrdinalIgnoreCase));

                if (sub != null)
                {
                    // Matched!
                    matchedCount++;
                    matchedSubmissionIds.Add(sub.Id);

                    if (!string.IsNullOrWhiteSpace(student.StudentName))
                    {
                        sub.StudentName = student.StudentName;
                    }
                    if (!string.IsNullOrWhiteSpace(student.Email))
                    {
                        sub.Email = student.Email;
                    }

                    reportItems.Add(new RosterReconciliationItem
                    {
                        StudentId = student.StudentId,
                        StudentName = student.StudentName,
                        Email = student.Email,
                        Status = "Matched",
                        SubmissionId = sub.Id,
                        Notes = "Khớp hợp lệ với bài nộp thực tế."
                    });
                }
                else
                {
                    // Absent student!
                    absentCount++;

                    var absentSubmission = new Submission
                    {
                        ExamClassId = examClass.Id,
                        StudentId = student.StudentId,
                        StudentName = student.StudentName,
                        Email = student.Email,
                        FilePath = string.Empty,
                        FileType = "none",
                        Status = "Absent"
                    };

                    _context.Submissions.Add(absentSubmission);

                    reportItems.Add(new RosterReconciliationItem
                    {
                        StudentId = student.StudentId,
                        StudentName = student.StudentName,
                        Email = student.Email,
                        Status = "Absent",
                        Notes = "Có tên trong danh sách thi chính thức nhưng không nộp bài (Vắng thi)."
                    });
                }
            }

            // 3. Detect Unrecognized Submissions (Files submitted not in roster)
            int unrecognizedCount = 0;
            foreach (var sub in existingSubmissions)
            {
                if (!matchedSubmissionIds.Contains(sub.Id))
                {
                    unrecognizedCount++;
                    sub.Status = "Unrecognized";

                    reportItems.Add(new RosterReconciliationItem
                    {
                        StudentId = sub.StudentId,
                        StudentName = sub.StudentName,
                        Email = sub.Email,
                        Status = "Unrecognized",
                        SubmissionId = sub.Id,
                        Notes = "Bài nộp không có tên trong danh sách phòng thi FAP chính thức."
                    });
                }
            }

            // 4. Save Reconciliation Report
            var report = new RosterReconciliationReport
            {
                ExamClassId = examClass.Id,
                ReconciledAt = DateTime.UtcNow,
                TotalInRoster = rosterStudents.Count,
                TotalSubmissions = existingSubmissions.Count,
                MatchedCount = matchedCount,
                AbsentCount = absentCount,
                UnrecognizedCount = unrecognizedCount,
                Items = reportItems
            };

            _context.RosterReconciliationReports.Add(report);
            await _context.SaveChangesAsync(cancellationToken);

            return new RosterReconciliationReportDto(
                report.Id,
                report.ExamClassId,
                report.ReconciledAt,
                report.TotalInRoster,
                report.TotalSubmissions,
                report.MatchedCount,
                report.AbsentCount,
                report.UnrecognizedCount,
                report.Items.Select(i => new RosterReconciliationItemDto(
                    i.Id,
                    i.StudentId,
                    i.StudentName,
                    i.Email,
                    i.Status,
                    i.SubmissionId,
                    i.Notes
                )).ToList()
            );
        }

        private record RosterEntry(string StudentId, string StudentName, string Email);
    }
}
