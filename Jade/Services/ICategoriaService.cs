// Services/ICategoriaService.cs
using Jade.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jade.Services // Seu namespace
{
    public interface ICategoriaService
    {
        // Métodos existentes (para AdminLoja ou quando um EstabelecimentoId específico é conhecido)
        Task<IEnumerable<CategoriaVerDto>> ObterTodasAsync(int estabelecimentoId);
        Task<CategoriaVerDto?> ObterPorIdAsync(int id, int estabelecimentoId);
        Task<CategoriaVerDto?> CriarAsync(CategoriaCriarDto categoriaDto, int estabelecimentoId); // Cria categoria para uma loja
        Task<bool> AtualizarAsync(int id, CategoriaCriarDto categoriaDto, int estabelecimentoId); // Atualiza categoria de uma loja
        Task<bool> DeletarAsync(int id, int estabelecimentoId); // Deleta categoria de uma loja

        // --- NOVOS MÉTODOS PARA FRANQUIAS E ROLES ---

        // Para AdminMarcaFranquia: lida com categorias "template" da marca
        Task<IEnumerable<CategoriaVerDto>> ObterTodasTemplatesDaMarcaAsync(int marcaFranquiaId);
        Task<CategoriaVerDto?> ObterTemplateDaMarcaPorIdAsync(int categoriaTemplateId, int marcaFranquiaId);
        Task<CategoriaVerDto?> CriarTemplateAsync(CategoriaCriarDto categoriaDto, int marcaFranquiaId);
        Task<bool> AtualizarTemplateAsync(int categoriaTemplateId, CategoriaCriarDto categoriaDto, int marcaFranquiaId);
        Task<bool> DeletarTemplateAsync(int categoriaTemplateId, int marcaFranquiaId); // Cuidado com produtos template associados

        // Para AdminLoja de uma franquia: obtém categorias da loja + templates da marca
        Task<IEnumerable<CategoriaVerDto>> ObterTodasDaLojaEMarcaAsync(int estabelecimentoId);
        // (ObterPorIdAsync já pode servir se o ID for único globalmente ou se a lógica de acesso for no controller)

        // Para SuperAdminSistema (pode precisar de mais sobrecargas ou lógica diferente)
        Task<IEnumerable<CategoriaVerDto>> ObterTodasParaSuperAdminAsync(); // Ex: Todas de todos estabelecimentos (com paginação)
        Task<CategoriaVerDto?> ObterPorIdParaSuperAdminAsync(int categoriaId); // Busca sem filtro de estabelecimento
    }
}