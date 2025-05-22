// Services/ItemOpcaoService.cs
using Jade.Data;
using Jade.Dtos;
using Jade.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Jade.Services
{
    public class ItemOpcaoService : IItemOpcaoService
    {
        private readonly AppDbContext _context;
        private readonly IGrupoOpcaoService _grupoOpcaoService; // Para validar e obter contexto do grupo pai

        public ItemOpcaoService(AppDbContext context, IGrupoOpcaoService grupoOpcaoService)
        {
            _context = context;
            _grupoOpcaoService = grupoOpcaoService;
        }

        // Helper para obter o GrupoOpcao pai validado e seu contexto
        private async Task<GrupoOpcao?> GetValidParentGrupoOpcaoForOperation(int grupoOpcaoId, string userRole, int? userEstabelecimentoId, int? userMarcaFranquiaId)
        {
            return await _grupoOpcaoService.GetGrupoOpcaoInternoVerificadoAsync(grupoOpcaoId, userRole, userEstabelecimentoId, userMarcaFranquiaId);
        }

        public async Task<ItemOpcaoVerDto?> CriarAsync(ItemOpcaoCriarDto dto, int grupoOpcaoId,
                                                      int? userEstabelecimentoId, string userRole, int? userMarcaFranquiaId)
        {
            var grupoPai = await GetValidParentGrupoOpcaoForOperation(grupoOpcaoId, userRole, userEstabelecimentoId, userMarcaFranquiaId);
            if (grupoPai == null)
            {
                Debug.WriteLine($"[ItemOpcaoService.CriarAsync] Grupo de Opção pai ID {grupoOpcaoId} inválido ou acesso negado.");
                return null;
            }

            var itemOpcao = new ItemOpcao
            {
                Nome = dto.Nome,
                PrecoAdicional = dto.PrecoAdicional,
                Disponivel = dto.Disponivel,
                OrdemExibicao = dto.OrdemExibicao,
                GrupoOpcaoId = grupoOpcaoId,
                // O EstabelecimentoId do ItemOpcao é o mesmo do seu GrupoOpcao pai, se o grupo for de loja.
                // Se o GrupoOpcao for template (MarcaFranquiaId != null), então ItemOpcao.EstabelecimentoId será null.
                EstabelecimentoId = grupoPai.EstabelecimentoId
            };

            _context.ItensOpcao.Add(itemOpcao);
            await _context.SaveChangesAsync();

            return MapToVerDto(itemOpcao, grupoPai);
        }

        public async Task<IEnumerable<ItemOpcaoVerDto>> ObterTodosPorGrupoAsync(int grupoOpcaoId,
                                                                                int? userEstabelecimentoId, string userRole, int? userMarcaFranquiaId)
        {
            var grupoPai = await GetValidParentGrupoOpcaoForOperation(grupoOpcaoId, userRole, userEstabelecimentoId, userMarcaFranquiaId);
            if (grupoPai == null)
            {
                Debug.WriteLine($"[ItemOpcaoService.ObterTodosPorGrupoAsync] Grupo de Opção pai ID {grupoOpcaoId} inválido ou acesso negado.");
                return new List<ItemOpcaoVerDto>();
            }

            var itens = await _context.ItensOpcao
                .Where(io => io.GrupoOpcaoId == grupoOpcaoId)
                // A validação de acesso ao grupoPai já garante o contexto correto de estabelecimento/marca
                .OrderBy(io => io.OrdemExibicao).ThenBy(io => io.Nome)
                .ToListAsync();

            return itens.Select(item => MapToVerDto(item, grupoPai)).ToList();
        }

        public async Task<ItemOpcaoVerDto?> ObterPorIdAsync(int itemId, int grupoOpcaoId,
                                                           int? userEstabelecimentoId, string userRole, int? userMarcaFranquiaId)
        {
            var grupoPai = await GetValidParentGrupoOpcaoForOperation(grupoOpcaoId, userRole, userEstabelecimentoId, userMarcaFranquiaId);
            if (grupoPai == null) return null;

            var itemOpcao = await _context.ItensOpcao
                .FirstOrDefaultAsync(io => io.Id == itemId && io.GrupoOpcaoId == grupoOpcaoId);

            return itemOpcao == null ? null : MapToVerDto(itemOpcao, grupoPai);
        }

        public async Task<bool> AtualizarAsync(int itemId, ItemOpcaoCriarDto dto, int grupoOpcaoId,
                                               int? userEstabelecimentoId, string userRole, int? userMarcaFranquiaId)
        {
            var grupoPai = await GetValidParentGrupoOpcaoForOperation(grupoOpcaoId, userRole, userEstabelecimentoId, userMarcaFranquiaId);
            if (grupoPai == null) return false;

            var itemOpcao = await _context.ItensOpcao
                .FirstOrDefaultAsync(io => io.Id == itemId && io.GrupoOpcaoId == grupoOpcaoId);

            if (itemOpcao == null) return false;
            // Valida se o item realmente pertence ao contexto do grupo pai (se o grupo pai for de loja, o item deve ser da mesma loja)
            if (grupoPai.EstabelecimentoId.HasValue && itemOpcao.EstabelecimentoId != grupoPai.EstabelecimentoId) return false;
            if (grupoPai.MarcaFranquiaId.HasValue && itemOpcao.EstabelecimentoId != null) return false; // Item de template não pode ter EstId

            itemOpcao.Nome = dto.Nome;
            itemOpcao.PrecoAdicional = dto.PrecoAdicional;
            itemOpcao.Disponivel = dto.Disponivel;
            itemOpcao.OrdemExibicao = dto.OrdemExibicao;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletarAsync(int itemId, int grupoOpcaoId,
                                             int? userEstabelecimentoId, string userRole, int? userMarcaFranquiaId)
        {
            var grupoPai = await GetValidParentGrupoOpcaoForOperation(grupoOpcaoId, userRole, userEstabelecimentoId, userMarcaFranquiaId);
            if (grupoPai == null) return false;

            var itemOpcao = await _context.ItensOpcao
                .FirstOrDefaultAsync(io => io.Id == itemId && io.GrupoOpcaoId == grupoOpcaoId);

            if (itemOpcao == null) return false;
            if (grupoPai.EstabelecimentoId.HasValue && itemOpcao.EstabelecimentoId != grupoPai.EstabelecimentoId) return false;
            if (grupoPai.MarcaFranquiaId.HasValue && itemOpcao.EstabelecimentoId != null) return false;

            // TODO: Validar se este ItemOpcao (se for template) está em uso por alguma LojaItemConfig antes de deletar.
            _context.ItensOpcao.Remove(itemOpcao);
            await _context.SaveChangesAsync();
            return true;
        }

        private ItemOpcaoVerDto MapToVerDto(ItemOpcao item, GrupoOpcao grupoPaiContexto)
        {
            return new ItemOpcaoVerDto
            {
                Id = item.Id,
                Nome = item.Nome,
                PrecoAdicional = item.PrecoAdicional,
                Disponivel = item.Disponivel,
                OrdemExibicao = item.OrdemExibicao,
                GrupoOpcaoId = item.GrupoOpcaoId,
                EstabelecimentoId = grupoPaiContexto.EstabelecimentoId, // Herda do grupo pai
                MarcaFranquiaId = grupoPaiContexto.MarcaFranquiaId,     // Herda do grupo pai
                IsTemplateDaFranquia = grupoPaiContexto.MarcaFranquiaId.HasValue && !grupoPaiContexto.EstabelecimentoId.HasValue
            };
        }
    }
}