// Controllers/ItensOpcaoController.cs
using Jade.Dtos;
using Jade.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Jade.Controllers
{
    [Authorize]
    [Route("api/gruposopcao/{grupoOpcaoId}/[controller]")] // api/gruposopcao/{grupoId}/itensopcao
    [ApiController]
    public class ItensOpcaoController : ControllerBase
    {
        private readonly IItemOpcaoService _itemOpcaoService;
        // IGrupoOpcaoService é injetado no ItemOpcaoService agora para validação interna

        public ItensOpcaoController(IItemOpcaoService itemOpcaoService)
        {
            _itemOpcaoService = itemOpcaoService;
        }

        // Método helper para obter detalhes do usuário logado
        private bool TryGetAuthenticatedUserDetails(out string userRole, out int? estabelecimentoId, out int? marcaFranquiaId)
        {
            userRole = string.Empty; estabelecimentoId = null; marcaFranquiaId = null;
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "roleInternal");
            if (roleClaim == null) return false; userRole = roleClaim.Value;
            var estIdClaim = User.Claims.FirstOrDefault(c => c.Type == "estabelecimentoId");
            if (estIdClaim != null && int.TryParse(estIdClaim.Value, out int estIdParsed)) estabelecimentoId = estIdParsed;
            var marcaIdClaim = User.Claims.FirstOrDefault(c => c.Type == "marcaFranquiaId");
            if (marcaIdClaim != null && int.TryParse(marcaIdClaim.Value, out int marcaIdParsed)) marcaFranquiaId = marcaIdParsed;
            return true;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemOpcaoVerDto>>> GetItensOpcao(int grupoOpcaoId)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estId, out int? marcaId))
                return Unauthorized("Token inválido.");

            var itens = await _itemOpcaoService.ObterTodosPorGrupoAsync(grupoOpcaoId, estId, userRole, marcaId);
            // O service ObterTodosPorGrupoAsync já deve ter validado o acesso ao grupoOpcaoId
            // Se retornar uma lista (mesmo vazia), significa que o acesso ao grupo pai foi ok.
            return Ok(itens);
        }

        [HttpGet("{itemId}")]
        public async Task<ActionResult<ItemOpcaoVerDto>> GetItemOpcao(int grupoOpcaoId, int itemId)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estId, out int? marcaId))
                return Unauthorized("Token inválido.");

            var item = await _itemOpcaoService.ObterPorIdAsync(itemId, grupoOpcaoId, estId, userRole, marcaId);
            if (item == null) return NotFound($"Item de opção ID {itemId} não encontrado para o grupo ID {grupoOpcaoId} ou acesso negado.");
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ItemOpcaoVerDto>> PostItemOpcao(int grupoOpcaoId, ItemOpcaoCriarDto itemDto)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estId, out int? marcaId))
                return Unauthorized("Token inválido.");

            var itemCriado = await _itemOpcaoService.CriarAsync(itemDto, grupoOpcaoId, estId, userRole, marcaId);
            if (itemCriado == null) return BadRequest("Não foi possível criar o item de opção. Verifique o grupo pai ou os dados do item.");

            return CreatedAtAction(nameof(GetItemOpcao), new { grupoOpcaoId = grupoOpcaoId, itemId = itemCriado.Id }, itemCriado);
        }

        [HttpPut("{itemId}")]
        public async Task<IActionResult> PutItemOpcao(int grupoOpcaoId, int itemId, ItemOpcaoCriarDto itemDto)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estId, out int? marcaId))
                return Unauthorized("Token inválido.");

            var sucesso = await _itemOpcaoService.AtualizarAsync(itemId, itemDto, grupoOpcaoId, estId, userRole, marcaId);
            if (!sucesso) return NotFound($"Item de opção ID {itemId} não encontrado para o grupo ID {grupoOpcaoId} ou falha na atualização.");

            return NoContent();
        }

        [HttpDelete("{itemId}")]
        public async Task<IActionResult> DeleteItemOpcao(int grupoOpcaoId, int itemId)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estId, out int? marcaId))
                return Unauthorized("Token inválido.");

            var sucesso = await _itemOpcaoService.DeletarAsync(itemId, grupoOpcaoId, estId, userRole, marcaId);
            if (!sucesso) return NotFound($"Item de opção ID {itemId} não encontrado para o grupo ID {grupoOpcaoId} ou não pôde ser deletado.");

            return NoContent();
        }
    }
}