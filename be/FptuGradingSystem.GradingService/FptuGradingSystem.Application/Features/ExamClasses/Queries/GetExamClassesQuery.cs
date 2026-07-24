using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Queries
{
    public record GetExamClassesQuery(int? LecturerId = null, string? Semester = null) : IRequest<List<ExamClassDto>>;

    public record ExamClassDto(
        int Id,
        int ClassId,
        string ClassCode,
        int SubjectId,
        string SubjectCode,
        string SubjectName,
        string Semester,
        int? LecturerId,
        string LecturerName,
        string Status);

    public class GetExamClassesQueryHandler : IRequestHandler<GetExamClassesQuery, List<ExamClassDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetExamClassesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ExamClassDto>> Handle(GetExamClassesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.ExamClasses
                .Include(ec => ec.Class)
                .Include(ec => ec.Subject)
                .Include(ec => ec.Lecturer)
                .AsQueryable();

            if (request.LecturerId.HasValue)
            {
                query = query.Where(ec => ec.LecturerId == request.LecturerId.Value);
            }

            if (!string.IsNullOrEmpty(request.Semester))
            {
                query = query.Where(ec => ec.Semester == request.Semester);
            }

            var list = await query
                .Include(ec => ec.Submissions)
                .ToListAsync(cancellationToken);

            return list.Select(ec =>
            {
                var totalSubmissions = ec.Submissions.Count;
                var gradedCount = ec.Submissions.Count(s => s.Status == "Graded");
                string computedStatus = ec.Status;

                if (totalSubmissions > 0 && gradedCount == totalSubmissions)
                {
                    computedStatus = "Completed";
                }
                else if (gradedCount > 0 && ec.Status != "Completed")
                {
                    computedStatus = "Grading";
                }

                return new ExamClassDto(
                    ec.Id,
                    ec.ClassId,
                    ec.Class != null ? ec.Class.ClassCode : "Unknown",
                    ec.SubjectId,
                    ec.Subject!.SubjectCode,
                    ec.Subject!.SubjectName,
                    ec.Semester,
                    ec.LecturerId,
                    ec.LecturerId.HasValue ? $"Lecturer #{ec.LecturerId}" : "Not Assigned",
                    computedStatus);
            }).ToList();
        }
    }
}
