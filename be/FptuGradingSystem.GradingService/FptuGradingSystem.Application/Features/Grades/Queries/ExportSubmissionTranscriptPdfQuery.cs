using FptuGradingSystem.Application.Common.Interfaces;
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

namespace FptuGradingSystem.Application.Features.Grades.Queries
{
    public record ExportSubmissionTranscriptPdfQuery(int SubmissionId) : IRequest<PdfExportResultDto?>;

    public record PdfExportResultDto(byte[] FileBytes, string FileName);

    public class ExportSubmissionTranscriptPdfQueryHandler
        : IRequestHandler<ExportSubmissionTranscriptPdfQuery, PdfExportResultDto?>
    {
        private readonly IApplicationDbContext _context;

        static ExportSubmissionTranscriptPdfQueryHandler()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public ExportSubmissionTranscriptPdfQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PdfExportResultDto?> Handle(
            ExportSubmissionTranscriptPdfQuery request,
            CancellationToken cancellationToken)
        {
            var submission = await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Grade)
                    .ThenInclude(g => g!.GradeDetails)
                        .ThenInclude(gd => gd.RubricCriteria)
                .Include(s => s.Grade)
                    .ThenInclude(g => g!.GradedBy)
                .Include(s => s.ExamClass)
                    .ThenInclude(ec => ec!.Class)
                .Include(s => s.ExamClass)
                    .ThenInclude(ec => ec!.Subject)
                .Include(s => s.ExamClass)
                    .ThenInclude(ec => ec!.Lecturer)
                .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, cancellationToken);

            if (submission == null || submission.Grade == null) return null;

            var examClass = submission.ExamClass;
            var grade = submission.Grade;
            var classCode = examClass?.Class?.ClassCode ?? "EXAM_CLASS";
            var subjectCode = examClass?.Subject?.SubjectCode ?? "SUBJ";
            var subjectName = examClass?.Subject?.SubjectName ?? "Practical Exam";
            var semester = examClass?.Semester ?? "N/A";
            var graderName = grade.GradedBy?.FullName ?? examClass?.Lecturer?.FullName ?? "Giảng viên chấm thi";

            // 1. Calculate SHA-256 Digital Verification Fingerprint
            var rawDataToHash = $"{submission.StudentId}|{classCode}|{subjectCode}|{grade.TotalScore:F2}|{grade.GradedAt:yyyyMMddHHmmss}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawDataToHash));
            var verificationHash = Convert.ToHexString(hashBytes);
            var shortHash = verificationHash[..16];

            // 2. Generate QR Code bytes using QRCoder (PngByteQRCode is 100% cross-platform)
            var qrPayload = $"https://fptu-grading.edu.vn/verify?sub={submission.Id}&sid={submission.StudentId}&score={grade.TotalScore}&hash={shortHash}";
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrPayload, QRCodeGenerator.ECCLevel.M);
            using var pngQrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = pngQrCode.GetGraphic(10);

            var letterGrade = CalculateLetterGrade(grade.TotalScore);
            var isPassed = grade.TotalScore >= 5.0m;

            // 3. Render PDF document using QuestPDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(headerCol =>
                            {
                                headerCol.Item().Text("TRƯỜNG ĐẠI HỌC FPT")
                                    .Bold().FontSize(13).FontColor("#F27024"); // FPT Orange
                                headerCol.Item().Text("PHÒNG KHẢO THÍ & ĐẢM BẢO CHẤT LƯỢNG")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                                headerCol.Item().Text("HỆ THỐNG CHẤM THI THỰC HÀNH (PE GRADING)")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(75).Height(75).Image(qrCodeBytes);
                        });

                        col.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor("#F27024");

                        col.Item().PaddingTop(10).PaddingBottom(10).AlignCenter().Column(titleCol =>
                        {
                            titleCol.Item().Text("PHIẾU BÁO ĐIỂM THI THỰC HÀNH CHÍNH THỨC")
                                .Bold().FontSize(16).FontColor(Colors.Blue.Darken3);
                            titleCol.Item().Text("OFFICIAL PRACTICAL EXAM TRANSCRIPT")
                                .Italic().FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                    });

                    // Body
                    page.Content().Column(col =>
                    {
                        // Student Info Box
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Mã số sinh viên: ").Bold(); t.Span(submission.StudentId); });
                                c.Item().Text(t => { t.Span("Họ và tên: ").Bold(); t.Span(submission.StudentName); });
                                c.Item().Text(t => { t.Span("Email: ").Bold(); t.Span(submission.Email); });
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Lớp thi: ").Bold(); t.Span(classCode); });
                                c.Item().Text(t => { t.Span("Môn thi: ").Bold(); t.Span($"{subjectCode} - {subjectName}"); });
                                c.Item().Text(t => { t.Span("Học kỳ: ").Bold(); t.Span(semester); });
                            });
                        });

                        col.Item().PaddingTop(15).Text("CHI TIẾT KẾT QUẢ CHẤM THEO BAREM (RUBRIC)")
                            .Bold().FontSize(11).FontColor(Colors.Blue.Darken2);

                        // Rubric Details Table
                        col.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);  // STT
                                columns.RelativeColumn(3);   // Tiêu chí
                                columns.ConstantColumn(50);  // Trọng số
                                columns.ConstantColumn(60);  // Điểm tối đa
                                columns.ConstantColumn(60);  // Điểm đạt
                                columns.RelativeColumn(3);   // Nhận xét tiêu chí
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#F27024").Padding(5).AlignCenter().Text("#").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(5).Text("Tiêu chí đánh giá").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(5).AlignCenter().Text("Tỷ trọng").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(5).AlignCenter().Text("Tối đa").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(5).AlignCenter().Text("Điểm").Bold().FontColor(Colors.White);
                                header.Cell().Background("#F27024").Padding(5).Text("Ghi chú / Nhận xét").Bold().FontColor(Colors.White);
                            });

                            int index = 1;
                            foreach (var detail in grade.GradeDetails)
                            {
                                var bgColor = (index % 2 == 0) ? Colors.Grey.Lighten4 : Colors.White;
                                var crit = detail.RubricCriteria;

                                table.Cell().Background(bgColor).Padding(5).AlignCenter().Text(index.ToString());
                                table.Cell().Background(bgColor).Padding(5).Text(crit?.CriteriaName ?? $"Tiêu chí {index}");
                                table.Cell().Background(bgColor).Padding(5).AlignCenter().Text($"{crit?.Weight ?? 0}%");
                                table.Cell().Background(bgColor).Padding(5).AlignCenter().Text($"{crit?.MaxPoints ?? 0:F1}");
                                table.Cell().Background(bgColor).Padding(5).AlignCenter().Text(detail.Score.ToString("F2")).Bold();
                                table.Cell().Background(bgColor).Padding(5).Text(string.IsNullOrWhiteSpace(detail.Feedback) ? "-" : detail.Feedback);

                                index++;
                            }
                        });

                        // Score Summary Box
                        col.Item().PaddingTop(15).Border(1.5f).BorderColor(isPassed ? Colors.Green.Medium : Colors.Red.Medium).Padding(12).Row(row =>
                        {
                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text(t =>
                                {
                                    t.Span("TỔNG ĐIỂM CHÍNH THỨC: ").Bold().FontSize(13);
                                    t.Span($"{grade.TotalScore:F2} / 10.0").Bold().FontSize(15).FontColor(isPassed ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                });

                                c.Item().Text(t =>
                                {
                                    t.Span("Xếp loại chữ: ").Bold();
                                    t.Span(letterGrade).Bold().FontColor(Colors.Blue.Darken2);
                                    t.Span(" | Kết quả: ").Bold();
                                    t.Span(isPassed ? "ĐẠT (PASSED)" : "KHÔNG ĐẠT (FAILED)").Bold().FontColor(isPassed ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                });

                                if (!string.IsNullOrWhiteSpace(grade.GeneralFeedback))
                                {
                                    c.Item().PaddingTop(4).Text(t =>
                                    {
                                        t.Span("Nhận xét chung: ").Bold();
                                        t.Span(grade.GeneralFeedback);
                                    });
                                }
                            });

                            row.RelativeItem(1).AlignCenter().Column(c =>
                            {
                                c.Item().Text(isPassed ? "PASSED" : "FAILED")
                                    .Bold().FontSize(22).FontColor(isPassed ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                c.Item().Text($"Chấm ngày: {grade.GradedAt:dd/MM/yyyy}").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });

                        // Digital Signature & Integrity Verification Section
                        col.Item().PaddingTop(12).Background(Colors.Grey.Lighten3).Padding(8).Column(secCol =>
                        {
                            secCol.Item().Text("XÁC THỰC TÍNH TOÀN VẸN CHỨNG TỪ (DIGITAL INTEGRITY VERIFICATION)")
                                .Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                            secCol.Item().Text($"Chữ ký mã hóa SHA-256: {verificationHash}").FontSize(7).FontFamily(Fonts.Consolas);
                            secCol.Item().Text($"Mã xác thực rút gọn (QR): {shortHash} | Quét mã QR góc trên để đối soát trực tiếp trên cổng đào tạo.").FontSize(7.5f).Italic();
                        });

                        // Signature Blocks
                        col.Item().PaddingTop(25).Row(row =>
                        {
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("CÁN BỘ CHẤM THI").Bold().FontSize(9);
                                c.Item().Text("(Ký và ghi rõ họ tên)").FontSize(8).Italic();
                                c.Item().PaddingTop(40).Text(graderName).Bold().FontSize(9);
                            });

                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("PHÒNG KHẢO THÍ").Bold().FontSize(9);
                                c.Item().Text("(Xác nhận điểm chính thức)").FontSize(8).Italic();
                                c.Item().PaddingTop(40).Text("ĐÃ XÁC THỰC").Bold().FontSize(9).FontColor(Colors.Green.Darken2);
                            });
                        });
                    });

                    // Footer
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Bản in chính thức từ Hệ thống Chấm thi Thực hành FPTU | Ngày in: ").FontSize(7).FontColor(Colors.Grey.Medium);
                            t.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss 'UTC'")).FontSize(7).FontColor(Colors.Grey.Medium);
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
            var fileName = $"BangDiem_{submission.StudentId}_{subjectCode}.pdf";

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
