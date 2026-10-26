using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Application.Features.Grades.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Queries
{
    public record ExportExamClassOfficialReportPdfQuery(int ExamClassId) : IRequest<PdfExportResultDto?>;

    public class ExportExamClassOfficialReportPdfQueryHandler
        : IRequestHandler<ExportExamClassOfficialReportPdfQuery, PdfExportResultDto?>
    {
        private readonly IApplicationDbContext _context;

        static ExportExamClassOfficialReportPdfQueryHandler()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public ExportExamClassOfficialReportPdfQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PdfExportResultDto?> Handle(
            ExportExamClassOfficialReportPdfQuery request,
            CancellationToken cancellationToken)
        {
            var examClass = await _context.ExamClasses
                .AsNoTracking()
                .Include(ec => ec.Class)
                .Include(ec => ec.Subject)
                .Include(ec => ec.Lecturer)
                .Include(ec => ec.Submissions)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(ec => ec.Id == request.ExamClassId, cancellationToken);

            if (examClass == null) return null;

            var classCode = examClass.Class?.ClassCode ?? "EXAM_CLASS";
            var subjectCode = examClass.Subject?.SubjectCode ?? "SUBJ";
            var subjectName = examClass.Subject?.SubjectName ?? "Practical Exam";
            var semester = examClass.Semester ?? "N/A";
            var lecturerName = examClass.Lecturer?.FullName ?? "Giảng viên phụ trách";

            var submissions = examClass.Submissions
                .OrderBy(s => s.StudentId)
                .ToList();

            var totalStudents = submissions.Count;
            var gradedSubmissions = submissions.Where(s => s.Grade != null).ToList();
            var passCount = gradedSubmissions.Count(s => s.Grade!.TotalScore >= 5.0m);
            var failCount = gradedSubmissions.Count(s => s.Grade!.TotalScore < 5.0m);
            var absentCount = submissions.Count(s => s.Status == "Absent" || s.Grade == null);
            var avgScore = gradedSubmissions.Any() ? gradedSubmissions.Average(s => s.Grade!.TotalScore) : 0m;

            // Integrity Fingerprint & QR Code
            var rawDataToHash = $"{examClass.Id}|{classCode}|{subjectCode}|{totalStudents}|{avgScore:F2}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawDataToHash));
            var verificationHash = Convert.ToHexString(hashBytes);
            var shortHash = verificationHash[..16];

            var qrPayload = $"https://fptu-grading.edu.vn/verify-class?id={examClass.Id}&class={classCode}&hash={shortHash}";
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrPayload, QRCodeGenerator.ECCLevel.M);
            using var pngQrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = pngQrCode.GetGraphic(10);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(headerCol =>
                            {
                                headerCol.Item().Text("TRƯỜNG ĐẠI HỌC FPT - HỆ THỐNG CHẤM THI PE")
                                    .Bold().FontSize(12).FontColor("#F27024");
                                headerCol.Item().Text("BẢNG TỔNG HỢP ĐIỂM THI THỰC HÀNH CHÍNH THỨC")
                                    .Bold().FontSize(15).FontColor(Colors.Blue.Darken3);
                                headerCol.Item().Text("OFFICIAL CLASS GRADE REPORT")
                                    .Italic().FontSize(9).FontColor(Colors.Grey.Medium);
                            });

                            row.ConstantItem(65).Height(65).Image(qrCodeBytes);
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor("#F27024");

                        // Class Meta Info
                        col.Item().PaddingTop(8).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Lớp thi: ").Bold(); t.Span(classCode); });
                                c.Item().Text(t => { t.Span("Môn thi: ").Bold(); t.Span($"{subjectCode} - {subjectName}"); });
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Học kỳ: ").Bold(); t.Span(semester); });
                                c.Item().Text(t => { t.Span("Giảng viên: ").Bold(); t.Span(lecturerName); });
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Tổng số SV: ").Bold(); t.Span(totalStudents.ToString()); });
                                c.Item().Text(t => { t.Span("Điểm trung bình: ").Bold(); t.Span($"{avgScore:F2} / 10"); });
                            });
                        });
                    });

                    // Body: Submissions Table
                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);  // STT
                                columns.ConstantColumn(80);  // MSSV
                                columns.RelativeColumn(3);   // Họ và tên
                                columns.ConstantColumn(60);  // Điểm
                                columns.ConstantColumn(50);  // Chữ
                                columns.ConstantColumn(80);  // Kết quả
                                columns.RelativeColumn(2);   // Ghi chú
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#F27024").Padding(4).AlignCenter().Text("#").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(4).Text("MSSV").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(4).Text("Họ và tên").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(4).AlignCenter().Text("Điểm").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(4).AlignCenter().Text("Chữ").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(4).AlignCenter().Text("Kết quả").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(4).Text("Ghi chú").Bold().FontColor(Colors.White);
                            });

                            int stt = 1;
                            foreach (var s in submissions)
                            {
                                var bgColor = (stt % 2 == 0) ? Colors.Grey.Lighten4 : Colors.White;
                                var hasGrade = s.Grade != null;
                                var score = hasGrade ? s.Grade!.TotalScore : 0m;
                                var isPass = hasGrade && score >= 5.0m;
                                var isAbsent = s.Status == "Absent";

                                table.Cell().Background(bgColor).Padding(4).AlignCenter().Text(stt.ToString());
                                table.Cell().Background(bgColor).Padding(4).Text(s.StudentId);
                                table.Cell().Background(bgColor).Padding(4).Text(s.StudentName);

                                if (isAbsent)
                                {
                                    table.Cell().Background(bgColor).Padding(4).AlignCenter().Text("0.00").FontColor(Colors.Red.Medium);
                                    table.Cell().Background(bgColor).Padding(4).AlignCenter().Text("F").FontColor(Colors.Red.Medium);
                                    table.Cell().Background(bgColor).Padding(4).AlignCenter().Text("VẮNG THI").Bold().FontColor(Colors.Red.Darken2);
                                    table.Cell().Background(bgColor).Padding(4).Text("Vắng thi (Absent)");
                                }
                                else if (!hasGrade)
                                {
                                    table.Cell().Background(bgColor).Padding(4).AlignCenter().Text("-");
                                    table.Cell().Background(bgColor).Padding(4).AlignCenter().Text("-");
                                    table.Cell().Background(bgColor).Padding(4).AlignCenter().Text("CHƯA CHẤM").Italic();
                                    table.Cell().Background(bgColor).Padding(4).Text("-");
                                }
                                else
                                {
                                    table.Cell().Background(bgColor).Padding(4).AlignCenter().Text(score.ToString("F2")).Bold();
                                    table.Cell().Background(bgColor).Padding(4).AlignCenter().Text(CalculateLetterGrade(score)).FontColor(Colors.Blue.Darken2);
                                    table.Cell().Background(bgColor).Padding(4).AlignCenter().Text(isPass ? "ĐẠT" : "HỎNG").Bold().FontColor(isPass ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                    table.Cell().Background(bgColor).Padding(4).Text(s.Grade!.GeneralFeedback.Length > 25 ? s.Grade.GeneralFeedback[..25] + "..." : s.Grade.GeneralFeedback);
                                }

                                stt++;
                            }
                        });

                        // Summary Statistics Box
                        col.Item().PaddingTop(15).Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Row(r =>
                        {
                            r.RelativeItem().Text(t =>
                            {
                                t.Span("Thống kê: ").Bold();
                                t.Span($"Đạt: {passCount} SV ({(totalStudents > 0 ? (passCount * 100.0 / totalStudents) : 0):F1}%) | ");
                                t.Span($"Hỏng: {failCount} SV | ");
                                t.Span($"Vắng thi: {absentCount} SV | ");
                                t.Span($"ĐTB: {avgScore:F2}");
                            });
                        });

                        // Signatures
                        col.Item().PaddingTop(25).Row(row =>
                        {
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("GIẢNG VIÊN CHẤM THI").Bold();
                                c.Item().Text("(Ký và ghi rõ họ tên)").FontSize(8).Italic();
                                c.Item().PaddingTop(35).Text(lecturerName).Bold();
                            });

                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("TRƯỞNG BỘ MÔN").Bold();
                                c.Item().Text("(Duyệt bảng điểm)").FontSize(8).Italic();
                                c.Item().PaddingTop(35).Text("ĐÃ DUYỆT").Bold().FontColor(Colors.Green.Darken2);
                            });

                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("PHÒNG KHẢO THÍ").Bold();
                                c.Item().Text("(Chốt kết quả)").FontSize(8).Italic();
                                c.Item().PaddingTop(35).Text("HOÀN TẤT").Bold().FontColor(Colors.Blue.Darken2);
                            });
                        });
                    });

                    // Footer
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Mã xác thực: ").FontSize(7).FontColor(Colors.Grey.Medium);
                            t.Span(verificationHash).FontSize(7).FontFamily(Fonts.Consolas).FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Trang ").FontSize(7).FontColor(Colors.Grey.Medium);
                            t.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                            t.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                            t.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var fileName = $"BangDiemLop_{classCode}_{subjectCode}.pdf";

            return new PdfExportResultDto(pdfBytes, fileName);
        }

        private static string CalculateLetterGrade(decimal score) => score switch
        {
            >= 9.0m => "A+",
            >= 8.5m => "A",
            >= 8.0m => "B+",
            >= 7.0m => "B",
            >= 6.5m => "C+",
            >= 5.5m => "C",
            >= 5.0m => "D",
            _ => "F"
        };
    }
}
