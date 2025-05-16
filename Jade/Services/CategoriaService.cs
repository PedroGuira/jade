// Services/CategoriaService.cs
using Jade.Data;
using Jade.Dtos;
using Jade.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Jade.Services // Seu namespace
{
    public class CategoriaService : ICategoriaService
    {
        private readonly AppDbContext _context;

        public CategoriaService(AppDbContext context)
        {
            _context = context;
        }

        // --- MÉTODOS PARA CATEGORIAS DE LOJA (EstabelecimentoId específico) ---

        public async Task<CategoriaVerDto?> CriarAsync(CategoriaCriarDto categoriaDto, int estabelecimentoId)
        {
            var estabelecimento = await _context.Estabelecimentos.FindAsync(estabelecimentoId);
            if (estabelecimento == null)
            {
                Debug.WriteLine($"[CategoriaService.CriarAsync] Estabelecimento ID {estabelecimentoId} não encontrado.");
                return null;
            }

            var categoria = new Categoria
            {
                Nome = categoriaDto.Nome,
                OrdemExibicao = categoriaDto.OrdemExibicao,
                EstabelecimentoId = estabelecimentoId, // Vinculado a esta loja
                MarcaFranquiaId = null // Não é um template de marca
            };

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            await ProcessarAssociacoesGrupoOpcao(categoria, categoriaDto.GrupoOpcaoIds, estabelecimentoId, null);

            return await ObterPorIdAsync(categoria.Id, estabelecimentoId);
        }

        public async Task<bool> AtualizarAsync(int id, CategoriaCriarDto categoriaDto, int estabelecimentoId)
        {
            var categoriaExistente = await _context.Categorias
                .Include(c => c.GruposDeOpcaoAssociados)
                .FirstOrDefaultAsync(c => c.Id == id && c.EstabelecimentoId == estabelecimentoId && c.MarcaFranquiaId == null); // Só atualiza categorias de loja

            if (categoriaExistente == null) return false;

            categoriaExistente.Nome = categoriaDto.Nome;
            categoriaExistente.OrdemExibicao = categoriaDto.OrdemExibicao;

            await ProcessarAssociacoesGrupoOpcao(categoriaExistente, categoriaDto.GrupoOpcaoIds, estabelecimentoId, null);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<CategoriaVerDto>> ObterTodasAsync(int estabelecimentoId)
        {
            // Este método agora retorna apenas categorias DA LOJA (não templates de franquia)
            var categorias = await _context.Categorias
                .Where(c => c.EstabelecimentoId == estabelecimentoId && c.MarcaFranquiaId == null) // Apenas da loja
                .Include(c => c.GruposDeOpcaoAssociados).ThenInclude(cgo => cgo.GrupoOpcao).ThenInclude(go => go.ItensOpcao)
                .OrderBy(c => c.OrdemExibicao).ThenBy(c => c.Nome)
                .ToListAsync();
            return categorias.Select(MapToVerDto).ToList();
        }

        public async Task<CategoriaVerDto?> ObterPorIdAsync(int id, int estabelecimentoId)
        {
            var categoria = await _context.Categorias
                .Include(c => c.GruposDeOpcaoAssociados).ThenInclude(cgo => cgo.GrupoOpcao).ThenInclude(go => go.ItensOpcao)
                .FirstOrDefaultAsync(c => c.Id == id && c.EstabelecimentoId == estabelecimentoId && c.MarcaFranquiaId == null); // Apenas da loja
            return categoria == null ? null : MapToVerDto(categoria);
        }

        public async Task<bool> DeletarAsync(int id, int estabelecimentoId)
        {
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id && c.EstabelecimentoId == estabelecimentoId && c.MarcaFranquiaId == null); // Só deleta da loja
            if (categoria == null) return false;
            // ON DELETE CASCADE nas FKs de CategoriaGrupoOpcao e Produto.CategoriaId (SetNull) cuidam das dependências
            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- MÉTODOS PARA CATEGORIAS TEMPLATE DE MARCA/FRANQUIA ---

        public async Task<CategoriaVerDto?> CriarTemplateAsync(CategoriaCriarDto categoriaDto, int marcaFranquiaId)
        {
            var marca = await _context.MarcasFranquia.FindAsync(marcaFranquiaId);
            if (marca == null)
            {
                Debug.WriteLine($"[CategoriaService.CriarTemplateAsync] MarcaFranquia ID {marcaFranquiaId} não encontrada.");
                return null;
            }

            var categoriaTemplate = new Categoria
            {
                Nome = categoriaDto.Nome,
                OrdemExibicao = categoriaDto.OrdemExibicao,
                MarcaFranquiaId = marcaFranquiaId,
                EstabelecimentoId = null // << ISSO ESTÁ CORRETO, pois Categoria.EstabelecimentoId é int?
            };

            _context.Categorias.Add(categoriaTemplate);
            await _context.SaveChangesAsync();

            await ProcessarAssociacoesGrupoOpcao(categoriaTemplate, categoriaDto.GrupoOpcaoIds, null, marcaFranquiaId);

            return await ObterTemplateDaMarcaPorIdAsync(categoriaTemplate.Id, marcaFranquiaId);
        }

        public async Task<bool> AtualizarTemplateAsync(int categoriaTemplateId, CategoriaCriarDto categoriaDto, int marcaFranquiaId)
        {
            var categoriaTemplate = await _context.Categorias
                .Include(c => c.GruposDeOpcaoAssociados)
                .FirstOrDefaultAsync(c => c.Id == categoriaTemplateId && c.MarcaFranquiaId == marcaFranquiaId && c.EstabelecimentoId == null);

            if (categoriaTemplate == null) return false;

            categoriaTemplate.Nome = categoriaDto.Nome;
            categoriaTemplate.OrdemExibicao = categoriaDto.OrdemExibicao;

            await ProcessarAssociacoesGrupoOpcao(categoriaTemplate, categoriaDto.GrupoOpcaoIds, null, marcaFranquiaId);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<CategoriaVerDto>> ObterTodasTemplatesDaMarcaAsync(int marcaFranquiaId)
        {
            var categorias = await _context.Categorias
                .Where(c => c.MarcaFranquiaId == marcaFranquiaId && c.EstabelecimentoId == null) // Apenas templates da marca
                .Include(c => c.GruposDeOpcaoAssociados).ThenInclude(cgo => cgo.GrupoOpcao).ThenInclude(go => go.ItensOpcao)
                .OrderBy(c => c.OrdemExibicao).ThenBy(c => c.Nome)
                .ToListAsync();
            return categorias.Select(MapToVerDto).ToList();
        }

        public async Task<CategoriaVerDto?> ObterTemplateDaMarcaPorIdAsync(int categoriaTemplateId, int marcaFranquiaId)
        {
            var categoria = await _context.Categorias
                .Include(c => c.GruposDeOpcaoAssociados).ThenInclude(cgo => cgo.GrupoOpcao).ThenInclude(go => go.ItensOpcao)
                .FirstOrDefaultAsync(c => c.Id == categoriaTemplateId && c.MarcaFranquiaId == marcaFranquiaId && c.EstabelecimentoId == null);
            return categoria == null ? null : MapToVerDto(categoria);
        }

        public async Task<bool> DeletarTemplateAsync(int categoriaTemplateId, int marcaFranquiaId)
        {
            var categoriaTemplate = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == categoriaTemplateId && c.MarcaFranquiaId == marcaFranquiaId && c.EstabelecimentoId == null);
            if (categoriaTemplate == null) return false;

            // TODO: Validar se este template de categoria está em uso por alguma LojaItemConfig
            // antes de permitir a deleção, ou definir regras de órfãos.
            // Por agora, deleção direta.
            _context.Categorias.Remove(categoriaTemplate);
            await _context.SaveChangesAsync();
            return true;
        }


        // --- MÉTODOS COMBINADOS OU PARA ROLES ESPECÍFICOS ---

        public async Task<IEnumerable<CategoriaVerDto>> ObterTodasDaLojaEMarcaAsync(int estabelecimentoId)
        {
            var loja = await _context.Estabelecimentos.FindAsync(estabelecimentoId);
            if (loja == null) return new List<CategoriaVerDto>();

            var categoriasDaLoja = await _context.Categorias
                .Where(c => c.EstabelecimentoId == estabelecimentoId && c.MarcaFranquiaId == null) // Locais da loja
                .Include(c => c.GruposDeOpcaoAssociados).ThenInclude(cgo => cgo.GrupoOpcao).ThenInclude(go => go.ItensOpcao)
                .ToListAsync();

            var resultado = categoriasDaLoja.Select(MapToVerDto).ToList();

            if (loja.MarcaFranquiaId.HasValue)
            {
                var categoriasTemplateDaMarca = await _context.Categorias
                    .Where(c => c.MarcaFranquiaId == loja.MarcaFranquiaId.Value && c.EstabelecimentoId == null) // Templates da marca
                                                                                                                // TODO: Aqui entra a lógica de LojaItemConfig para saber se a loja usa este template e com quais overrides.
                                                                                                                // Por agora, vamos apenas listar os templates da marca. No futuro, mesclar com LojaItemConfig.
                    .Include(c => c.GruposDeOpcaoAssociados).ThenInclude(cgo => cgo.GrupoOpcao).ThenInclude(go => go.ItensOpcao)
                    .ToListAsync();

                resultado.AddRange(categoriasTemplateDaMarca.Select(MapToVerDto));
            }
            // Ordenar a lista combinada
            return resultado.OrderBy(c => c.OrdemExibicao).ThenBy(c => c.Nome).ToList();
        }


        // --- MÉTODOS PARA SUPERADMIN --- (Podem precisar de paginação no futuro)
        public async Task<IEnumerable<CategoriaVerDto>> ObterTodasParaSuperAdminAsync()
        {
            var categorias = await _context.Categorias // Pega todas, de lojas e templates
                .Include(c => c.GruposDeOpcaoAssociados).ThenInclude(cgo => cgo.GrupoOpcao).ThenInclude(go => go.ItensOpcao)
                .Include(c => c.Estabelecimento) // Para saber de qual loja é
                .Include(c => c.MarcaFranquiaOriginadora) // Para saber de qual marca é o template
                .OrderBy(c => c.EstabelecimentoId).ThenBy(c => c.MarcaFranquiaId).ThenBy(c => c.OrdemExibicao).ThenBy(c => c.Nome)
                .ToListAsync();
            return categorias.Select(MapToVerDto).ToList(); // O MapToVerDto precisaria ser adaptado para mostrar nome do Est/Marca
        }

        public async Task<CategoriaVerDto?> ObterPorIdParaSuperAdminAsync(int categoriaId)
        {
            var categoria = await _context.Categorias
               .Include(c => c.GruposDeOpcaoAssociados).ThenInclude(cgo => cgo.GrupoOpcao).ThenInclude(go => go.ItensOpcao)
               .Include(c => c.Estabelecimento)
               .Include(c => c.MarcaFranquiaOriginadora)
               .FirstOrDefaultAsync(c => c.Id == categoriaId);
            return categoria == null ? null : MapToVerDto(categoria); // Adaptar MapToVerDto
        }


        // --- MÉTODO HELPER PRIVADO ---
        private async Task ProcessarAssociacoesGrupoOpcao(Categoria categoria, List<int>? grupoOpcaoIds, int? estabelecimentoId, int? marcaFranquiaId)
        {
            // Se categoria.GruposDeOpcaoAssociados não foi carregado, carrega
            if (!_context.Entry(categoria).Collection(c => c.GruposDeOpcaoAssociados).IsLoaded)
            {
                await _context.Entry(categoria).Collection(c => c.GruposDeOpcaoAssociados).LoadAsync();
            }

            // Remover associações antigas que não estão na nova lista
            var idsParaRemover = categoria.GruposDeOpcaoAssociados
                                    .Select(cgo => cgo.GrupoOpcaoId)
                                    .Except(grupoOpcaoIds ?? new List<int>())
                                    .ToList();
            if (idsParaRemover.Any())
            {
                var associacoesParaRemover = categoria.GruposDeOpcaoAssociados
                                                .Where(cgo => idsParaRemover.Contains(cgo.GrupoOpcaoId))
                                                .ToList();
                _context.CategoriasGruposOpcao.RemoveRange(associacoesParaRemover);
            }

            // Adicionar novas associações
            if (grupoOpcaoIds != null)
            {
                var idsJaAssociados = categoria.GruposDeOpcaoAssociados.Select(cgo => cgo.GrupoOpcaoId).ToList();
                var idsParaAdicionar = grupoOpcaoIds.Except(idsJaAssociados).ToList();

                foreach (var grupoId in idsParaAdicionar)
                {
                    // Valida se o grupoOpcao pertence ao mesmo contexto (estabelecimento ou marca)
                    bool grupoValido = false;
                    if (estabelecimentoId.HasValue) // Categoria de Loja
                    {
                        // Permite associar grupos da própria loja OU templates da marca da loja
                        var est = await _context.Estabelecimentos.FindAsync(estabelecimentoId.Value);
                        grupoValido = await _context.GruposOpcao.AnyAsync(go => go.Id == grupoId &&
                            (go.EstabelecimentoId == estabelecimentoId.Value || (est.MarcaFranquiaId.HasValue && go.MarcaFranquiaId == est.MarcaFranquiaId.Value && go.EstabelecimentoId == null)));
                    }
                    else if (marcaFranquiaId.HasValue) // Categoria Template de Marca
                    {
                        // Permite associar apenas grupos template da mesma marca
                        grupoValido = await _context.GruposOpcao.AnyAsync(go => go.Id == grupoId && go.MarcaFranquiaId == marcaFranquiaId.Value && go.EstabelecimentoId == null);
                    }

                    if (grupoValido)
                    {
                        categoria.GruposDeOpcaoAssociados.Add(new CategoriaGrupoOpcao
                        {
                            // CategoriaId é o da 'categoria'
                            GrupoOpcaoId = grupoId
                            // OrdemExibicaoGrupoNaCategoria pode ser definida aqui se o DTO incluir
                        });
                    }
                    else
                    {
                        Debug.WriteLine($"Tentativa de associar GrupoOpcaoId {grupoId} inválido/não permitido à CategoriaId {categoria.Id}");
                    }
                }
            }
            // SaveChanges será chamado pelo método chamador (CriarAsync/AtualizarAsync) após esta função.
        }

        // Services/CategoriaService.cs
        private CategoriaVerDto MapToVerDto(Categoria categoria)
        {
            return new CategoriaVerDto
            {
                Id = categoria.Id,
                Nome = categoria.Nome,
                OrdemExibicao = categoria.OrdemExibicao,
                EstabelecimentoId = categoria.EstabelecimentoId, // Atribuição direta agora é válida
                MarcaFranquiaId = categoria.MarcaFranquiaId,   // Adicionado
                GruposOpcaoAssociados = categoria.GruposDeOpcaoAssociados?
                    .Select(cgo => new GrupoOpcaoVerDto
                    {
                        Id = cgo.GrupoOpcao.Id,
                        Nome = cgo.GrupoOpcao.Nome,
                        Descricao = cgo.GrupoOpcao.Descricao,
                        MinSelecoes = cgo.GrupoOpcao.MinSelecoes,
                        MaxSelecoes = cgo.GrupoOpcao.MaxSelecoes,
                        OrdemExibicao = cgo.OrdemExibicaoGrupoNaCategoria,
                        EstabelecimentoId = cgo.GrupoOpcao.EstabelecimentoId, // Atribuição direta
                        MarcaFranquiaId = cgo.GrupoOpcao.MarcaFranquiaId,     // Adicionado
                        QuantidadeItens = cgo.GrupoOpcao.ItensOpcao?.Count ?? 0
                    })
                    .OrderBy(g => g.OrdemExibicao)
                    .ThenBy(g => g.Nome)
                    .ToList() ?? new List<GrupoOpcaoVerDto>()
            };
        }
    }
}