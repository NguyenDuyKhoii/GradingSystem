using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Application.Common.Models;
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
        bool IsDescending = false,
        int PageNumber = 1,
        int PageSize = 10) : IRequest<PaginatedList<ExamClassDto>>;

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

    public class GetExamClassesQueryHandler : IRequestHandler<GetExamClassesQuery, PaginatedList<ExamClassDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetExamClassesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<ExamClassDto>> Handle(GetExamClassesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.ExamClasses
                .AsNoTracking()
                .Include(ec => ec.Class)
                .Include(ec => ec.Subject)
                .Include(ec => ec.Lecturer)
                .AsQueryable();

            // Filter by LecturerId (Role-based)
            if (request.LecturerId.HasValue)
            {
                query = query.Where(ec => ec.LecturerId == request.LecturerId.Value);
            }

            // Filter by Semester
            if (!string.IsNullOrEmpty(request.Semester))
            {
                query = query.Where(ec => ec.Semester == request.Semester);
            }

            // Filter by Status
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(ec => ec.Status == request.Status);
            }

            // Search by ClassCode, SubjectCode, SubjectName
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(ec => (ec.Class != null && ec.Class.ClassCode.ToLower().Contains(term)) ||
                                          (ec.Subject != null && (ec.Subject.SubjectCode.ToLower().Contains(term) || ec.Subject.SubjectName.ToLower().Contains(term))));
            }

            // Dynamic Sorting
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

            // Include Submissions for status computation, then project to DTO
            var projectedQuery = query
                .Include(ec => ec.Submissions)
                .Select(ec => new
                {
                    ec.Id,
                    ec.ClassId,
                    ClassCode = ec.Class != null ? ec.Class.ClassCode : "Unknown",
                    ec.SubjectId,
                    SubjectCode = ec.Subject!.SubjectCode,
                    SubjectName = ec.Subject!.SubjectName,
                    ec.Semester,
                    ec.LecturerId,
                    ec.Status,
                    TotalSubmissions = ec.Submissions.Count,
                    GradedCount = ec.Submissions.Count(s => s.Status == "Graded")
                });

            // Count total before pagination
            var totalCount = await projectedQuery.CountAsync(cancellationToken);

            // Apply pagination
            var items = await projectedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTO with computed status
            var dtoItems = items.Select(ec =>
            {
                string computedStatus = ec.Status;

                if (ec.Status == "GradesPublished" || ec.Status == "AppealOpen" || ec.Status == "Finalized")
                {
                    computedStatus = ec.Status;
                }
                else if (ec.TotalSubmissions > 0 && ec.GradedCount == ec.TotalSubmissions)
                {
                    computedStatus = "Completed";
                }
                else if (ec.GradedCount > 0 && ec.Status != "Completed")
                {
                    computedStatus = "Grading";
                }

                return new ExamClassDto(
                    ec.Id,
                    ec.ClassId,
                    ec.ClassCode,
                    ec.SubjectId,
                    ec.SubjectCode,
                    ec.SubjectName,
                    ec.Semester,
                    ec.LecturerId,
                    ec.LecturerId.HasValue ? $"Lecturer #{ec.LecturerId}" : "Not Assigned",
                    computedStatus);
            }).ToList();

            return new PaginatedList<ExamClassDto>(dtoItems, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
