using FptuGradingSystem.AuthService.Application.Features.Auth.Commands;
using FptuGradingSystem.AuthService.Application.Features.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

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
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            try
            {
                var command = new LoginCommand(
                    request.Username,
                    request.Password,
                    GetIpAddress(),
                    GetUserAgent()
                );
                return await _mediator.Send(command);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var command = new RefreshTokenCommand(
                    request.RefreshToken,
                    GetIpAddress(),
                    GetUserAgent()
                );
                return await _mediator.Send(command);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request)
        {
            await _mediator.Send(new RevokeTokenCommand(
                request.RefreshToken,
                null,
                null,
                GetIpAddress(),
                request.Reason ?? "Token revoked via API"
            ));
            return Ok(new { message = "Refresh token đã được thu hồi thành công." });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
        {
            var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out var userId);

            await _mediator.Send(new RevokeTokenCommand(
                request?.RefreshToken,
                jti,
                userId > 0 ? userId : null,
                GetIpAddress(),
                "User logged out"
            ));

            return Ok(new { message = "Đăng xuất thành công. Token đã được đưa vào blacklist." });
        }

        [HttpGet("lecturers")]
        public async Task<ActionResult<System.Collections.Generic.List<LecturerDto>>> GetLecturers(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool isDescending = false)
        {
            return await _mediator.Send(new GetLecturersQuery(searchTerm, sortBy, isDescending));
        }

        private string? GetIpAddress() =>
            Request.Headers.ContainsKey("X-Forwarded-For")
                ? Request.Headers["X-Forwarded-For"].ToString()
                : HttpContext.Connection.RemoteIpAddress?.ToString();

        private string? GetUserAgent() => Request.Headers["User-Agent"].ToString();
    }

    public record LoginRequest(string Username, string Password);
    public record RefreshTokenRequest(string RefreshToken);
    public record RevokeTokenRequest(string RefreshToken, string? Reason = null);
    public record LogoutRequest(string? RefreshToken = null);
}
