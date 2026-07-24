using FptuGradingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Subject> Subjects { get; }
        DbSet<Rubric> Rubrics { get; }
        DbSet<RubricCriteria> RubricCriteria { get; }
        DbSet<ExamClass> ExamClasses { get; }
        DbSet<Submission> Submissions { get; }
        DbSet<Grade> Grades { get; }
        DbSet<GradeDetail> GradeDetails { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
