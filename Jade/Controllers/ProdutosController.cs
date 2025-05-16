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

        [HttpPost] // POST api/produtos (Painel Admin)
        public async Task<ActionResult<ProdutoVerDto>> PostProduto(ProdutoCriarDto produtoDto)
        {
            if (!TryGetAuthenticatedUserDetails(out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
                return Unauthorized("Token inválido.");

            ProdutoVerDto? produtoCriado;
            if (userRole == "AdminLoja" && estIdFromToken.HasValue)
            {
                produtoCriado = await _produtoService.CriarAsync(produtoDto, estIdFromToken.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaIdFromToken.HasValue)
            {
                // CategoriaId no DTO deve ser o ID de uma CategoriaTemplate da mesma marca
                produtoCriado = await _produtoService.CriarTemplateAsync(produtoDto, marcaIdFromToken.Value, produtoDto.CategoriaId);
            }
            else { return Forbid("Criação não permitida para este perfil ou contexto."); }

            if (produtoCriado == null) return BadRequest("Não foi possível criar o produto. Verifique os dados.");
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
    }
}