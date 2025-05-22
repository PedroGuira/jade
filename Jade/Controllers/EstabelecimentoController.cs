// Controllers/EstabelecimentosController.cs
using Jade.Dtos;
using Jade.Services;
using Microsoft.AspNetCore.Authorization; // Para proteger no futuro
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jade.Controllers // Seu namespace
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // Protegeremos este controller depois, quando tivermos roles de "super admin"
    // Por agora, vamos deixar aberto para facilitar a criação do primeiro estabelecimento.
    public class EstabelecimentosController : ControllerBase
    {
        private readonly IEstabelecimentoService _estabelecimentoService;

        public EstabelecimentosController(IEstabelecimentoService estabelecimentoService)
        {
            _estabelecimentoService = estabelecimentoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EstabelecimentoVerDto>>> GetEstabelecimentos()
        {
            var estabelecimentos = await _estabelecimentoService.ObterTodosAsync();
            return Ok(estabelecimentos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EstabelecimentoVerDto>> GetEstabelecimento(int id)
        {
            var estabelecimento = await _estabelecimentoService.ObterPorIdAsync(id);
            if (estabelecimento == null)
            {
                return NotFound();
            }
            return Ok(estabelecimento);
        }

        [HttpPost]
        public async Task<ActionResult<EstabelecimentoVerDto>> PostEstabelecimento(EstabelecimentoCriarDto estabelecimentoDto)
        {
            var estabelecimentoCriado = await _estabelecimentoService.CriarAsync(estabelecimentoDto);
            if (estabelecimentoCriado == null) // Pode acontecer se houver validação futura no service
            {
                return BadRequest("Não foi possível criar o estabelecimento.");
            }
            return CreatedAtAction(nameof(GetEstabelecimento), new { id = estabelecimentoCriado.Id }, estabelecimentoCriado);
        }

    }
}