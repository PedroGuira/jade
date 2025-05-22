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
                EstabelecimentoId = estabelecimentoId,
                MarcaFranquiaId = null,
                GruposDeOpcaoAssociados = new List<CategoriaGrupoOpcao>() // Inicializa
            };

            _context.Categorias.Add(categoria);

            await ProcessarAssociacoesGrupoOpcao(categoria, categoriaDto.GrupoOpcaoIds, estabelecimentoId, null);

            await _context.SaveChangesAsync();

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
                .Include(c => c.GruposDeOpcaoAssociados)
                    .ThenInclude(cgo => cgo.GrupoOpcao) // Essencial
                    .ThenInclude(go => go.ItensOpcao) // Para 
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
                EstabelecimentoId = null,
                GruposDeOpcaoAssociados = new List<CategoriaGrupoOpcao>()
            };
            _context.Categorias.Add(categoriaTemplate);
            await ProcessarAssociacoesGrupoOpcao(categoriaTemplate, categoriaDto.GrupoOpcaoIds, null, marcaFranquiaId);

            await _context.SaveChangesAsync(); // AGORA salva a categoria E suas associações de uma vez

            // ObterPorIdAsync/ObterTemplateDaMarcaPorIdAsync DEVE fazer os Includes corretos
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
        private async Task ProcessarAssociacoesGrupoOpcao(Categoria categoria, List<int>? grupoOpcaoIds, int? contextoEstabelecimentoId, int? contextoMarcaFranquiaId)
        {
            // Carrega as associações existentes se a categoria já existe e tem a coleção carregada
            if (categoria.Id > 0 && !_context.Entry(categoria).Collection(c => c.GruposDeOpcaoAssociados).IsLoaded)
            {
                // Carrega apenas as chaves ou os objetos completos, dependendo do que você precisa
                // Para simplesmente comparar IDs e remover, carregar só as chaves pode ser mais eficiente.
                // Mas para evitar problemas, carregar os objetos completos é mais seguro se a coleção não foi carregada.
                await _context.Entry(categoria).Collection(c => c.GruposDeOpcaoAssociados)
                    .Query() // Permite adicionar mais filtros ou includes se necessário
                    .Include(cgo => cgo.GrupoOpcao) // Inclui o GrupoOpcao para validação futura se necessário
                    .LoadAsync();
            }

            // Lista de IDs dos grupos atualmente associados à categoria
            var idsAtualmenteAssociados = categoria.GruposDeOpcaoAssociados.Select(cgo => cgo.GrupoOpcaoId).ToList();
            var novosIdsRecebidos = grupoOpcaoIds ?? new List<int>();

            // 1. Remover associações que não estão na nova lista (novosIdsRecebidos)
            var idsParaRemover = idsAtualmenteAssociados.Except(novosIdsRecebidos).ToList();
            if (idsParaRemover.Any())
            {
                var associacoesParaRemover = categoria.GruposDeOpcaoAssociados
                                                .Where(cgo => idsParaRemover.Contains(cgo.GrupoOpcaoId))
                                                .ToList();
                _context.CategoriasGruposOpcao.RemoveRange(associacoesParaRemover);
                // Também remove da coleção em memória da entidade Categoria para consistência
                foreach (var assocRemovida in associacoesParaRemover)
                {
                    categoria.GruposDeOpcaoAssociados.Remove(assocRemovida);
                }
            }

            // 2. Adicionar novas associações que estão na nova lista mas não nas atuais
            var idsParaAdicionar = novosIdsRecebidos.Except(idsAtualmenteAssociados).ToList();
            if (idsParaAdicionar.Any())
            {
                foreach (var grupoIdParaAdicionar in idsParaAdicionar)
                {
                    // Validação crucial: o GrupoOpcao a ser associado deve ser compatível com o contexto da Categoria
                    bool grupoValido = false;
                    var grupoOpcaoParaAssociar = await _context.GruposOpcao.FindAsync(grupoIdParaAdicionar);

                    if (grupoOpcaoParaAssociar != null)
                    {
                        if (contextoEstabelecimentoId.HasValue) // Categoria é de Loja
                        {
                            // Grupo pode ser local da loja OU template da marca da loja
                            var loja = await _context.Estabelecimentos.FindAsync(contextoEstabelecimentoId.Value);
                            if (loja != null &&
                                (grupoOpcaoParaAssociar.EstabelecimentoId == contextoEstabelecimentoId.Value ||
                                 (loja.MarcaFranquiaId.HasValue && grupoOpcaoParaAssociar.MarcaFranquiaId == loja.MarcaFranquiaId.Value && grupoOpcaoParaAssociar.EstabelecimentoId == null)))
                            {
                                grupoValido = true;
                            }
                        }
                        else if (contextoMarcaFranquiaId.HasValue) // Categoria é Template de Marca
                        {
                            // Grupo DEVE ser template da MESMA marca
                            if (grupoOpcaoParaAssociar.MarcaFranquiaId == contextoMarcaFranquiaId.Value && grupoOpcaoParaAssociar.EstabelecimentoId == null)
                            {
                                grupoValido = true;
                            }
                        }
                    }

                    if (grupoValido)
                    {
                        var novaAssociacao = new CategoriaGrupoOpcao
                        {
                            // CategoriaId será definido pelo EF Core ao adicionar à coleção de navegação da categoria
                            // ou você pode definir explicitamente: CategoriaId = categoria.Id,
                            GrupoOpcaoId = grupoIdParaAdicionar
                            // OrdemExibicaoGrupoNaCategoria = 0 // Defina se tiver essa propriedade
                        };
                        categoria.GruposDeOpcaoAssociados.Add(novaAssociacao); // Adiciona à coleção da entidade
                                                                               // _context.CategoriasGruposOpcao.Add(novaAssociacao); // Não estritamente necessário se adicionado à coleção de navegação de uma entidade rastreada
                    }
                    else
                    {
                        Debug.WriteLine($"[ProcessarAssociacoesGrupoOpcao] Tentativa de associar GrupoOpcaoId {grupoIdParaAdicionar} inválido/não permitido à CategoriaId {categoria.Id}");
                    }
                }
            }
            // O SaveChanges será chamado no método PAI (CriarAsync/AtualizarAsync) DEPOIS desta função.
            // Se esta função for chamada DENTRO de CriarAsync APÓS o primeiro SaveChanges,
            // um SaveChanges adicional é necessário aqui para persistir as associações.
            // No seu CriarTemplateAsync, você chama SaveChanges e DEPOIS ProcessarAssociacoes.
            // Isso significa que ProcessarAssociacoes precisa de seu próprio SaveChanges se modificar o contexto.
            // OU, melhor: ProcessarAssociacoes modifica o estado da entidade 'categoria', e o SaveChanges final
            // no método CriarTemplateAsync/CriarAsync persiste tudo.
        }

        // Services/CategoriaService.cs
        private CategoriaVerDto MapToVerDto(Categoria categoria)
        {
            return new CategoriaVerDto
            {
                Id = categoria.Id,
                Nome = categoria.Nome,
                OrdemExibicao = categoria.OrdemExibicao,
                EstabelecimentoId = categoria.EstabelecimentoId,
                MarcaFranquiaId = categoria.MarcaFranquiaId,
                GruposOpcaoAssociados = categoria.GruposDeOpcaoAssociados?
                    .Where(cgo => cgo.GrupoOpcao != null) // <<< ADICIONAR FILTRO PARA GARANTIR QUE GrupoOpcao NÃO É NULO
                    .Select(cgo => new GrupoOpcaoVerDto
                    {
                        Id = cgo.GrupoOpcao!.Id, // Agora podemos usar '!' se filtramos nulls, ou manter '?' se preferir
                        Nome = cgo.GrupoOpcao.Nome,
                        Descricao = cgo.GrupoOpcao.Descricao,
                        MinSelecoes = cgo.GrupoOpcao.MinSelecoes,
                        MaxSelecoes = cgo.GrupoOpcao.MaxSelecoes,
                        OrdemExibicao = cgo.OrdemExibicaoGrupoNaCategoria,
                        EstabelecimentoId = cgo.GrupoOpcao.EstabelecimentoId,
                        MarcaFranquiaId = cgo.GrupoOpcao.MarcaFranquiaId,
                        // Para QuantidadeItens, certifique-se que ItensOpcao foi incluído no GrupoOpcao
                        // O Include em ObterTemplateDaMarcaPorIdAsync já faz ThenInclude(go => go.ItensOpcao)
                        QuantidadeItens = cgo.GrupoOpcao.ItensOpcao?.Count ?? 0
                    })
                    .OrderBy(g => g.OrdemExibicao)
                    .ThenBy(g => g.Nome)
                    .ToList() ?? new List<GrupoOpcaoVerDto>()
            };
        }

    }
}