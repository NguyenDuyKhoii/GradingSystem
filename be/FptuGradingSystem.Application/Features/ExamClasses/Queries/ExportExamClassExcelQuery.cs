using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Queries
{
    public record ExportExamClassExcelQuery(int ExamClassId) : IRequest<ExcelExportResult?>;

    public record ExcelExportResult(byte[] FileBytes, string FileName);

    public class ExportExamClassExcelQueryHandler : IRequestHandler<ExportExamClassExcelQuery, ExcelExportResult?>
    {
        private readonly IApplicationDbContext _context;

        public ExportExamClassExcelQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExcelExportResult?> Handle(ExportExamClassExcelQuery request, CancellationToken cancellationToken)
        {
            // Load exam class info
            var examClass = await _context.ExamClasses
                .Include(ec => ec.Class)
                .Include(ec => ec.Subject)
                .Include(ec => ec.Lecturer)
                .FirstOrDefaultAsync(ec => ec.Id == request.ExamClassId, cancellationToken);

            if (examClass == null) return null;

            // Load all submissions with grades
            var submissions = await _context.Submissions
                .Where(s => s.ExamClassId == request.ExamClassId)
                .Include(s => s.Grade)
                    .ThenInclude(g => g!.GradeDetails)
                    .ThenInclude(gd => gd.RubricCriteria)
                .OrderBy(s => s.StudentId)
                .ToListAsync(cancellationToken);

            // Get rubric criteria: Subject has a Rubrics collection, load separately
            var criteria = await _context.Rubrics
                .Where(r => r.SubjectId == examClass.SubjectId)
                .Include(r => r.Criteria)
                .Select(r => r.Criteria.OrderBy(c => c.Id).ToList())
                .FirstOrDefaultAsync(cancellationToken) ?? new List<RubricCriteria>();

            // Required by EPPlus for non-commercial use
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Bảng Điểm");

            // ─── Header block ──────────────────────────────────────────
            int headerRows = 4;
            ws.Cells[1, 1].Value = "Lớp thi:";
            ws.Cells[1, 2].Value = $"{examClass.Class?.ClassCode} - {examClass.Subject?.SubjectCode} ({examClass.Semester})";
            ws.Cells[2, 1].Value = "Môn học:";
            ws.Cells[2, 2].Value = examClass.Subject?.SubjectName;
            ws.Cells[3, 1].Value = "Giảng viên:";
            ws.Cells[3, 2].Value = examClass.Lecturer?.FullName ?? "N/A";
            ws.Cells[4, 1].Value = "Xuất ngày:";
            ws.Cells[4, 2].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // Style header block
            for (int r = 1; r <= headerRows; r++)
            {
                ws.Cells[r, 1].Style.Font.Bold = true;
                ws.Cells[r, 1].Style.Font.Color.SetColor(Color.FromArgb(100, 116, 139));
            }

            // ─── Column headers ─────────────────────────────────────────
            int tableStartRow = headerRows + 2; // row 6
            int col = 1;

            ws.Cells[tableStartRow, col].Value = "MSSV";
            ws.Cells[tableStartRow, col++].Style.Font.Bold = true;
            ws.Cells[tableStartRow, col].Value = "Họ & Tên";
            ws.Cells[tableStartRow, col++].Style.Font.Bold = true;

            var criteriaStartCol = col;
            foreach (var c in criteria)
            {
                ws.Cells[tableStartRow, col].Value = $"{c.CriteriaName}\n(Max: {c.MaxPoints}, {c.Weight}%)";
                ws.Cells[tableStartRow, col].Style.Font.Bold = true;
                ws.Cells[tableStartRow, col].Style.WrapText = true;
                col++;
            }

            ws.Cells[tableStartRow, col].Value = "Tổng điểm";
            ws.Cells[tableStartRow, col++].Style.Font.Bold = true;
            ws.Cells[tableStartRow, col].Value = "Trạng thái";
            ws.Cells[tableStartRow, col++].Style.Font.Bold = true;

            int lastCol = col - 1;

            // Style header row
            using (var headerRange = ws.Cells[tableStartRow, 1, tableStartRow, lastCol])
            {
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 41, 59));
                headerRange.Style.Font.Color.SetColor(Color.White);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            // ─── Data rows ──────────────────────────────────────────────
            int dataRow = tableStartRow + 1;
            foreach (var sub in submissions)
            {
                col = 1;
                ws.Cells[dataRow, col++].Value = sub.StudentId;
                ws.Cells[dataRow, col++].Value = sub.StudentName;

                foreach (var criterion in criteria)
                {
                    var detail = sub.Grade?.GradeDetails
                        .FirstOrDefault(gd => gd.RubricCriteriaId == criterion.Id);
                    if (detail != null)
                    {
                        ws.Cells[dataRow, col].Value = (double)detail.Score;
                        ws.Cells[dataRow, col].Style.Numberformat.Format = "0.00";
                    }
                    else
                    {
                        ws.Cells[dataRow, col].Value = "-";
                    }
                    col++;
                }

                // Total score
                if (sub.Grade != null)
                {
                    ws.Cells[dataRow, col].Value = (double)sub.Grade.TotalScore;
                    ws.Cells[dataRow, col].Style.Numberformat.Format = "0.00";
                    ws.Cells[dataRow, col].Style.Font.Bold = true;

                    // Color-code total score
                    var scoreColor = sub.Grade.TotalScore >= 8
                        ? Color.FromArgb(220, 252, 231)  // green
                        : sub.Grade.TotalScore >= 5
                            ? Color.FromArgb(254, 249, 195)  // yellow
                            : Color.FromArgb(254, 226, 226);  // red
                    ws.Cells[dataRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[dataRow, col].Style.Fill.BackgroundColor.SetColor(scoreColor);
                }
                col++;

                // Status
                ws.Cells[dataRow, col].Value = sub.Status;
                col++;

                // Alternate row shading
                if (dataRow % 2 == 0)
                {
                    using var rowRange = ws.Cells[dataRow, 1, dataRow, lastCol];
                    // Only apply if cell has no background color set already
                    for (int c2 = 1; c2 <= lastCol; c2++)
                    {
                        if (ws.Cells[dataRow, c2].Style.Fill.BackgroundColor.Rgb == null ||
                            ws.Cells[dataRow, c2].Style.Fill.PatternType == ExcelFillStyle.None)
                        {
                            ws.Cells[dataRow, c2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[dataRow, c2].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 250, 252));
                        }
                    }
                }

                dataRow++;
            }

            // ─── Summary row ────────────────────────────────────────────
            ws.Cells[dataRow, 1].Value = "TỔNG KẾT";
            ws.Cells[dataRow, 1].Style.Font.Bold = true;
            ws.Cells[dataRow, 2].Value = $"{submissions.Count} sinh viên";

            var gradedList = submissions.Where(s => s.Grade != null).ToList();
            if (gradedList.Any())
            {
                var allScores = gradedList.Select(s => s.Grade!.TotalScore).ToList();
                int summaryTotalCol = criteriaStartCol + criteria.Count;

                ws.Cells[dataRow, summaryTotalCol].Value = "TB: " + Math.Round(allScores.Average(), 2);
                ws.Cells[dataRow, summaryTotalCol].Style.Font.Bold = true;
                ws.Cells[dataRow, summaryTotalCol + 1].Value = $"Đã chấm: {gradedList.Count}/{submissions.Count}";
            }

            using (var summaryRange = ws.Cells[dataRow, 1, dataRow, lastCol])
            {
                summaryRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                summaryRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(241, 245, 249));
                summaryRange.Style.Font.Bold = true;
            }

            // ─── Auto-fit & borders ─────────────────────────────────────
            ws.Cells[tableStartRow, 1, dataRow, lastCol].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            ws.Cells[tableStartRow, 1, dataRow, lastCol].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            ws.Cells[tableStartRow, 1, dataRow, lastCol].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            ws.Cells[tableStartRow, 1, dataRow, lastCol].Style.Border.Right.Style = ExcelBorderStyle.Thin;

            ws.Cells.AutoFitColumns(10, 30);
            ws.Row(tableStartRow).Height = 40;
            ws.View.FreezePanes(tableStartRow + 1, 3);

            // ─── Return bytes ────────────────────────────────────────────
            var fileBytes = package.GetAsByteArray();
            var fileName = $"BangDiem_{examClass.Class?.ClassCode}_{examClass.Subject?.SubjectCode}_{examClass.Semester}_{DateTime.Now:yyyyMMdd}.xlsx";

            return new ExcelExportResult(fileBytes, fileName);
        }
    }
}
