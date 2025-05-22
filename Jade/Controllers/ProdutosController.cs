// Controllers/ProdutosController.cs
using Jade.Data;
using Jade.Dtos;
using Jade.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Jade.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutosController : ControllerBase
    {
        private readonly IProdutoService _produtoService;
        private readonly AppDbContext _context; // Usado para lógica de SuperAdmin ou validações complexas

        public ProdutosController(IProdutoService produtoService, AppDbContext context)
        {
            _produtoService = produtoService;
            _context = context;
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

        [HttpGet] // GET api/produtos (Painel Admin)
        public async Task<ActionResult<IEnumerable<ProdutoVerDto>>> GetProdutos([FromQuery] int? categoriaId)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
                return Unauthorized("Token inválido.");

            IEnumerable<ProdutoVerDto> produtos;
            if (userRole == "SuperAdminSistema")
            {
                // SuperAdmin pode ver tudo ou precisa de um contexto.
                // Para simplificar, vamos usar um método do service que retorna todos os produtos.
                // O frontend do SuperAdmin precisaria de filtros para Estabelecimento/Marca.
                produtos = await _produtoService.ObterTodosParaSuperAdminAsync(categoriaId);
            }
            else if (userRole == "AdminMarcaFranquia")
            {
                if (!marcaIdFromToken.HasValue) return BadRequest("Admin de Marca sem MarcaFranquiaId.");
                produtos = await _produtoService.ObterTodosTemplatesDaMarcaAsync(marcaIdFromToken.Value, categoriaId);
            }
            else if (userRole == "AdminLoja")
            {
                if (!estIdFromToken.HasValue) return BadRequest("Admin de Loja sem EstabelecimentoId.");
                produtos = await _produtoService.ObterTodosDaLojaEMarcaAsync(estIdFromToken.Value, categoriaId);
            }
            else { return Forbid(); }

            return Ok(produtos);
        }

        [HttpGet("{id}")] // GET api/produtos/{id} (Painel Admin)
        public async Task<ActionResult<ProdutoVerDto>> GetProduto(int id)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
                return Unauthorized("Token inválido.");

            ProdutoVerDto? produto;
            if (userRole == "AdminLoja" && estIdFromToken.HasValue)
            {
                produto = await _produtoService.ObterPorIdAsync(id, estIdFromToken.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaIdFromToken.HasValue)
            {
                produto = await _produtoService.ObterTemplateDaMarcaPorIdAsync(id, marcaIdFromToken.Value);
            }
            else if (userRole == "SuperAdminSistema")
            {
                produto = await _produtoService.ObterPorIdParaSuperAdminAsync(id);
            }
            else { return Forbid(); }

            if (produto == null) return NotFound($"Produto ID {id} não encontrado ou acesso negado.");
            return Ok(produto);
        }

        [HttpPost]
        public async Task<ActionResult<ProdutoVerDto>> PostProduto(ProdutoCriarDto produtoDto)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            ProdutoVerDto? produtoCriado;

            if (userRole == "AdminLoja" && estIdFromToken.HasValue)
            {
                // O ProdutoCriarDto do frontend NÃO envia mais estabelecimentoId.
                // O service CriarAsync espera o estIdFromToken.
                produtoCriado = await _produtoService.CriarAsync(produtoDto, estIdFromToken.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaIdFromToken.HasValue)
            {
                // O ProdutoCriarDto do frontend envia categoriaId (que deve ser de um CategoriaTemplate).
                // O service CriarTemplateAsync espera o marcaIdFromToken e o categoriaTemplateId.
                produtoCriado = await _produtoService.CriarTemplateAsync(produtoDto, marcaIdFromToken.Value, produtoDto.CategoriaId);
            }
            else if (userRole == "SuperAdminSistema")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "SuperAdmin deve operar dentro do contexto de uma Marca ou Estabelecimento para criar produtos." });
            }
            else
            {
                return Forbid("Perfil de usuário não autorizado para esta ação.");
            }

            if (produtoCriado == null)
            {
                return BadRequest(new { message = "Não foi possível criar o produto. Verifique os dados fornecidos e o contexto (categoria/marca)." });
            }
            return CreatedAtAction(nameof(GetProduto), new { id = produtoCriado.Id }, produtoCriado);
        }

        [HttpPut("{id}")] // PUT api/produtos/{id} (Painel Admin)
        public async Task<IActionResult> PutProduto(int id, ProdutoCriarDto produtoDto)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
                return Unauthorized("Token inválido.");

            bool sucesso = false;
            if (userRole == "AdminLoja" && estIdFromToken.HasValue)
            {
                sucesso = await _produtoService.AtualizarAsync(id, produtoDto, estIdFromToken.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaIdFromToken.HasValue)
            {
                sucesso = await _produtoService.AtualizarTemplateAsync(id, produtoDto, marcaIdFromToken.Value, produtoDto.CategoriaId);
            }
            else { return Forbid("Atualização não permitida."); }

            if (!sucesso) return NotFound($"Produto ID {id} não encontrado ou falha na atualização.");
            return NoContent();
        }

        [HttpDelete("{id}")] // DELETE api/produtos/{id} (Painel Admin)
        public async Task<IActionResult> DeleteProduto(int id)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
                return Unauthorized("Token inválido.");

            bool sucesso = false;
            if (userRole == "AdminLoja" && estIdFromToken.HasValue)
            {
                sucesso = await _produtoService.DeletarAsync(id, estIdFromToken.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaIdFromToken.HasValue)
            {
                sucesso = await _produtoService.DeletarTemplateAsync(id, marcaIdFromToken.Value);
            }
            else { return Forbid("Deleção não permitida."); }

            if (!sucesso) return NotFound($"Produto ID {id} não encontrado ou não pôde ser deletado.");
            return NoContent();
        }

        // --- Endpoints públicos para o cardápio do cliente (já existentes e ajustados) ---
        [AllowAnonymous]
        [HttpGet("/api/cardapio/{estabelecimentoId}/produtos")]
        public async Task<ActionResult<IEnumerable<ProdutoVerDto>>> GetCardapioProdutos(int estabelecimentoId, [FromQuery] int? categoriaId)
        {
            // Usando o método que combina itens da loja e templates da marca com configs locais
            var produtos = await _produtoService.ObterTodosDaLojaEMarcaAsync(estabelecimentoId, categoriaId, true);
            return Ok(produtos); // O service já deve filtrar por disponibilidade se apenasAtivosParaCardapio=true
        }

        [AllowAnonymous]
        [HttpGet("/api/cardapio/{estabelecimentoId}/categorias")]
        public async Task<ActionResult<IEnumerable<CategoriaVerDto>>> GetCardapioCategorias(int estabelecimentoId, [FromServices] ICategoriaService categoriaService)
        {
            // Este método do categoriaService também precisa ser o que combina categorias da loja e templates
            var categorias = await categoriaService.ObterTodasDaLojaEMarcaAsync(estabelecimentoId);
            return Ok(categorias);
        }

        [AllowAnonymous] // Este endpoint é para o cliente final, não precisa de login do painel admin
        [HttpGet("/api/cardapio/produtos/{produtoId}/opcoes")] // Rota absoluta e específica
        public async Task<ActionResult<IEnumerable<GrupoOpcaoVerDto>>> GetProdutoOpcoes(
    int produtoId,
    [FromQuery] int estabelecimentoId) // O estabelecimentoId vem como query parameter
        {
            // Debug.WriteLine($"--- GetProdutoOpcoes: produtoId={produtoId}, estabelecimentoId={estabelecimentoId} ---");

            // Validação básica se produto e loja existem
            var produtoBase = await _context.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == produtoId);
            var loja = await _context.Estabelecimentos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == estabelecimentoId);

            if (produtoBase == null)
            {
                // Debug.WriteLine($"[GetProdutoOpcoes] Produto ID {produtoId} não encontrado. Retornando 404.");
                return NotFound(new { message = $"Produto com ID {produtoId} não encontrado." });
            }
            if (loja == null)
            {
                // Debug.WriteLine($"[GetProdutoOpcoes] Loja ID {estabelecimentoId} não encontrada. Retornando 404.");
                return NotFound(new { message = $"Estabelecimento com ID {estabelecimentoId} não encontrado." });
            }

            // Validação se o produto é acessível pela loja (local ou template da marca da loja)
            bool acessoPermitidoAoProduto =
                (produtoBase.EstabelecimentoId == estabelecimentoId && produtoBase.MarcaFranquiaId == null) || // Produto é local desta loja
                (produtoBase.EstabelecimentoId == null && // Produto é template
                 produtoBase.MarcaFranquiaId.HasValue &&  // Template pertence a uma marca
                 loja.MarcaFranquiaId.HasValue &&         // Loja pertence a uma marca
                 produtoBase.MarcaFranquiaId == loja.MarcaFranquiaId.Value); // Template é da marca da loja

            if (!acessoPermitidoAoProduto)
            {
                // Debug.WriteLine($"[GetProdutoOpcoes] Acesso ao Produto ID {produtoId} (Marca: {produtoBase.MarcaFranquiaId}) negado para Loja ID {estabelecimentoId} (Marca: {loja.MarcaFranquiaId}). Retornando 404.");
                return NotFound(new { message = "Produto não disponível para este estabelecimento." });
            }

            // Chama o service para obter as opções de personalização configuradas
            var opcoes = await _produtoService.ObterOpcoesDePersonalizacaoAsync(produtoId, estabelecimentoId);

            // O service já deve retornar uma lista vazia se não houver opções válidas,
            // não null, para evitar um 500 aqui.
            if (opcoes == null)
            { // Segurança extra, embora o service deva retornar List<>()
              // Debug.WriteLine($"[GetProdutoOpcoes] _produtoService.ObterOpcoesDePersonalizacaoAsync retornou null para Produto ID {produtoId}, Loja ID {estabelecimentoId}.");
                return Ok(new List<GrupoOpcaoVerDto>()); // Retorna lista vazia em vez de erro
            }

            // Debug.WriteLine($"[GetProdutoOpcoes] Retornando {opcoes.Count()} grupos de opção para Produto ID {produtoId}, Loja ID {estabelecimentoId}.");
            return Ok(opcoes);
        }

    }
}