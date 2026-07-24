using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Rubric> Rubrics => Set<Rubric>();
        public DbSet<RubricCriteria> RubricCriteria => Set<RubricCriteria>();
        public DbSet<ExamClass> ExamClasses => Set<ExamClass>();
        public DbSet<Submission> Submissions => Set<Submission>();
        public DbSet<Grade> Grades => Set<Grade>();
        public DbSet<GradeDetail> GradeDetails => Set<GradeDetail>();
        public DbSet<Class> Classes => Set<Class>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Unique Indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.SubjectCode)
                .IsUnique();

            // Configure Decimals Precision
            modelBuilder.Entity<Rubric>()
                .Property(r => r.TotalWeight)
                .HasPrecision(5, 2);

            modelBuilder.Entity<RubricCriteria>()
                .Property(rc => rc.MaxPoints)
                .HasPrecision(5, 2);

            modelBuilder.Entity<RubricCriteria>()
                .Property(rc => rc.Weight)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Grade>()
                .Property(g => g.TotalScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<GradeDetail>()
                .Property(gd => gd.Score)
                .HasPrecision(5, 2);

            // Configure Relationships
            modelBuilder.Entity<ExamClass>()
                .HasOne(ec => ec.Lecturer)
                .WithMany(u => u.ExamClasses)
                .HasForeignKey(ec => ec.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Submission)
                .WithOne(s => s.Grade)
                .HasForeignKey<Grade>(g => g.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.GradedBy)
                .WithMany(u => u.Grades)
                .HasForeignKey(g => g.GradedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GradeDetail>()
                .HasOne(gd => gd.Grade)
                .WithMany(g => g.GradeDetails)
                .HasForeignKey(gd => gd.GradeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GradeDetail>()
                .HasOne(gd => gd.RubricCriteria)
                .WithMany(rc => rc.GradeDetails)
                .HasForeignKey(gd => gd.RubricCriteriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Class constraints
            modelBuilder.Entity<Class>()
                .HasIndex(c => c.ClassCode)
                .IsUnique();

            modelBuilder.Entity<ExamClass>()
                .HasOne(ec => ec.Class)
                .WithMany(c => c.ExamClasses)
                .HasForeignKey(ec => ec.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: one exam class per (Class, Subject, Semester)
            modelBuilder.Entity<ExamClass>()
                .HasIndex(ec => new { ec.ClassId, ec.SubjectId, ec.Semester })
                .IsUnique();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
