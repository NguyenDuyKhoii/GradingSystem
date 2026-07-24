using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.ExamClasses.Queries
{
    public record GetExamClassesQuery(
        int? LecturerId = null,
        string? Semester = null,
        string? SearchTerm = null,
        string? Status = null,
        string? SortBy = null,
        bool IsDescending = false) : IRequest<List<ExamClassDto>>;

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
                .AsNoTracking()
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

            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(ec => ec.Status == request.Status);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(ec => (ec.Class != null && ec.Class.ClassCode.ToLower().Contains(term)) ||
                                          (ec.Subject != null && (ec.Subject.SubjectCode.ToLower().Contains(term) || ec.Subject.SubjectName.ToLower().Contains(term))));
            }

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                var sortByLower = request.SortBy.ToLower();
                if (sortByLower == "classcode" || sortByLower == "code")
                {
                    query = request.IsDescending ? query.OrderByDescending(ec => ec.Class != null ? ec.Class.ClassCode : "") : query.OrderBy(ec => ec.Class != null ? ec.Class.ClassCode : "");
                }
                else if (sortByLower == "semester")
                {
                    query = request.IsDescending ? query.OrderByDescending(ec => ec.Semester) : query.OrderBy(ec => ec.Semester);
                }
                else if (sortByLower == "status")
                {
                    query = request.IsDescending ? query.OrderByDescending(ec => ec.Status) : query.OrderBy(ec => ec.Status);
                }
                else
                {
                    query = query.OrderBy(ec => ec.Id);
                }
            }
            else
            {
                query = query.OrderBy(ec => ec.Id);
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
