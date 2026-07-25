using FptuGradingSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FptuGradingSystem.Worker.Services
{
    public class BrevoEmailService : IEmailService
    {
        private readonly ILogger<BrevoEmailService> _logger;
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _httpClient = new();

        public BrevoEmailService(ILogger<BrevoEmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendGradeResultEmailAsync(
            string toEmail,
            string studentName,
            string studentId,
            string classCode,
            string subjectName,
            decimal totalScore,
            string letterGrade,
            bool isPassed,
            string generalFeedback)
        {
            var apiKey = _configuration["EmailSettings:ApiKey"] ?? "";
            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "no-reply@fpt.edu.vn";
            var senderName = _configuration["EmailSettings:SenderName"] ?? "FPTU PE Grading System";

            var targetEmail = string.IsNullOrWhiteSpace(toEmail)
                ? $"{studentId.ToLower()}@fpt.edu.vn"
                : toEmail;

            var statusBadge = isPassed
                ? "<span style='color: #15803d; background-color: #dcfce7; padding: 4px 12px; border-radius: 9999px; font-weight: bold;'>PASSED ✓</span>"
                : "<span style='color: #b91c1c; background-color: #fee2e2; padding: 4px 12px; border-radius: 9999px; font-weight: bold;'>FAILED ✗</span>";

            var htmlBody = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden;'>
    <div style='background-color: #f97316; color: white; padding: 20px; text-align: center;'>
        <h2 style='margin: 0;'>FPT University PE Grading Result</h2>
        <p style='margin: 5px 0 0 0;'>Thông Báo Kết Quả Chấm Thi Thực Hành</p>
    </div>
    <div style='padding: 24px;'>
        <p>Xin chào <strong>{studentName}</strong> ({studentId}),</p>
        <p>Bài thi của bạn thuộc môn <strong>{subjectName}</strong> (Lớp <strong>{classCode}</strong>) đã được hoàn tất chấm điểm:</p>
        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
            <tr style='background-color: #f3f4f6;'>
                <td style='padding: 12px; border: 1px solid #e5e7eb;'><strong>Mã Sinh Viên</strong></td>
                <td style='padding: 12px; border: 1px solid #e5e7eb;'>{studentId}</td>
            </tr>
            <tr>
                <td style='padding: 12px; border: 1px solid #e5e7eb;'><strong>Họ và Tên</strong></td>
                <td style='padding: 12px; border: 1px solid #e5e7eb;'>{studentName}</td>
            </tr>
            <tr style='background-color: #f3f4f6;'>
                <td style='padding: 12px; border: 1px solid #e5e7eb;'><strong>Tổng Điểm (Thang 10)</strong></td>
                <td style='padding: 12px; border: 1px solid #e5e7eb; font-size: 18px; font-weight: bold; color: #f97316;'>{totalScore:F1}</td>
            </tr>
            <tr>
                <td style='padding: 12px; border: 1px solid #e5e7eb;'><strong>Điểm Chữ (Letter Grade)</strong></td>
                <td style='padding: 12px; border: 1px solid #e5e7eb; font-weight: bold;'>{letterGrade}</td>
            </tr>
            <tr style='background-color: #f3f4f6;'>
                <td style='padding: 12px; border: 1px solid #e5e7eb;'><strong>Trạng Thái Môn Học</strong></td>
                <td style='padding: 12px; border: 1px solid #e5e7eb;'>{statusBadge}</td>
            </tr>
        </table>
        <div style='background-color: #f8fafc; border-left: 4px solid #3b82f6; padding: 12px; margin-top: 16px;'>
            <strong>Nhận xét từ Giảng viên:</strong>
            <p style='margin: 8px 0 0 0; color: #475569;'>{(string.IsNullOrWhiteSpace(generalFeedback) ? "Không có nhận xét thêm." : generalFeedback)}</p>
        </div>
    </div>
    <div style='background-color: #f3f4f6; color: #6b7280; padding: 12px; text-align: center; font-size: 12px;'>
        <p style='margin: 0;'>Trường Đại học FPT - Hệ Thống Chấm Thi PE Tự Động</p>
    </div>
</div>";

            var payload = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = targetEmail, name = string.IsNullOrWhiteSpace(studentName) ? studentId : studentName } },
                subject = $"[FPTU PE Result] Kết quả chấm thi môn {subjectName} - Lớp {classCode}",
                htmlContent = htmlBody
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("⚠️ BREVO_API_KEY chưa được cấu hình. [Simulated Email Delivery] Đã giả lập gửi Email điểm thi tới {ToEmail} (Học viên: {StudentId}, Điểm: {Score}, Grade: {Grade})", targetEmail, studentId, totalScore, letterGrade);
                return;
            }

            try
            {
                _logger.LogInformation("Sending grade email via Brevo API to {ToEmail} for Student {StudentId}...", targetEmail, studentId);
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Email sent successfully via Brevo API to {ToEmail}. Response: {Body}", targetEmail, responseBody);
                }
                else
                {
                    _logger.LogWarning("❌ Brevo API returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                    _logger.LogInformation("ℹ️ [Dev Fallback] Đã ghi nhận Email thông báo điểm thi gửi đến {ToEmail} cho Sinh viên {StudentId}.", targetEmail, studentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email via Brevo API to {ToEmail}.", targetEmail);
            }
        }
    }
}
