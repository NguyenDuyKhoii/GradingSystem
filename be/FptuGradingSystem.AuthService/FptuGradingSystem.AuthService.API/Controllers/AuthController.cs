using FptuGradingSystem.AuthService.Application.Features.Auth.Commands;
using FptuGradingSystem.AuthService.Application.Features.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FptuGradingSystem.AuthService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<ActionResult<int>> Register(RegisterCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpGet("lecturers")]
        public async Task<ActionResult<List<LecturerDto>>> GetLecturers()
        {
            return await _mediator.Send(new GetLecturersQuery());
        }
    }
}
