// Controllers/AuthController.cs
using Jade.Data; // Para AppDbContext
using Jade.Dtos;
using Jade.Models;
using Microsoft.AspNetCore.Authorization; // Para proteger o Register no futuro
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Para ToListAsync, FirstOrDefaultAsync etc.

namespace Jade.Controllers // Seu namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<UsuarioAdmin> _userManager;
        // private readonly SignInManager<UsuarioAdmin> _signInManager; // Não estamos usando para login JWT
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole<int>> _roleManager; // INJETADO
        private readonly AppDbContext _context; // INJETADO para validar FKs

        public AuthController(
            UserManager<UsuarioAdmin> userManager,
            IConfiguration configuration,
            RoleManager<IdentityRole<int>> roleManager, // INJETADO
            AppDbContext context) // INJETADO
        {
            _userManager = userManager;
            _configuration = configuration;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto loginDto)
        {
            // Buscar usuário com suas entidades relacionadas para obter os nomes
            var user = await _userManager.Users // Acessa a coleção de usuários do UserManager
                .Include(u => u.Estabelecimento) // Inclui o Estabelecimento vinculado
                .Include(u => u.MarcaFranquiaVinculada) // Inclui a MarcaFranquia vinculada
                .FirstOrDefaultAsync(u => u.NormalizedEmail == _userManager.NormalizeEmail(loginDto.Email));

            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return Unauthorized(new { message = "Email ou senha inválidos." });
            }

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("roleInternal", user.Role) // Sua propriedade string Role
            };

            if (user.EstabelecimentoId.HasValue)
            {
                authClaims.Add(new Claim("estabelecimentoId", user.EstabelecimentoId.Value.ToString()));
            }
            if (user.MarcaFranquiaIdVinculada.HasValue)
            {
                authClaims.Add(new Claim("marcaFranquiaId", user.MarcaFranquiaIdVinculada.Value.ToString()));
            }

            var identityRoles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in identityRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
            var tokenValidityInMinutes = _configuration.GetValue<int>("JwtSettings:TokenValidityInMinutes", 120);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                expires: DateTime.UtcNow.AddMinutes(tokenValidityInMinutes), // Usando UtcNow
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new LoginResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo,
                UserId = user.Id.ToString(),
                Email = user.Email,
                NomeCompleto = user.NomeCompleto,
                Role = user.Role,
                EstabelecimentoId = user.EstabelecimentoId,
                MarcaFranquiaId = user.MarcaFranquiaIdVinculada,
                // Populando os novos campos
                NomeEstabelecimento = user.Estabelecimento?.Nome, // Usa o operador null-conditional
                NomeMarcaFranquia = user.MarcaFranquiaVinculada?.Nome // Usa o operador null-conditional
            });
        }


        // TODO: Proteger este endpoint para que apenas usuários autorizados (ex: SuperAdminSistema) possam criar outros usuários admin.
        // [Authorize(Roles = "SuperAdminSistema")] // Exemplo de proteção futura
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
                return StatusCode(StatusCodes.Status409Conflict, new { message = "Usuário com este email já existe." });

            // Validar Role
            if (!await _roleManager.RoleExistsAsync(registerDto.Role))
            {
                return BadRequest(new { message = $"O perfil (Role) '{registerDto.Role}' não existe. Perfis disponíveis: SuperAdminSistema, AdminMarcaFranquia, AdminLoja." });
            }

            // Validações de FK com base no Role
            if (registerDto.Role == "AdminLoja")
            {
                if (!registerDto.EstabelecimentoId.HasValue)
                    return BadRequest(new { message = "AdminLoja requer um EstabelecimentoId." });

                var estabelecimento = await _context.Estabelecimentos.FindAsync(registerDto.EstabelecimentoId.Value);
                if (estabelecimento == null)
                    return BadRequest(new { message = $"Estabelecimento com ID {registerDto.EstabelecimentoId} não encontrado." });

                // Opcional: Se AdminLoja é de uma franquia, MarcaFranquiaIdVinculada poderia ser o da marca do estabelecimento
                if (estabelecimento.MarcaFranquiaId.HasValue)
                {
                    registerDto.MarcaFranquiaIdVinculada = estabelecimento.MarcaFranquiaId;
                }

            }
            else if (registerDto.Role == "AdminMarcaFranquia")
            {
                if (!registerDto.MarcaFranquiaIdVinculada.HasValue)
                    return BadRequest(new { message = "AdminMarcaFranquia requer um MarcaFranquiaIdVinculada." });

                if (!await _context.MarcasFranquia.AnyAsync(mf => mf.Id == registerDto.MarcaFranquiaIdVinculada.Value))
                    return BadRequest(new { message = $"Marca/Franquia com ID {registerDto.MarcaFranquiaIdVinculada} não encontrada." });

                registerDto.EstabelecimentoId = null; // Admin de marca não está vinculado a uma loja específica diretamente
            }
            else if (registerDto.Role == "SuperAdminSistema")
            {
                registerDto.EstabelecimentoId = null;
                registerDto.MarcaFranquiaIdVinculada = null;
            }
            else
            {
                return BadRequest(new { message = "Perfil (Role) inválido fornecido." });
            }


            UsuarioAdmin user = new UsuarioAdmin()
            {
                Email = registerDto.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = registerDto.Email,
                NomeCompleto = registerDto.NomeCompleto,
                EstabelecimentoId = registerDto.EstabelecimentoId, // Pode ser null
                MarcaFranquiaIdVinculada = registerDto.MarcaFranquiaIdVinculada, // Pode ser null
                Role = registerDto.Role, // Armazena o Role
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Falha ao criar usuário.", errors });
            }

            // Adicionar usuário ao Role do Identity (para usar [Authorize(Roles="...")])
            var roleResult = await _userManager.AddToRoleAsync(user, registerDto.Role);
            if (!roleResult.Succeeded)
            {
                // Pode logar o erro, mas a criação do usuário já foi um sucesso.
                // Talvez retornar um warning ou uma mensagem diferente.
                // Por agora, vamos considerar sucesso se o usuário foi criado.
                var roleErrors = roleResult.Errors.Select(e => e.Description);
                Console.WriteLine($"Aviso: Falha ao adicionar usuário '{user.Email}' ao perfil '{registerDto.Role}'. Erros: {string.Join(", ", roleErrors)}");
            }

            return Ok(new { message = $"Usuário '{user.Email}' criado com sucesso e configurado com o perfil '{registerDto.Role}'!" });
        }
    }
}