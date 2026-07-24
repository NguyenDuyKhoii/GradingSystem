using FptuGradingSystem.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace FptuGradingSystem.Worker.Services
{
    public class MailtrapEmailService : IEmailService
    {
        private readonly ILogger<MailtrapEmailService> _logger;
        private readonly IConfiguration _configuration;

        public MailtrapEmailService(ILogger<MailtrapEmailService> logger, IConfiguration configuration)
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
            var host = _configuration["EmailSettings:SmtpServer"] ?? "sandbox.smtp.mailtrap.io";
            var portStr = _configuration["EmailSettings:SmtpPort"] ?? "2525";
            int.TryParse(portStr, out var port);
            var username = _configuration["EmailSettings:Username"] ?? "mailtrap_user";
            var password = _configuration["EmailSettings:Password"] ?? "mailtrap_pass";
            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "no-reply@fpt.edu.vn";
            var senderName = _configuration["EmailSettings:SenderName"] ?? "FPTU PE Grading System";

            var targetEmail = string.IsNullOrWhiteSpace(toEmail) ? $"{studentId.ToLower()}@fpt.edu.vn" : toEmail;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(studentName, targetEmail));
            message.Subject = $"[FPTU PE Result] Kế quả chấm thi môn {subjectName} - Lớp {classCode}";

            var statusBadge = isPassed 
                ? "<span style='color: #15803d; background-color: #dcfce7; padding: 4px 12px; border-radius: 9999px; font-weight: bold;'>PASSED</span>"
                : "<span style='color: #b91c1c; background-color: #fee2e2; padding: 4px 12px; border-radius: 9999px; font-weight: bold;'>FAILED</span>";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden;'>
                    <div style='background-color: #f97316; color: white; padding: 20px; text-align: center;'>
                        <h2 style='margin: 0;'>FPT University PE Grading Result</h2>
                        <p style='margin: 5px 0 0 0;'>Thông Báo Kết Quả Chấm Thi Thực Hành</p>
                    </div>
                    <div style='padding: 24px;'>
                        <p>Xin chào <strong>{studentName}</strong> ({studentId}),</p>
                        <p>Bài thi của bạn thuộc môn <strong>{subjectName}</strong> (Lớp <strong>{classCode}</strong>) đã được hoàn tất chấm điểm với chi tiết như sau:</p>
                        
                        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                            <tr style='background-color: #f3f4f6;'>
                                <td style='padding: 12px; border: 1px solid #e5e7eb;'><strong>Mã Sinh Viên:</strong></td>
                                <td style='padding: 12px; border: 1px solid #e5e7eb;'>{studentId}</td>
                            </tr>
                            <tr>
                                <td style='padding: 12px; border: 1px solid #e5e7eb;'><strong>Họ và Tên:</strong></td>
                                <td style='padding: 12px; border: 1px solid #e5e7eb;'>{studentName}</td>
                            </tr>
                            <tr style='background-color: #f3f4f6;'>
                                <td style='padding: 12px; border: 1px solid #e5e7eb;'><strong>Tổng Điểm (Thang 10):</strong></td>
                                <td style='padding: 12px; border: 1px solid #e5e7eb; font-size: 18px; font-weight: bold;'>{totalScore:F1}</td>
                            </tr>
                            <tr>
                                <td style='padding: 12px; border: 1px solid #e5e7eb;'><strong>Điểm Chữ (Letter Grade):</strong></td>
                                <td style='padding: 12px; border: 1px solid #e5e7eb; font-weight: bold;'>{letterGrade}</td>
                            </tr>
                            <tr style='background-color: #f3f4f6;'>
                                <td style='padding: 12px; border: 1px solid #e5e7eb;'><strong>Trạng Thái Môn Học:</strong></td>
                                <td style='padding: 12px; border: 1px solid #e5e7eb;'>{statusBadge}</td>
                            </tr>
                        </table>

                        <div style='background-color: #f8fafc; border-left: 4px solid #3b82f6; padding: 12px; margin-top: 16px;'>
                            <strong>Nhận xét từ Giảng viên:</strong>
                            <p style='margin: 8px 0 0 0; color: #475569;'>{(string.IsNullOrWhiteSpace(generalFeedback) ? "Không có nhận xét thêm." : generalFeedback)}</p>
                        </div>
                    </div>
                    <div style='background-color: #f3f4f6; color: #6b7280; padding: 12px; text-align: center; font-size: 12px;'>
                        <p style='margin: 0;'>Trường Đại học FPT - Hệ Thống Chấm Thi PE Tự Động Microservices</p>
                    </div>
                </div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using var client = new SmtpClient();
                _logger.LogInformation("Connecting to Mailtrap SMTP Server at {Host}:{Port}...", host, port);
                await client.ConnectAsync(host, port > 0 ? port : 2525, MailKit.Security.SecureSocketOptions.Auto);
                if (!string.IsNullOrEmpty(username) && username != "mailtrap_user")
                {
                    await client.AuthenticateAsync(username, password);
                }
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Successfully sent Grade Result Email to {ToEmail} for Student {StudentId}", targetEmail, studentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deliver email via Mailtrap SMTP to {ToEmail}. Email notification logged.", targetEmail);
            }
        }
    }
}
