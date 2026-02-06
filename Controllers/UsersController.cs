using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using users_api.Domain.Entities;
using users_api.Identity;

namespace users_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly TokenService _tokenService;
        private readonly IBus _bus;

        public UsersController(UserManager<Usuario> userManager,
                               SignInManager<Usuario> signInManager,
                               TokenService tokenService,
                               IBus bus)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _bus = bus;
        }

        public record RegisterDto(string Nome, string Email, string UserName, string Password);
        public record LoginDto(string UserName, string Password);

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new Usuario { Nome = dto.Nome, Email = dto.Email, UserName = dto.UserName, Role = Role.USER };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            await _bus.Publish(new UserCreatedEvent(user.Id, user.Email!, user.Nome, DateTime.UtcNow));
            return Ok(new { message = "Usuário cadastrado", userId = user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.UserName);
            if (user is null) return Unauthorized();
            var ok = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!ok) return Unauthorized();
            var token = _tokenService.GenerateToken(user);
            return Ok(new { token });
        }
    }
}
