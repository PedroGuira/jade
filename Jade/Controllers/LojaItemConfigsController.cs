// Controllers/LojaItemConfigsController.cs
using Jade.Data;
using Jade.Dtos;
using Jade.Models; // Para TipoItemConfiguravel
using Jade.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jade.Controllers
{
    [Authorize(Roles = "AdminLoja")] // Apenas AdminLoja de uma franquia pode configurar itens da franquia
    [Route("api/loja/configuracoes-itens")] // Rota específica para o AdminLoja logado
    [ApiController]
    public class LojaItemConfigsController : ControllerBase
    {
        private readonly ILojaItemConfigService _lojaItemConfigService;
        private readonly AppDbContext _context; // Para validar se a loja é franquia

        public LojaItemConfigsController(ILojaItemConfigService lojaItemConfigService, AppDbContext context)
        {
            _lojaItemConfigService = lojaItemConfigService;
            _context = context;
        }

        private bool TryGetAuthenticatedEstabelecimentoId(out int estabelecimentoId)
        {
            estabelecimentoId = 0;
            var estIdClaim = User.Claims.FirstOrDefault(c => c.Type == "estabelecimentoId");
            if (estIdClaim != null && int.TryParse(estIdClaim.Value, out int estIdParsed))
            {
                estabelecimentoId = estIdParsed;
                return true;
            }
            return false;
        }

        // POST api/loja/configuracoes-itens
        [HttpPost]
        public async Task<ActionResult<LojaItemConfigVerDto>> CriarOuAtualizarConfig(LojaItemConfigCriarAtualizarDto dto)
        {
            if (!TryGetAuthenticatedEstabelecimentoId(out int estId))
                return Unauthorized("Token inválido ou EstabelecimentoId não encontrado no token.");

            var loja = await _context.Estabelecimentos.FindAsync(estId);
            if (loja == null || !loja.MarcaFranquiaId.HasValue)
            {
                return Forbid("Esta operação é permitida apenas para lojas de franquia.");
            }

            var configResult = await _lojaItemConfigService.CriarOuAtualizarConfigAsync(dto, estId);
            if (configResult == null)
                return BadRequest("Não foi possível salvar a configuração. Verifique se o item original da franquia é válido.");

            // O service já trata se é create ou update.
            // Se o configResult.Id era 0 antes e agora tem valor, foi um create.
            // Se já tinha ID, foi um update.
            // Para simplificar a resposta HTTP, podemos retornar Ok com o objeto.
            // Se quisesse ser estritamente RESTful para create, poderia checar se foi criação
            // e retornar CreatedAtAction, mas Ok é aceitável aqui.
            return Ok(configResult);
        }

        // GET api/loja/configuracoes-itens
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LojaItemConfigVerDto>>> GetTodasConfiguracoes([FromQuery] TipoItemConfiguravel? tipoItem)
        {
            if (!TryGetAuthenticatedEstabelecimentoId(out int estId))
                return Unauthorized("Token inválido.");

            var configs = await _lojaItemConfigService.ObterTodasConfiguracoesDaLojaAsync(estId, tipoItem);
            return Ok(configs);
        }

        // GET api/loja/configuracoes-itens/item?itemOriginalId=X&tipoItem=Y
        [HttpGet("item")]
        public async Task<ActionResult<LojaItemConfigVerDto>> GetConfiguracaoPorItemOriginal(
            [FromQuery] int itemOriginalId, [FromQuery] TipoItemConfiguravel tipoItem)
        {
            if (!TryGetAuthenticatedEstabelecimentoId(out int estId))
                return Unauthorized("Token inválido.");

            var config = await _lojaItemConfigService.ObterConfigAsync(estId, itemOriginalId, tipoItem);
            if (config == null) return NotFound("Configuração não encontrada para este item na sua loja.");
            return Ok(config);
        }

        // DELETE api/loja/configuracoes-itens/{configId} (ID da própria LojaItemConfig)
        [HttpDelete("{configId}")]
        public async Task<IActionResult> DeleteConfiguracao(int configId)
        {
            if (!TryGetAuthenticatedEstabelecimentoId(out int estId))
                return Unauthorized("Token inválido.");

            var sucesso = await _lojaItemConfigService.DeletarConfigAsync(configId, estId);
            if (!sucesso) return NotFound("Configuração não encontrada ou não pertence à sua loja.");
            return NoContent();
        }
    }
}