// Services/GrupoOpcaoService.cs
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
    public class GrupoOpcaoService : IGrupoOpcaoService
    {
        private readonly AppDbContext _context;

        public GrupoOpcaoService(AppDbContext context)
        {
            _context = context;
        }

        // --- MÉTODOS PARA GRUPOS DE OPÇÃO DE LOJA (EstabelecimentoId específico, MarcaFranquiaId NULL) ---
        public async Task<GrupoOpcaoVerDto?> CriarParaLojaAsync(GrupoOpcaoCriarDto dto, int estabelecimentoId)
        {
            if (!await _context.Estabelecimentos.AnyAsync(e => e.Id == estabelecimentoId))
            {
                Debug.WriteLine($"[GrupoOpcaoService.CriarParaLojaAsync] Estabelecimento ID {estabelecimentoId} não encontrado.");
                return null;
            }
            if (dto.MinSelecoes > dto.MaxSelecoes && dto.MaxSelecoes > 0) return null;

            var grupo = new GrupoOpcao
            {
                Nome = dto.Nome,
                Descricao = dto.Descricao,
                MinSelecoes = dto.MinSelecoes,
                MaxSelecoes = dto.MaxSelecoes,
                OrdemExibicao = dto.OrdemExibicao,
                EstabelecimentoId = estabelecimentoId, // Vinculado a esta loja
                MarcaFranquiaId = null // Não é um template de marca
            };
            _context.GruposOpcao.Add(grupo);
            await _context.SaveChangesAsync();
            return MapToVerDto(grupo);
        }

        public async Task<bool> AtualizarDeLojaAsync(int id, GrupoOpcaoCriarDto dto, int estabelecimentoId)
        {
            var grupo = await _context.GruposOpcao.FirstOrDefaultAsync(go => go.Id == id && go.EstabelecimentoId == estabelecimentoId && go.MarcaFranquiaId == null);
            if (grupo == null) return false;
            if (dto.MinSelecoes > dto.MaxSelecoes && dto.MaxSelecoes > 0) return false;

            grupo.Nome = dto.Nome; grupo.Descricao = dto.Descricao; grupo.MinSelecoes = dto.MinSelecoes;
            grupo.MaxSelecoes = dto.MaxSelecoes; grupo.OrdemExibicao = dto.OrdemExibicao;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<GrupoOpcaoVerDto>> ObterTodosDeLojaAsync(int estabelecimentoId)
        {
            var grupos = await _context.GruposOpcao
                .Where(go => go.EstabelecimentoId == estabelecimentoId && go.MarcaFranquiaId == null)
                .Include(go => go.ItensOpcao)
                .OrderBy(go => go.OrdemExibicao).ThenBy(go => go.Nome)
                .ToListAsync();
            return grupos.Select(MapToVerDto).ToList();
        }

        public async Task<GrupoOpcaoVerDto?> ObterDeLojaPorIdAsync(int id, int estabelecimentoId)
        {
            var grupo = await _context.GruposOpcao
                .Include(go => go.ItensOpcao)
                .FirstOrDefaultAsync(go => go.Id == id && go.EstabelecimentoId == estabelecimentoId && go.MarcaFranquiaId == null);
            return grupo == null ? null : MapToVerDto(grupo);
        }

        public async Task<bool> DeletarDeLojaAsync(int id, int estabelecimentoId)
        {
            var grupo = await _context.GruposOpcao
                .Include(go => go.CategoriasAssociadas) // Para validar se está em uso
                .FirstOrDefaultAsync(go => go.Id == id && go.EstabelecimentoId == estabelecimentoId && go.MarcaFranquiaId == null);
            if (grupo == null) return false;
            if (grupo.CategoriasAssociadas.Any())
            {
                Debug.WriteLine($"Grupo de Opção de Loja ID {id} ainda está associado a categorias.");
                return false; // Não deletar se estiver associado
            }
            _context.GruposOpcao.Remove(grupo); // ItensOpcao devem ser Cascade
            await _context.SaveChangesAsync();
            return true;
        }

        // --- MÉTODOS PARA GRUPOS DE OPÇÃO TEMPLATE DE MARCA ---
        public async Task<GrupoOpcaoVerDto?> CriarTemplateParaMarcaAsync(GrupoOpcaoCriarDto dto, int marcaFranquiaId)
        {
            if (!await _context.MarcasFranquia.AnyAsync(mf => mf.Id == marcaFranquiaId))
            {
                Debug.WriteLine($"[GrupoOpcaoService.CriarTemplateParaMarcaAsync] MarcaFranquia ID {marcaFranquiaId} não encontrada.");
                return null;
            }
            if (dto.MinSelecoes > dto.MaxSelecoes && dto.MaxSelecoes > 0) return null;

            var grupoTemplate = new GrupoOpcao
            {
                Nome = dto.Nome,
                Descricao = dto.Descricao,
                MinSelecoes = dto.MinSelecoes,
                MaxSelecoes = dto.MaxSelecoes,
                OrdemExibicao = dto.OrdemExibicao,
                MarcaFranquiaId = marcaFranquiaId, // Vinculado a esta marca
                EstabelecimentoId = null // Não é de uma loja específica
            };
            _context.GruposOpcao.Add(grupoTemplate);
            await _context.SaveChangesAsync();
            return MapToVerDto(grupoTemplate);
        }

        public async Task<bool> AtualizarTemplateDaMarcaAsync(int grupoOpcaoTemplateId, GrupoOpcaoCriarDto dto, int marcaFranquiaId)
        {
            var grupoTemplate = await _context.GruposOpcao.FirstOrDefaultAsync(go => go.Id == grupoOpcaoTemplateId && go.MarcaFranquiaId == marcaFranquiaId && go.EstabelecimentoId == null);
            if (grupoTemplate == null) return false;
            if (dto.MinSelecoes > dto.MaxSelecoes && dto.MaxSelecoes > 0) return false;

            grupoTemplate.Nome = dto.Nome; /* ... etc ... */
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<GrupoOpcaoVerDto>> ObterTodosTemplatesDaMarcaAsync(int marcaFranquiaId)
        {
            var grupos = await _context.GruposOpcao
                .Where(go => go.MarcaFranquiaId == marcaFranquiaId && go.EstabelecimentoId == null)
                .Include(go => go.ItensOpcao)
                .OrderBy(go => go.OrdemExibicao).ThenBy(go => go.Nome)
                .ToListAsync();
            return grupos.Select(MapToVerDto).ToList();
        }

        public async Task<GrupoOpcaoVerDto?> ObterTemplateDaMarcaPorIdAsync(int grupoOpcaoTemplateId, int marcaFranquiaId)
        {
            var grupo = await _context.GruposOpcao
                .Include(go => go.ItensOpcao)
                .FirstOrDefaultAsync(go => go.Id == grupoOpcaoTemplateId && go.MarcaFranquiaId == marcaFranquiaId && go.EstabelecimentoId == null);
            return grupo == null ? null : MapToVerDto(grupo);
        }

        public async Task<bool> DeletarTemplateDaMarcaAsync(int grupoOpcaoTemplateId, int marcaFranquiaId)
        {
            var grupoTemplate = await _context.GruposOpcao
                .Include(go => go.CategoriasAssociadas) // Para validar se está em uso por CategoriaTemplates
                                                        // TODO: Precisaria validar também se está em uso por alguma LojaItemConfig (indiretamente via CategoriaLoja que usa CategoriaTemplate)
                .FirstOrDefaultAsync(go => go.Id == grupoOpcaoTemplateId && go.MarcaFranquiaId == marcaFranquiaId && go.EstabelecimentoId == null);
            if (grupoTemplate == null) return false;
            if (grupoTemplate.CategoriasAssociadas.Any())
            {
                Debug.WriteLine($"Grupo de Opção Template ID {grupoOpcaoTemplateId} ainda está associado a categorias template.");
                return false;
            }
            _context.GruposOpcao.Remove(grupoTemplate);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- MÉTODO COMBINADO PARA ADMINLOJA DE FRANQUIA ---
        public async Task<IEnumerable<GrupoOpcaoVerDto>> ObterTodosParaLojaDeFranquiaAsync(int estabelecimentoId)
        {
            var loja = await _context.Estabelecimentos
                .AsNoTracking() // Leitura apenas
                .FirstOrDefaultAsync(e => e.Id == estabelecimentoId);

            if (loja == null)
            {
                Debug.WriteLine($"[GrupoOpcaoService.ObterTodosParaLojaDeFranquiaAsync] Loja ID {estabelecimentoId} não encontrada.");
                return new List<GrupoOpcaoVerDto>();
            }

            var resultadoQuery = new List<GrupoOpcao>();

            // 1. Grupos locais da loja
            var gruposLocais = await _context.GruposOpcao
                .Where(go => go.EstabelecimentoId == estabelecimentoId && go.MarcaFranquiaId == null)
                .Include(go => go.ItensOpcao) // Para QuantidadeItens
                .ToListAsync();
            resultadoQuery.AddRange(gruposLocais);
            Debug.WriteLine($"[GrupoOpcaoService.ObterTodosParaLojaDeFranquiaAsync] Encontrados {gruposLocais.Count} grupos locais para EstID {estabelecimentoId}.");


            // 2. Grupos template da marca da loja (se a loja for uma franquia)
            if (loja.MarcaFranquiaId.HasValue)
            {
                var gruposTemplate = await _context.GruposOpcao
                    .Where(go => go.MarcaFranquiaId == loja.MarcaFranquiaId.Value && go.EstabelecimentoId == null)
                    .Include(go => go.ItensOpcao)
                    .ToListAsync();
                resultadoQuery.AddRange(gruposTemplate);
                Debug.WriteLine($"[GrupoOpcaoService.ObterTodosParaLojaDeFranquiaAsync] Encontrados {gruposTemplate.Count} grupos template para MarcaID {loja.MarcaFranquiaId.Value}.");
            }

            // Mapeia e ordena, garantindo que IsTemplateDaFranquia seja setado corretamente
            return resultadoQuery
                .Select(MapToVerDto) // MapToVerDto já seta IsTemplateDaFranquia
                .OrderBy(g => g.IsTemplateDaFranquia) // Templates por último, ou como preferir
                .ThenBy(g => g.OrdemExibicao)
                .ThenBy(g => g.Nome)
                .ToList();
        }


        // --- MÉTODOS PARA SUPERADMIN ---
        public async Task<IEnumerable<GrupoOpcaoVerDto>> ObterTodosParaSuperAdminAsync()
        {
            var grupos = await _context.GruposOpcao.Include(go => go.ItensOpcao)
                                    .OrderBy(go => go.MarcaFranquiaId).ThenBy(go => go.EstabelecimentoId)
                                    .ThenBy(go => go.OrdemExibicao).ThenBy(go => go.Nome)
                                    .ToListAsync();
            return grupos.Select(MapToVerDto).ToList();
        }
        public async Task<GrupoOpcaoVerDto?> ObterPorIdParaSuperAdminAsync(int grupoOpcaoId)
        {
            var grupo = await _context.GruposOpcao.Include(go => go.ItensOpcao).FirstOrDefaultAsync(go => go.Id == grupoOpcaoId);
            return grupo == null ? null : MapToVerDto(grupo);
        }

        // Helper para outros services/controllers
        public async Task<GrupoOpcao?> GetGrupoOpcaoInternoVerificadoAsync(int grupoOpcaoId, string userRole, int? userEstabelecimentoId, int? userMarcaFranquiaId)
        {
            // Se AdminLoja
            if (userRole == "AdminLoja" && userEstabelecimentoId.HasValue)
            {
                var loja = await _context.Estabelecimentos.FindAsync(userEstabelecimentoId.Value);
                if (loja == null) return null;

                // Verifica se o grupoOpcaoId é um grupo local desta loja OU um template da marca desta loja
                return await _context.GruposOpcao.FirstOrDefaultAsync(go => go.Id == grupoOpcaoId &&
                    (go.EstabelecimentoId == userEstabelecimentoId.Value || // É um grupo local da loja
                     (loja.MarcaFranquiaId.HasValue && go.MarcaFranquiaId == loja.MarcaFranquiaId.Value && go.EstabelecimentoId == null) // É um template da marca da loja
                    ));
            }
            // Se AdminMarcaFranquia
            else if (userRole == "AdminMarcaFranquia" && userMarcaFranquiaId.HasValue)
            {
                // Só pode acessar templates da sua marca
                return await _context.GruposOpcao.FirstOrDefaultAsync(go => go.Id == grupoOpcaoId && go.MarcaFranquiaId == userMarcaFranquiaId.Value && go.EstabelecimentoId == null);
            }
            // Se SuperAdminSistema
            else if (userRole == "SuperAdminSistema")
            {
                return await _context.GruposOpcao.FindAsync(grupoOpcaoId); // Pode acessar qualquer um
            }
            return null; // Role desconhecido ou sem contexto
        }

        public async Task<GrupoOpcaoVerDto?> ObterTemplateDaMarcaDaSuaFranquiaAsync(int grupoOpcaoTemplateId, int lojaEstabelecimentoId)
        {
            // 1. Buscar a loja para obter o ID da sua marca/franquia
            var loja = await _context.Estabelecimentos
                .AsNoTracking() // Não precisamos rastrear para esta operação de leitura
                .FirstOrDefaultAsync(e => e.Id == lojaEstabelecimentoId);

            if (loja == null || !loja.MarcaFranquiaId.HasValue)
            {
                // Loja não existe ou não pertence a uma franquia
                Debug.WriteLine($"[GrupoOpcaoService.ObterTemplateDaMarcaDaSuaFranquiaAsync] Loja ID {lojaEstabelecimentoId} não encontrada ou não é franquia.");
                return null;
            }

            // 2. Buscar o GrupoOpcao template que tem o ID fornecido E pertence à marca da loja
            var grupoTemplate = await _context.GruposOpcao
                .Include(go => go.ItensOpcao) // Para popular QuantidadeItens e se o DTO retornar itens
                .AsNoTracking()
                .FirstOrDefaultAsync(go =>
                    go.Id == grupoOpcaoTemplateId &&                  // ID do grupo template corresponde
                    go.MarcaFranquiaId == loja.MarcaFranquiaId.Value && // Pertence à mesma marca da loja
                    go.EstabelecimentoId == null);                    // É de fato um template (não específico de loja)

            if (grupoTemplate == null)
            {
                Debug.WriteLine($"[GrupoOpcaoService.ObterTemplateDaMarcaDaSuaFranquiaAsync] Grupo Template ID {grupoOpcaoTemplateId} não encontrado para Marca ID {loja.MarcaFranquiaId.Value}.");
                return null;
            }

            return MapToVerDto(grupoTemplate);
        }



        private GrupoOpcaoVerDto MapToVerDto(GrupoOpcao grupo)
        {
            return new GrupoOpcaoVerDto
            {
                Id = grupo.Id,
                Nome = grupo.Nome,
                Descricao = grupo.Descricao,
                MinSelecoes = grupo.MinSelecoes,
                MaxSelecoes = grupo.MaxSelecoes,
                OrdemExibicao = grupo.OrdemExibicao,
                EstabelecimentoId = grupo.EstabelecimentoId,
                MarcaFranquiaId = grupo.MarcaFranquiaId,
                IsTemplateDaFranquia = grupo.MarcaFranquiaId.HasValue && !grupo.EstabelecimentoId.HasValue,
                QuantidadeItens = grupo.ItensOpcao?.Count ?? 0
            };
        }

        public async Task<GrupoOpcaoVerDto?> MapGrupoOpcaoParaClienteAsync(GrupoOpcao grupoOriginal, int lojaEstabelecimentoId, bool apenasItensAtivosNoGrupo)
        {
            if (grupoOriginal == null) return null;

            var loja = await _context.Estabelecimentos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == lojaEstabelecimentoId);
            if (loja == null) return null;

            // ... (validação se grupoOriginal é acessível pela loja, como antes) ...
            bool grupoAcessivelPelaLoja = (grupoOriginal.EstabelecimentoId == lojaEstabelecimentoId && grupoOriginal.MarcaFranquiaId == null) ||
                                         (grupoOriginal.MarcaFranquiaId == loja.MarcaFranquiaId && grupoOriginal.EstabelecimentoId == null);
            if (!grupoAcessivelPelaLoja) return null;

            var configGrupoLoja = await _context.LojaItemConfiguracoes.AsNoTracking()
                .FirstOrDefaultAsync(lic => lic.EstabelecimentoId == lojaEstabelecimentoId &&
                                            lic.TipoItem == TipoItemConfiguravel.GrupoOpcao &&
                                            lic.ItemOriginalId == grupoOriginal.Id);

            if (apenasItensAtivosNoGrupo && configGrupoLoja != null && !configGrupoLoja.AtivoNaLoja) return null;

            // Contar os itens efetivamente disponíveis para o cliente
            int quantidadeItensEfetivos = 0;
            var itensOriginaisDoGrupo = grupoOriginal.ItensOpcao ?? await _context.ItensOpcao
                                                                        .Where(io => io.GrupoOpcaoId == grupoOriginal.Id)
                                                                        .AsNoTracking() // Importante para evitar rastreamento desnecessário
                                                                        .ToListAsync();

            foreach (var itemOriginal in itensOriginaisDoGrupo)
            {
                bool mostrarEsteItem = true;
                if (apenasItensAtivosNoGrupo) // Se for para o cardápio do cliente, aplicamos filtros de disponibilidade
                {
                    if (!itemOriginal.Disponivel) mostrarEsteItem = false; // Base do template

                    var configItemLoja = await _context.LojaItemConfiguracoes.AsNoTracking()
                        .FirstOrDefaultAsync(lic => lic.EstabelecimentoId == lojaEstabelecimentoId &&
                                                    lic.TipoItem == TipoItemConfiguravel.ItemOpcao &&
                                                    lic.ItemOriginalId == itemOriginal.Id);

                    if (configItemLoja != null)
                    {
                        if (!configItemLoja.AtivoNaLoja) mostrarEsteItem = false;
                        if (configItemLoja.DisponivelLocalmente.HasValue && !configItemLoja.DisponivelLocalmente.Value) mostrarEsteItem = false;
                    }
                }
                if (mostrarEsteItem)
                {
                    quantidadeItensEfetivos++;
                }
            }

            // Só retorna o grupo se ele tiver itens (após filtros) ou se não for para filtrar apenas ativos
            if (apenasItensAtivosNoGrupo && quantidadeItensEfetivos == 0) return null;

            return new GrupoOpcaoVerDto
            {
                Id = grupoOriginal.Id,
                Nome = configGrupoLoja?.NomeLocal ?? grupoOriginal.Nome,
                Descricao = grupoOriginal.Descricao,
                MinSelecoes = grupoOriginal.MinSelecoes,
                MaxSelecoes = grupoOriginal.MaxSelecoes,
                OrdemExibicao = configGrupoLoja?.OrdemExibicaoLocal ?? grupoOriginal.OrdemExibicao,
                EstabelecimentoId = lojaEstabelecimentoId, // Contexto da loja que está visualizando
                MarcaFranquiaId = grupoOriginal.MarcaFranquiaId, // Para saber se o grupo original é template
                IsTemplateDaFranquia = grupoOriginal.MarcaFranquiaId.HasValue && grupoOriginal.EstabelecimentoId == null,
                QuantidadeItens = quantidadeItensEfetivos
                // Sem a propriedade 'Itens' aqui
            };
        }
    }
}