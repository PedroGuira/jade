// Services/IProdutoService.cs
using Jade.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jade.Services
{
    public interface IProdutoService
    {
        // Para AdminLoja ou contexto de Estabelecimento específico
        Task<IEnumerable<ProdutoVerDto>> ObterTodosAsync(int estabelecimentoId, int? categoriaId = null, bool apenasAtivosParaCardapio = false);
        Task<ProdutoVerDto?> ObterPorIdAsync(int id, int estabelecimentoId);
        Task<ProdutoVerDto?> CriarAsync(ProdutoCriarDto dto, int estabelecimentoId); // Cria produto para uma loja
        Task<bool> AtualizarAsync(int id, ProdutoCriarDto dto, int estabelecimentoId); // Atualiza produto de uma loja
        Task<bool> DeletarAsync(int id, int estabelecimentoId); // Deleta produto de uma loja

        // Para AdminMarcaFranquia (templates)
        Task<IEnumerable<ProdutoVerDto>> ObterTodosTemplatesDaMarcaAsync(int marcaFranquiaId, int? categoriaTemplateId = null);
        Task<ProdutoVerDto?> ObterTemplateDaMarcaPorIdAsync(int produtoTemplateId, int marcaFranquiaId);
        Task<ProdutoVerDto?> CriarTemplateAsync(ProdutoCriarDto dto, int marcaFranquiaId, int? categoriaTemplateId);
        Task<bool> AtualizarTemplateAsync(int produtoTemplateId, ProdutoCriarDto dto, int marcaFranquiaId, int? categoriaTemplateId);
        Task<bool> DeletarTemplateAsync(int produtoTemplateId, int marcaFranquiaId);

        // Para AdminLoja de uma Franquia (combina templates da marca com itens/configs locais)
        Task<IEnumerable<ProdutoVerDto>> ObterTodosDaLojaEMarcaAsync(int estabelecimentoId, int? categoriaId = null, bool apenasAtivosParaCardapio = false);
        // (ObterPorIdAsync pode já servir, o controller validaria o acesso)

        // Para SuperAdminSistema
        Task<IEnumerable<ProdutoVerDto>> ObterTodosParaSuperAdminAsync(int? categoriaId = null); // Pode precisar de filtros de marca/estabelecimento
        Task<ProdutoVerDto?> ObterPorIdParaSuperAdminAsync(int produtoId);

        Task<IEnumerable<GrupoOpcaoVerDto>> ObterOpcoesDePersonalizacaoAsync(int produtoId, int estabelecimentoId);
    }
}