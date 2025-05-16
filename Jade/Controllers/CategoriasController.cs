// Controllers/CategoriasController.cs
using Jade.Data;
using Jade.Dtos;
using Jade.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Adicionado para FirstOrDefaultAsync
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Jade.Controllers
{
    [Authorize] // Todas as actions aqui exigem autenticação
    [Route("api/[controller]")] // Rota base: api/categorias
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private readonly ICategoriaService _categoriaService;
        private readonly AppDbContext _context; // Injetado para lógica específica do SuperAdmin

        public CategoriasController(ICategoriaService categoriaService, AppDbContext context)
        {
            _categoriaService = categoriaService;
            _context = context;
        }

        // Método helper para obter detalhes do usuário logado
        private bool TryGetAuthenticatedUserDetails(out int userId, out string userRole, out int? estabelecimentoId, out int? marcaFranquiaId)
        {
            userId = 0;
            userRole = string.Empty;
            estabelecimentoId = null;
            marcaFranquiaId = null;

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "roleInternal" || c.Type == ClaimTypes.Role); // Prioriza sua claim interna
            var estIdClaim = User.Claims.FirstOrDefault(c => c.Type == "estabelecimentoId");
            var marcaIdClaim = User.Claims.FirstOrDefault(c => c.Type == "marcaFranquiaId");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId) || roleClaim == null)
            {
                return false; // Informações básicas do usuário ausentes ou inválidas
            }
            userRole = roleClaim.Value;

            if (estIdClaim != null && int.TryParse(estIdClaim.Value, out int estIdParsed))
            {
                estabelecimentoId = estIdParsed;
            }
            if (marcaIdClaim != null && int.TryParse(marcaIdClaim.Value, out int marcaIdParsed))
            {
                marcaFranquiaId = marcaIdParsed;
            }
            return true;
        }


        [HttpGet] // GET api/categorias
        public async Task<ActionResult<IEnumerable<CategoriaVerDto>>> GetCategorias()
        {
            if (!TryGetAuthenticatedUserDetails(out _, out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
            {
                return Unauthorized(new { message = "Token inválido ou informações de usuário ausentes." });
            }

            IEnumerable<CategoriaVerDto> categorias;

            if (userRole == "SuperAdminSistema")
            {
                // SuperAdmin: Precisa de uma forma de listar todas as categorias de TODOS os estabelecimentos,
                // ou de um estabelecimento específico se um filtro for fornecido.
                // Por agora, vamos retornar uma lista vazia ou um erro, pois não temos um filtro.
                // Idealmente, o SuperAdmin teria uma interface diferente ou filtros.
                // Para o CRUD funcionar, o SuperAdmin precisaria "atuar como" um estabelecimento.
                // Vamos assumir por enquanto que o frontend do SuperAdmin fornecerá um contexto de estabelecimento.
                // Se nenhum contexto for fornecido, pode-se retornar todas as categorias (cuidado com performance).
                // Para simplificar, se ele não tiver um estabelecimentoId específico no contexto da requisição (via query param talvez)
                // vamos retornar as categorias do primeiro estabelecimento apenas para teste.
                var todosEstabelecimentos = await _context.Estabelecimentos.ToListAsync();
                var todasCategorias = new List<CategoriaVerDto>();
                foreach (var est in todosEstabelecimentos)
                {
                    todasCategorias.AddRange(await _categoriaService.ObterTodasAsync(est.Id));
                }
                categorias = todasCategorias;
                // Em um cenário real, isso precisaria de paginação e filtros.
                // Ou, se o SuperAdmin SÓ gerencia Marcas, ele não deveria ver categorias de lojas diretamente aqui.
            }
            else if (userRole == "AdminMarcaFranquia")
            {
                if (!marcaIdFromToken.HasValue) return BadRequest("Admin de Marca sem MarcaFranquiaId associada.");
                // Listar categorias TEMPLATE desta marca (EstabelecimentoId IS NULL e MarcaFranquiaId corresponde)
                categorias = await _categoriaService.ObterTodasTemplatesDaMarcaAsync(marcaIdFromToken.Value); // MÉTODO NOVO NO SERVICE
            }
            else if (userRole == "AdminLoja")
            {
                if (!estIdFromToken.HasValue) return BadRequest("Admin de Loja sem EstabelecimentoId associado.");
                // Listar categorias DESTA loja (EstabelecimentoId corresponde E MarcaFranquiaId é NULL)
                // E também categorias TEMPLATE da franquia da loja (se for uma franquia)
                categorias = await _categoriaService.ObterTodasDaLojaEMarcaAsync(estIdFromToken.Value); // MÉTODO NOVO NO SERVICE
            }
            else
            {
                return Forbid(); // Role desconhecido
            }
            return Ok(categorias);
        }

        [HttpGet("{id}")] // GET api/categorias/{id}
        public async Task<ActionResult<CategoriaVerDto>> GetCategoria(int id)
        {
            if (!TryGetAuthenticatedUserDetails(out _, out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            CategoriaVerDto? categoria;

            // A lógica aqui precisa garantir que o usuário só acesse categorias que ele tem permissão.
            // O service ObterPorIdAsync já recebe estabelecimentoId.
            // Para AdminMarca, ele só pode ver categorias template da sua marca.
            // Para SuperAdmin, ele pode ver qualquer uma, mas o service precisaria de uma sobrecarga.

            if (userRole == "AdminLoja" && estIdFromToken.HasValue)
            {
                categoria = await _categoriaService.ObterPorIdAsync(id, estIdFromToken.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaIdFromToken.HasValue)
            {
                categoria = await _categoriaService.ObterTemplateDaMarcaPorIdAsync(id, marcaIdFromToken.Value); // MÉTODO NOVO NO SERVICE
            }
            else if (userRole == "SuperAdminSistema")
            {
                // SuperAdmin: Pode ver qualquer categoria. O service precisa de uma forma de buscar sem filtro de est.
                categoria = await _categoriaService.ObterPorIdParaSuperAdminAsync(id); // MÉTODO NOVO NO SERVICE
            }
            else
            {
                return Forbid();
            }

            if (categoria == null)
            {
                return NotFound(new { message = $"Categoria com ID {id} não encontrada ou acesso negado." });
            }
            return Ok(categoria);
        }

        [HttpPost] // POST api/categorias
        public async Task<ActionResult<CategoriaVerDto>> PostCategoria(CategoriaCriarDto categoriaDto)
        {
            if (!TryGetAuthenticatedUserDetails(out _, out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            int targetEstabelecimentoId;
            int? targetMarcaFranquiaId = null;

            if (userRole == "AdminLoja" && estIdFromToken.HasValue)
            {
                targetEstabelecimentoId = estIdFromToken.Value;
                // Categoria de loja não é template de marca
            }
            else if (userRole == "AdminMarcaFranquia" && marcaIdFromToken.HasValue)
            {
                // Categoria template da marca. Não tem EstabelecimentoId direto.
                // O service precisa saber que é para criar um template de marca.
                // Passaremos marcaIdFromToken.Value para o service.
                // O service CriarTemplateAsync receberia (dto, marcaId)
                // Por enquanto, vamos assumir que o service CriarAsync pode lidar com isso
                // se o DTO ou um parâmetro indicar que é um template.
                // A forma mais simples é ter métodos diferentes no service.
                // Vamos simplificar: o service CriarAsync recebe o estId (que pode ser nulo) e o marcaId (que pode ser nulo)
                // e decide como criar.
                // Para esta implementação, o CategoriaCriarDto não tem estId.
                // O service CriarAsync precisa de estabelecimentoId para criar a categoria.
                // Se for AdminMarca, ele está criando um template, então o EstabelecimentoId na Categoria será NULL.
                // E MarcaFranquiaId na Categoria será o marcaIdFromToken.
                // Isso requer uma mudança no ICategoriaService.CriarAsync.
                // Por agora, vamos forçar a lógica do service CriarAsync a usar marcaIdFromToken.
                // O CategoriaService.CriarAsync precisa ser adaptado.
                // Vamos usar o estIdFromToken (que será 0 ou null para AdminMarca) e o service adapta.
                targetEstabelecimentoId = estIdFromToken ?? 0; // Hack temporário se estIdFromToken for null
                targetMarcaFranquiaId = marcaIdFromToken;

                // O correto seria o CategoriaCriarDto ter um campo MarcaFranquiaId
                // e o service usar isso. Ou um endpoint/service diferente para templates.
                // Vamos assumir que o CategoriaService.CriarAsync é inteligente
                // e se estId for 0/null E marcaId for fornecido, ele cria um template de marca.
                // Isso precisa ser implementado no CategoriaService.
                var categoriaTemplateCriada = await _categoriaService.CriarTemplateAsync(categoriaDto, marcaIdFromToken.Value);
                if (categoriaTemplateCriada == null) return BadRequest("Não foi possível criar categoria template.");
                return CreatedAtAction(nameof(GetCategoria), new { id = categoriaTemplateCriada.Id }, categoriaTemplateCriada);

            }
            else if (userRole == "SuperAdminSistema")
            {
                // SuperAdmin precisa especificar para qual estabelecimento ou marca está criando.
                // Isso deveria vir no DTO ou ele deveria estar "atuando como".
                // Por agora, vamos impedir a criação direta pelo SuperAdmin sem contexto.
                return Forbid("SuperAdmin deve especificar um contexto de estabelecimento/marca para criar categorias.");
            }
            else
            {
                return Forbid();
            }

            // Se for AdminLoja:
            var categoriaCriada = await _categoriaService.CriarAsync(categoriaDto, targetEstabelecimentoId);

            if (categoriaCriada == null)
            {
                return BadRequest(new { message = "Não foi possível criar a categoria." });
            }
            return CreatedAtAction(nameof(GetCategoria), new { id = categoriaCriada.Id }, categoriaCriada);
        }

        [HttpPut("{id}")] // PUT api/categorias/{id}
        public async Task<IActionResult> PutCategoria(int id, CategoriaCriarDto categoriaDto)
        {
            if (!TryGetAuthenticatedUserDetails(out _, out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            bool sucesso;
            if (userRole == "AdminLoja" && estIdFromToken.HasValue)
            {
                sucesso = await _categoriaService.AtualizarAsync(id, categoriaDto, estIdFromToken.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaIdFromToken.HasValue)
            {
                // Precisa de um método para atualizar template no service
                sucesso = await _categoriaService.AtualizarTemplateAsync(id, categoriaDto, marcaIdFromToken.Value);
            }
            else if (userRole == "SuperAdminSistema")
            {
                // SuperAdmin precisa de contexto
                return Forbid("SuperAdmin deve especificar um contexto para atualizar.");
            }
            else { return Forbid(); }


            if (!sucesso)
            {
                return NotFound(new { message = $"Categoria com ID {id} não encontrada ou falha na atualização." });
            }
            return NoContent();
        }

        [HttpDelete("{id}")] // DELETE api/categorias/{id}
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            if (!TryGetAuthenticatedUserDetails(out _, out string userRole, out int? estIdFromToken, out int? marcaIdFromToken))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            bool sucesso;
            if (userRole == "AdminLoja" && estIdFromToken.HasValue)
            {
                sucesso = await _categoriaService.DeletarAsync(id, estIdFromToken.Value);
            }
            else if (userRole == "AdminMarcaFranquia" && marcaIdFromToken.HasValue)
            {
                // Precisa de um método para deletar template no service
                sucesso = await _categoriaService.DeletarTemplateAsync(id, marcaIdFromToken.Value);
            }
            else if (userRole == "SuperAdminSistema")
            {
                return Forbid("SuperAdmin deve especificar um contexto para deletar.");
            }
            else { return Forbid(); }


            if (!sucesso)
            {
                return NotFound(new { message = $"Categoria com ID {id} não encontrada ou não pode ser deletada." });
            }
            return NoContent();
        }
    }
}