using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendGradeResultEmailAsync(
            string toEmail,
            string studentName,
            string studentId,
            string classCode,
            string subjectName,
            decimal totalScore,
            string letterGrade,
            bool isPassed,
            string generalFeedback);
    }
}
