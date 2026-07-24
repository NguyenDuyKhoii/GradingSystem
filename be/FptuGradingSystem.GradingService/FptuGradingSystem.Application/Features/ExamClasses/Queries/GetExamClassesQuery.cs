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

            return await query
                .Select(ec => new ExamClassDto(
                    ec.Id,
                    ec.ClassCode,
                    ec.SubjectId,
                    ec.Subject!.SubjectCode,
                    ec.Subject!.SubjectName,
                    ec.Semester,
                    ec.LecturerId,
                    ec.Lecturer != null ? ec.Lecturer.FullName : "Not Assigned",
                    ec.Status))
                .ToListAsync(cancellationToken);
        }
    }
}
