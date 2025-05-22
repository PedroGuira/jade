// Controllers/GruposOpcaoController.cs
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
    [Route("api/[controller]")] // api/gruposopcao
    [ApiController]
    public class GruposOpcaoController : ControllerBase
    {
        private readonly IGrupoOpcaoService _grupoOpcaoService;

        public GruposOpcaoController(IGrupoOpcaoService grupoOpcaoService)
        {
            _grupoOpcaoService = grupoOpcaoService;
        }

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
        public async Task<ActionResult<IEnumerable<GrupoOpcaoVerDto>>> GetGruposOpcao()
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estId, out int? marcaId))
                return Unauthorized("Token inválido ou informações de usuário ausentes.");

            IEnumerable<GrupoOpcaoVerDto> grupos;
            if (userRole == "SuperAdminSistema")
            {
                grupos = await _grupoOpcaoService.ObterTodosParaSuperAdminAsync();
            }
            else if (userRole == "AdminMarcaFranquia")
            {
                if (!marcaId.HasValue) return BadRequest("Admin de Marca sem MarcaFranquiaId associada.");
                grupos = await _grupoOpcaoService.ObterTodosTemplatesDaMarcaAsync(marcaId.Value);
            }
            else if (userRole == "AdminLoja")
            {
                if (!estId.HasValue) return BadRequest("Admin de Loja sem EstabelecimentoId associado.");
                grupos = await _grupoOpcaoService.ObterTodosParaLojaDeFranquiaAsync(estId.Value);
            }
            else { return Forbid(); }

            return Ok(grupos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GrupoOpcaoVerDto>> GetGrupoOpcao(int id)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estId, out int? marcaId))
                return Unauthorized("Token inválido.");

            GrupoOpcaoVerDto? grupo;
            if (userRole == "SuperAdminSistema")
            {
                grupo = await _grupoOpcaoService.ObterPorIdParaSuperAdminAsync(id);
            }
            else if (userRole == "AdminMarcaFranquia")
            {
                if (!marcaId.HasValue) return BadRequest("Admin de Marca sem MarcaFranquiaId.");
                grupo = await _grupoOpcaoService.ObterTemplateDaMarcaPorIdAsync(id, marcaId.Value);
            }
            else if (userRole == "AdminLoja")
            {
                if (!estId.HasValue) return BadRequest("Admin de Loja sem EstabelecimentoId.");
                // Um AdminLoja pode ver um grupo local ou um template da sua marca
                grupo = await _grupoOpcaoService.ObterDeLojaPorIdAsync(id, estId.Value) ??
                        await _grupoOpcaoService.ObterTemplateDaMarcaDaSuaFranquiaAsync(id, estId.Value); // Método novo/adaptado
            }
            else { return Forbid(); }

            if (grupo == null) return NotFound($"Grupo de opção com ID {id} não encontrado ou acesso negado.");
            return Ok(grupo);
        }
        // Adicionar método ao IGrupoOpcaoService e GrupoOpcaoService:
        // Task<GrupoOpcaoVerDto?> ObterTemplateDaMarcaDaSuaFranquiaAsync(int grupoOpcaoTemplateId, int lojaEstabelecimentoId);
        // que busca o grupo template se a loja pertencer à marca do grupo.

        [HttpPost]
        public async Task<ActionResult<GrupoOpcaoVerDto>> PostGrupoOpcao(GrupoOpcaoCriarDto dto)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estId, out int? marcaId))
                return Unauthorized("Token inválido.");

            GrupoOpcaoVerDto? grupoCriado;
            if (userRole == "AdminLoja" && estId.HasValue)
            {
                grupoCriado = await _grupoOpcaoService.CriarParaLojaAsync(dto, estId.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaId.HasValue)
            {
                grupoCriado = await _grupoOpcaoService.CriarTemplateParaMarcaAsync(dto, marcaId.Value);
            }
            else if (userRole == "SuperAdminSistema")
            {
                // SuperAdmin precisaria especificar para qual marca/loja está criando, ou ter um contexto "global"
                return Forbid("SuperAdmin precisa de um contexto (marca/loja) para criar grupos de opção. Use o painel de Marcas.");
            }
            else { return Forbid("Criação não permitida."); }

            if (grupoCriado == null) return BadRequest("Não foi possível criar o grupo de opção. Verifique os dados.");
            return CreatedAtAction(nameof(GetGrupoOpcao), new { id = grupoCriado.Id }, grupoCriado);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutGrupoOpcao(int id, GrupoOpcaoCriarDto dto)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estId, out int? marcaId))
                return Unauthorized("Token inválido.");

            bool sucesso = false;
            if (userRole == "AdminLoja" && estId.HasValue)
            {
                sucesso = await _grupoOpcaoService.AtualizarDeLojaAsync(id, dto, estId.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaId.HasValue)
            {
                sucesso = await _grupoOpcaoService.AtualizarTemplateDaMarcaAsync(id, dto, marcaId.Value);
            }
            else { return Forbid("Atualização não permitida."); }

            if (!sucesso) return NotFound($"Grupo de opção com ID {id} não encontrado ou falha na atualização.");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrupoOpcao(int id)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estId, out int? marcaId))
                return Unauthorized("Token inválido.");

            bool sucesso = false;
            if (userRole == "AdminLoja" && estId.HasValue)
            {
                sucesso = await _grupoOpcaoService.DeletarDeLojaAsync(id, estId.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaId.HasValue)
            {
                sucesso = await _grupoOpcaoService.DeletarTemplateDaMarcaAsync(id, marcaId.Value);
            }
            else { return Forbid("Deleção não permitida."); }

            if (!sucesso) return NotFound($"Grupo de opção com ID {id} não encontrado ou não pôde ser deletado.");
            return NoContent();
        }
    }
}