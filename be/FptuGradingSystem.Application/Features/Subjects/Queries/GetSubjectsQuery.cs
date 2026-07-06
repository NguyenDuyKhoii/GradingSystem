using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Subjects.Queries
{
    public record GetSubjectsQuery : IRequest<List<SubjectDto>>;

    public record SubjectDto(int Id, string SubjectCode, string SubjectName);

    public class GetSubjectsQueryHandler : IRequestHandler<GetSubjectsQuery, List<SubjectDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetSubjectsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SubjectDto>> Handle(GetSubjectsQuery request, CancellationToken cancellationToken)
        {
            return await _context.Subjects
                .Select(s => new SubjectDto(s.Id, s.SubjectCode, s.SubjectName))
                .ToListAsync(cancellationToken);
        }
    }
}
