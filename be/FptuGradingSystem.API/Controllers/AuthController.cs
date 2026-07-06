using FptuGradingSystem.Application.Features.Auth.Commands;
using FptuGradingSystem.Application.Features.Auth.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FptuGradingSystem.API.Controllers
{
    public class AuthController : ApiControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<int>> Register(RegisterCommand command)
        {
            return await Mediator.Send(command);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginCommand command)
        {
            return await Mediator.Send(command);
        }

        [HttpGet("lecturers")]
        public async Task<ActionResult<List<LecturerDto>>> GetLecturers()
        {
            return await Mediator.Send(new GetLecturersQuery());
        }
    }
}
