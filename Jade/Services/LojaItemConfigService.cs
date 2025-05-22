// Services/LojaItemConfigService.cs
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
    public class LojaItemConfigService : ILojaItemConfigService
    {
        private readonly AppDbContext _context;

        public LojaItemConfigService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LojaItemConfigVerDto?> CriarOuAtualizarConfigAsync(LojaItemConfigCriarAtualizarDto dto, int estabelecimentoId)
        {
            var loja = await _context.Estabelecimentos.FindAsync(estabelecimentoId); // FindAsync é do EF Core
            if (loja == null)
            {
                Debug.WriteLine($"[LojaItemConfigService] Estabelecimento ID {estabelecimentoId} não encontrado.");
                return null;
            }

            if (loja.MarcaFranquiaId.HasValue) // Só prossegue se a loja for de franquia
            {
                bool itemTemplateValido = false;
                switch (dto.TipoItem)
                {
                    case TipoItemConfiguravel.Produto:
                        itemTemplateValido = await _context.Produtos.AnyAsync(p => p.Id == dto.ItemOriginalId && p.MarcaFranquiaId == loja.MarcaFranquiaId.Value && p.EstabelecimentoId == null);
                        break;
                    case TipoItemConfiguravel.Categoria:
                        itemTemplateValido = await _context.Categorias.AnyAsync(c => c.Id == dto.ItemOriginalId && c.MarcaFranquiaId == loja.MarcaFranquiaId.Value && c.EstabelecimentoId == null);
                        break;
                    case TipoItemConfiguravel.GrupoOpcao:
                        itemTemplateValido = await _context.GruposOpcao.AnyAsync(go => go.Id == dto.ItemOriginalId && go.MarcaFranquiaId == loja.MarcaFranquiaId.Value && go.EstabelecimentoId == null);
                        break;
                    case TipoItemConfiguravel.ItemOpcao:
                        var itemOpcao = await _context.ItensOpcao
                                            .Include(io => io.GrupoOpcao) // Precisa incluir o GrupoOpcao para checar a MarcaFranquiaId do grupo
                                            .FirstOrDefaultAsync(io => io.Id == dto.ItemOriginalId && io.EstabelecimentoId == null); // Checa se o item é template
                        itemTemplateValido = itemOpcao != null && itemOpcao.GrupoOpcao?.MarcaFranquiaId == loja.MarcaFranquiaId.Value;
                        break;
                }
                if (!itemTemplateValido)
                {
                    Debug.WriteLine($"[LojaItemConfigService] Item Original ID {dto.ItemOriginalId} do tipo {dto.TipoItem} não é um template válido para a marca da loja ID {estabelecimentoId}.");
                    return null;
                }
            }
            else
            {
                Debug.WriteLine($"[LojaItemConfigService] Loja ID {estabelecimentoId} não é uma franquia, não pode configurar itens de franquia.");
                return null;
            }

            // ... resto da lógica de CriarOuAtualizarConfigAsync como antes ...
            var configExistente = await _context.LojaItemConfiguracoes
                .FirstOrDefaultAsync(lic => lic.EstabelecimentoId == estabelecimentoId &&
                                            lic.ItemOriginalId == dto.ItemOriginalId &&
                                            lic.TipoItem == dto.TipoItem);

            if (configExistente != null)
            {
                configExistente.PrecoLocal = dto.PrecoLocal;
                configExistente.DisponivelLocalmente = dto.DisponivelLocalmente;
                configExistente.AtivoNaLoja = dto.AtivoNaLoja;
                configExistente.NomeLocal = dto.NomeLocal;
                configExistente.OrdemExibicaoLocal = dto.OrdemExibicaoLocal;
            }
            else
            {
                configExistente = new LojaItemConfig
                {
                    EstabelecimentoId = estabelecimentoId,
                    TipoItem = dto.TipoItem,
                    ItemOriginalId = dto.ItemOriginalId,
                    PrecoLocal = dto.PrecoLocal,
                    DisponivelLocalmente = dto.DisponivelLocalmente,
                    AtivoNaLoja = dto.AtivoNaLoja,
                    NomeLocal = dto.NomeLocal,
                    OrdemExibicaoLocal = dto.OrdemExibicaoLocal
                };
                _context.LojaItemConfiguracoes.Add(configExistente);
            }

            await _context.SaveChangesAsync();
            return await MapToVerDto(configExistente); // Usa o configExistente que é a entidade salva/atualizada
        }

        public async Task<LojaItemConfigVerDto?> ObterConfigAsync(int estabelecimentoId, int itemOriginalId, TipoItemConfiguravel tipoItem)
        {
            var config = await _context.LojaItemConfiguracoes
                .FirstOrDefaultAsync(lic => lic.EstabelecimentoId == estabelecimentoId &&
                                            lic.ItemOriginalId == itemOriginalId &&
                                            lic.TipoItem == tipoItem);
            if (config == null) return null;
            return await MapToVerDto(config);
        }

        public async Task<IEnumerable<LojaItemConfigVerDto>> ObterTodasConfiguracoesDaLojaAsync(int estabelecimentoId, TipoItemConfiguravel? tipoItemFiltro = null)
        {
            var query = _context.LojaItemConfiguracoes
                .Where(lic => lic.EstabelecimentoId == estabelecimentoId);

            if (tipoItemFiltro.HasValue)
            {
                query = query.Where(lic => lic.TipoItem == tipoItemFiltro.Value);
            }

            var configs = await query.ToListAsync();
            // Mapear para VerDto (pode ser uma lista de promessas se MapToVerDto for async)
            var dtos = new List<LojaItemConfigVerDto>();
            foreach (var config in configs)
            {
                var dto = await MapToVerDto(config);
                if (dto != null) dtos.Add(dto);
            }
            return dtos;
        }

        public async Task<bool> DeletarConfigAsync(int configId, int estabelecimentoId)
        {
            var config = await _context.LojaItemConfiguracoes
                .FirstOrDefaultAsync(lic => lic.Id == configId && lic.EstabelecimentoId == estabelecimentoId);
            if (config == null) return false;

            _context.LojaItemConfiguracoes.Remove(config);
            await _context.SaveChangesAsync();
            return true;
        }

        // Helper para buscar dados do item original e mapear
        private async Task<LojaItemConfigVerDto?> MapToVerDto(LojaItemConfig config)
        {
            string nomeOriginal = "Item Template Não Encontrado";
            decimal? precoOriginalSugerido = null;

            switch (config.TipoItem)
            {
                case TipoItemConfiguravel.Produto:
                    var produtoTemplate = await _context.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == config.ItemOriginalId && p.MarcaFranquiaId != null && p.EstabelecimentoId == null);
                    if (produtoTemplate != null) { nomeOriginal = produtoTemplate.Nome; precoOriginalSugerido = produtoTemplate.Preco; }
                    break;
                case TipoItemConfiguravel.Categoria:
                    var categoriaTemplate = await _context.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == config.ItemOriginalId && c.MarcaFranquiaId != null && c.EstabelecimentoId == null);
                    if (categoriaTemplate != null) { nomeOriginal = categoriaTemplate.Nome; }
                    break;
                // Adicionar casos para GrupoOpcao e ItemOpcao
                case TipoItemConfiguravel.GrupoOpcao:
                    var grupoTemplate = await _context.GruposOpcao.AsNoTracking().FirstOrDefaultAsync(go => go.Id == config.ItemOriginalId && go.MarcaFranquiaId != null && go.EstabelecimentoId == null);
                    if (grupoTemplate != null) { nomeOriginal = grupoTemplate.Nome; }
                    break;
                case TipoItemConfiguravel.ItemOpcao:
                    var itemOpcaoTemplate = await _context.ItensOpcao.AsNoTracking().FirstOrDefaultAsync(io => io.Id == config.ItemOriginalId && io.GrupoOpcao.MarcaFranquiaId != null && io.EstabelecimentoId == null);
                    if (itemOpcaoTemplate != null) { nomeOriginal = itemOpcaoTemplate.Nome; precoOriginalSugerido = itemOpcaoTemplate.PrecoAdicional; }
                    break;
            }

            return new LojaItemConfigVerDto
            {
                Id = config.Id,
                EstabelecimentoId = config.EstabelecimentoId,
                TipoItem = config.TipoItem,
                ItemOriginalId = config.ItemOriginalId,
                NomeOriginal = nomeOriginal,
                PrecoOriginalSugerido = precoOriginalSugerido,
                PrecoLocal = config.PrecoLocal,
                DisponivelLocalmente = config.DisponivelLocalmente,
                AtivoNaLoja = config.AtivoNaLoja,
                NomeLocal = config.NomeLocal,
                OrdemExibicaoLocal = config.OrdemExibicaoLocal
            };
        }
    }
}