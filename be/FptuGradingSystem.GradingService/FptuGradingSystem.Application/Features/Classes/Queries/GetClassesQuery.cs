using FptuGradingSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Features.Classes.Queries
{
    public record GetClassesQuery : IRequest<List<ClassDto>>;

    public record ClassDto(int Id, string ClassCode);

    public class GetClassesQueryHandler : IRequestHandler<GetClassesQuery, List<ClassDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetClassesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClassDto>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Classes
                .OrderBy(c => c.ClassCode)
                .Select(c => new ClassDto(c.Id, c.ClassCode))
                .ToListAsync(cancellationToken);
        }
    }
}
