// Controllers/MarcasFranquiaController.cs (NOVO CONTROLLER)
using Jade.Dtos; // Crie MarcaFranquiaCriarDto e MarcaFranquiaVerDto
using Jade.Services; // Crie IMarcaFranquiaService e MarcaFranquiaService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// ...

namespace Jade.Controllers
{
    [Authorize(Roles = "SuperAdminSistema")] // SÓ SUPER ADMIN PODE ACESSAR
    [Route("api/superadmin/marcas-franquia")] // Rota específica para super admin
    [ApiController]
    public class MarcasFranquiaController : ControllerBase
    {
        private readonly IMarcaFranquiaService _marcaFranquiaService;

        public MarcasFranquiaController(IMarcaFranquiaService marcaFranquiaService)
        {
            _marcaFranquiaService = marcaFranquiaService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MarcaFranquiaVerDto>>> GetMarcas()
        {
            return Ok(await _marcaFranquiaService.ObterTodasAsync());
        }

        [HttpPost]
        public async Task<ActionResult<MarcaFranquiaVerDto>> PostMarca(MarcaFranquiaCriarDto dto)
        {
            var marcaCriada = await _marcaFranquiaService.CriarAsync(dto);
            if (marcaCriada == null) return BadRequest();
            return CreatedAtAction(nameof(GetMarca), new { id = marcaCriada.Id }, marcaCriada);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MarcaFranquiaVerDto>> GetMarca(int id)
        {
            var marca = await _marcaFranquiaService.ObterPorIdAsync(id);
            if (marca == null) return NotFound();
            return Ok(marca);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMarca(int id, MarcaFranquiaCriarDto dto) // Reutilizando CriarDto para atualização
        {
            var sucesso = await _marcaFranquiaService.AtualizarAsync(id, dto);
            if (!sucesso)
            {
                return NotFound(new { message = $"Marca/Franquia com ID {id} não encontrada ou falha na atualização." });
            }
            return NoContent(); // Sucesso na atualização
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMarca(int id)
        {
            // O service deve lidar com a lógica de não permitir deletar se tiver
            // estabelecimentos ou templates vinculados, a menos que as regras de ON DELETE
            // no banco estejam configuradas para Cascade ou SetNull e isso seja o desejado.
            // No nosso caso, Estabelecimento.MarcaFranquiaId é SetNull.
            // Itens template (Categoria, Produto, etc.) com MarcaFranquiaId são Cascade.
            var sucesso = await _marcaFranquiaService.DeletarAsync(id);
            if (!sucesso)
            {
                return NotFound(new { message = $"Marca/Franquia com ID {id} não encontrada ou não pôde ser deletada." });
            }
            return NoContent(); // Sucesso na deleção
        }
    }
}