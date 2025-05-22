// Services/ProdutoService.cs
// ... (usings, construtor) ...

using Jade.Data;
using Jade.Dtos;
using Jade.Models;
using Jade.Services;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

public class ProdutoService : IProdutoService
{
    private readonly AppDbContext _context;
    private readonly IGrupoOpcaoService _grupoOpcaoService;
    public ProdutoService(AppDbContext context, IGrupoOpcaoService grupoOpcaoService)
    {
        _context = context;
        _grupoOpcaoService = grupoOpcaoService; // Salva a instância
    }

    // --- MÉTODOS PARA PRODUTOS DE LOJA (EstabelecimentoId específico) ---
    public async Task<ProdutoVerDto?> CriarAsync(ProdutoCriarDto dto, int estabelecimentoId)
    {
        if (!await _context.Estabelecimentos.AnyAsync(e => e.Id == estabelecimentoId)) return null;

        Categoria? categoriaDb = null;
        if (dto.CategoriaId.HasValue && dto.CategoriaId.Value > 0)
        {
            // Categoria deve ser local desta loja
            categoriaDb = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == dto.CategoriaId.Value && c.EstabelecimentoId == estabelecimentoId && c.MarcaFranquiaId == null);
            if (categoriaDb == null) return null;
        }

        var produto = new Produto
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Preco = dto.Preco,
            ImagemUrl = dto.ImagemUrl,
            Disponivel = dto.Disponivel,
            CategoriaId = categoriaDb?.Id,
            EstabelecimentoId = estabelecimentoId,
            MarcaFranquiaId = null
        };
        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();
        return MapToVerDto(produto, categoriaDb?.Nome, false);
    }

    public async Task<bool> AtualizarAsync(int id, ProdutoCriarDto dto, int estabelecimentoId)
    {
        var produtoExistente = await _context.Produtos.FirstOrDefaultAsync(p => p.Id == id && p.EstabelecimentoId == estabelecimentoId && p.MarcaFranquiaId == null);
        if (produtoExistente == null) return false;

        Categoria? categoriaDb = null;
        if (dto.CategoriaId.HasValue && dto.CategoriaId.Value > 0)
        {
            categoriaDb = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == dto.CategoriaId.Value && c.EstabelecimentoId == estabelecimentoId && c.MarcaFranquiaId == null);
            if (categoriaDb == null) return false; // Categoria inválida para esta loja
        }
        else if (dto.CategoriaId == null || dto.CategoriaId == 0)
        {
            // Permite desassociar categoria
            produtoExistente.CategoriaId = null;
        }


        produtoExistente.Nome = dto.Nome;
        produtoExistente.Descricao = dto.Descricao;
        produtoExistente.Preco = dto.Preco;
        produtoExistente.ImagemUrl = dto.ImagemUrl;
        produtoExistente.Disponivel = dto.Disponivel;
        produtoExistente.CategoriaId = categoriaDb?.Id ?? produtoExistente.CategoriaId; // Mantém se categoriaDb for null e CategoriaId não foi explicitamente setado para null

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ProdutoVerDto>> ObterTodosAsync(int estabelecimentoId, int? categoriaId = null, bool apenasAtivosParaCardapio = false)
    {
        var query = _context.Produtos
            .Where(p => p.EstabelecimentoId == estabelecimentoId && p.MarcaFranquiaId == null)
            .Include(p => p.Categoria)
            .AsQueryable();
        if (categoriaId.HasValue) query = query.Where(p => p.CategoriaId == categoriaId.Value);
        if (apenasAtivosParaCardapio) query = query.Where(p => p.Disponivel);

        var produtos = await query.OrderBy(p => p.Categoria.OrdemExibicao).ThenBy(p => p.Nome).ToListAsync();
        return produtos.Select(p => MapToVerDto(p, p.Categoria?.Nome, false)).ToList();
    }

    public async Task<ProdutoVerDto?> ObterPorIdAsync(int id, int estabelecimentoId)
    {
        var produto = await _context.Produtos.Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id && p.EstabelecimentoId == estabelecimentoId && p.MarcaFranquiaId == null);
        return produto == null ? null : MapToVerDto(produto, produto.Categoria?.Nome, false);
    }

    public async Task<bool> DeletarAsync(int id, int estabelecimentoId)
    {
        var produto = await _context.Produtos.FirstOrDefaultAsync(p => p.Id == id && p.EstabelecimentoId == estabelecimentoId && p.MarcaFranquiaId == null);
        if (produto == null) return false;
        _context.Produtos.Remove(produto);
        await _context.SaveChangesAsync();
        return true;
    }


    // --- MÉTODOS PARA PRODUTOS TEMPLATE DE MARCA/FRANQUIA ---
    public async Task<ProdutoVerDto?> CriarTemplateAsync(ProdutoCriarDto dto, int marcaFranquiaId, int? categoriaTemplateId)
    {
        if (!await _context.MarcasFranquia.AnyAsync(mf => mf.Id == marcaFranquiaId)) return null;

        Categoria? categoriaTemplateDb = null;
        if (categoriaTemplateId.HasValue && categoriaTemplateId.Value > 0)
        {
            categoriaTemplateDb = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == categoriaTemplateId.Value && c.MarcaFranquiaId == marcaFranquiaId && c.EstabelecimentoId == null);
            if (categoriaTemplateDb == null) return null;
        }

        var produtoTemplate = new Produto
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Preco = dto.Preco,
            ImagemUrl = dto.ImagemUrl,
            Disponivel = dto.Disponivel,
            CategoriaId = categoriaTemplateDb?.Id,
            MarcaFranquiaId = marcaFranquiaId,
            EstabelecimentoId = null
        };
        _context.Produtos.Add(produtoTemplate);
        await _context.SaveChangesAsync();
        return MapToVerDto(produtoTemplate, categoriaTemplateDb?.Nome, true);
    }

    public async Task<bool> AtualizarTemplateAsync(int produtoTemplateId, ProdutoCriarDto dto, int marcaFranquiaId, int? categoriaTemplateId)
    {
        var produtoTemplate = await _context.Produtos.FirstOrDefaultAsync(p => p.Id == produtoTemplateId && p.MarcaFranquiaId == marcaFranquiaId && p.EstabelecimentoId == null);
        if (produtoTemplate == null) return false;

        Categoria? categoriaTemplateDb = null;
        if (categoriaTemplateId.HasValue && categoriaTemplateId.Value > 0)
        {
            categoriaTemplateDb = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == categoriaTemplateId.Value && c.MarcaFranquiaId == marcaFranquiaId && c.EstabelecimentoId == null);
            if (categoriaTemplateDb == null) return false;
        }
        else if (categoriaTemplateId == null)
        {
            produtoTemplate.CategoriaId = null; // Permite desassociar categoria template
        }

        produtoTemplate.Nome = dto.Nome; produtoTemplate.Descricao = dto.Descricao; produtoTemplate.Preco = dto.Preco;
        produtoTemplate.ImagemUrl = dto.ImagemUrl; produtoTemplate.Disponivel = dto.Disponivel;
        produtoTemplate.CategoriaId = categoriaTemplateDb?.Id ?? produtoTemplate.CategoriaId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ProdutoVerDto>> ObterTodosTemplatesDaMarcaAsync(int marcaFranquiaId, int? categoriaTemplateId = null)
    {
        var query = _context.Produtos
            .Where(p => p.MarcaFranquiaId == marcaFranquiaId && p.EstabelecimentoId == null)
            .Include(p => p.Categoria)
            .AsQueryable();
        if (categoriaTemplateId.HasValue) query = query.Where(p => p.CategoriaId == categoriaTemplateId.Value);

        var produtos = await query.OrderBy(p => p.Categoria.OrdemExibicao).ThenBy(p => p.Nome).ToListAsync();
        return produtos.Select(p => MapToVerDto(p, p.Categoria?.Nome, true)).ToList();
    }

    public async Task<ProdutoVerDto?> ObterTemplateDaMarcaPorIdAsync(int produtoTemplateId, int marcaFranquiaId)
    {
        var produto = await _context.Produtos.Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == produtoTemplateId && p.MarcaFranquiaId == marcaFranquiaId && p.EstabelecimentoId == null);
        return produto == null ? null : MapToVerDto(produto, produto.Categoria?.Nome, true);
    }

    public async Task<bool> DeletarTemplateAsync(int produtoTemplateId, int marcaFranquiaId)
    {
        var produtoTemplate = await _context.Produtos
            .FirstOrDefaultAsync(p => p.Id == produtoTemplateId && p.MarcaFranquiaId == marcaFranquiaId && p.EstabelecimentoId == null);
        if (produtoTemplate == null) return false;

        // Validação se está em uso por LojaItemConfig
        bool emUsoPorLoja = await _context.LojaItemConfiguracoes
            .AnyAsync(lic => lic.ItemOriginalId == produtoTemplateId && lic.TipoItem == TipoItemConfiguravel.Produto && lic.AtivoNaLoja);
        if (emUsoPorLoja)
        {
            Debug.WriteLine($"Produto Template ID {produtoTemplateId} está em uso por lojas e não pode ser deletado.");
            return false;
        }

        _context.Produtos.Remove(produtoTemplate);
        await _context.SaveChangesAsync();
        return true;
    }


    // --- MÉTODO COMBINADO PARA ADMINLOJA DE FRANQUIA ---
    public async Task<IEnumerable<ProdutoVerDto>> ObterTodosDaLojaEMarcaAsync(int estabelecimentoId, int? categoriaId = null, bool apenasAtivosParaCardapio = false)
    {
        var loja = await _context.Estabelecimentos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == estabelecimentoId);
        if (loja == null) return new List<ProdutoVerDto>();

        var resultadoFinal = new List<ProdutoVerDto>();

        // 1. Produtos 100% locais da loja
        var queryLocais = _context.Produtos
            .Where(p => p.EstabelecimentoId == estabelecimentoId && p.MarcaFranquiaId == null)
            .Include(p => p.Categoria).AsQueryable();
        if (categoriaId.HasValue) queryLocais = queryLocais.Where(p => p.CategoriaId == categoriaId.Value);
        if (apenasAtivosParaCardapio) queryLocais = queryLocais.Where(p => p.Disponivel);

        var produtosLocais = await queryLocais.OrderBy(p => p.Categoria.OrdemExibicao).ThenBy(p => p.Nome).ToListAsync();
        resultadoFinal.AddRange(produtosLocais.Select(p => MapToVerDto(p, p.Categoria?.Nome, false)));

        // 2. Produtos template da marca, aplicando configurações de LojaItemConfig
        if (loja.MarcaFranquiaId.HasValue)
        {
            var queryTemplates = _context.Produtos
                .Where(p => p.MarcaFranquiaId == loja.MarcaFranquiaId.Value && p.EstabelecimentoId == null)
                .Include(p => p.Categoria).AsQueryable();

            if (categoriaId.HasValue)
            {
                // Se filtrando por categoria, os templates devem pertencer a uma categoria template correspondente
                // ou a uma categoria local que está configurada para usar este template de produto.
                // Esta lógica pode ser complexa. Por agora, filtramos pela CategoriaId do template.
                queryTemplates = queryTemplates.Where(p => p.CategoriaId == categoriaId.Value);
            }

            var produtosTemplate = await queryTemplates.OrderBy(p => p.Categoria.OrdemExibicao).ThenBy(p => p.Nome).ToListAsync();

            foreach (var pt in produtosTemplate)
            {
                var configLoja = await _context.LojaItemConfiguracoes.AsNoTracking()
                    .FirstOrDefaultAsync(lic => lic.EstabelecimentoId == estabelecimentoId &&
                                                lic.TipoItem == TipoItemConfiguravel.Produto && lic.ItemOriginalId == pt.Id);

                bool exibirEsteTemplate = true;
                if (apenasAtivosParaCardapio)
                {
                    if (configLoja != null)
                    { // Se existe configuração específica da loja
                        if (!configLoja.AtivoNaLoja || (configLoja.DisponivelLocalmente.HasValue && !configLoja.DisponivelLocalmente.Value))
                        {
                            exibirEsteTemplate = false;
                        }
                    }
                    else if (!pt.Disponivel)
                    { // Se não há config da loja, usa disponibilidade do template
                        exibirEsteTemplate = false;
                    }
                }
                else
                { // Se não for para mostrar apenas ativos (ex: no painel admin)
                    if (configLoja != null && !configLoja.AtivoNaLoja)
                    {
                        // Mesmo não sendo "apenasAtivosParaCardapio", se a loja explicitamente desativou, não mostra
                        // (a menos que a regra para o painel admin seja diferente)
                        // Para o cardápio do cliente, esta condição é importante.
                        // Para o painel AdminLoja listando "itens da franquia", talvez ele queira ver mesmo os inativos.
                        // Ajustar `apenasAtivosParaCardapio` conforme o contexto da chamada.
                        if (apenasAtivosParaCardapio) exibirEsteTemplate = false; // Redundante, mas claro
                    }
                }

                if (exibirEsteTemplate)
                {
                    resultadoFinal.Add(new ProdutoVerDto
                    {
                        Id = pt.Id,
                        Nome = configLoja?.NomeLocal ?? pt.Nome,
                        Descricao = pt.Descricao, // Pode ter DescricaoLocal
                        Preco = configLoja?.PrecoLocal ?? pt.Preco,
                        ImagemUrl = pt.ImagemUrl, // Pode ter ImagemUrlLocal
                        Disponivel = configLoja?.DisponivelLocalmente ?? pt.Disponivel,
                        CategoriaId = pt.CategoriaId,
                        CategoriaNome = pt.Categoria?.Nome,
                        EstabelecimentoId = estabelecimentoId,
                        MarcaFranquiaId = pt.MarcaFranquiaId,
                        IsTemplateDaFranquia = true
                    });
                }
            }
        }
        return resultadoFinal.OrderBy(p => p.CategoriaNome).ThenBy(p => p.IsTemplateDaFranquia).ThenBy(p => p.Nome).ToList();
    }

    // --- MÉTODOS PARA SUPERADMIN ---
    public async Task<IEnumerable<ProdutoVerDto>> ObterTodosParaSuperAdminAsync(int? categoriaId = null)
    {
        Debug.WriteLine("[ProdutoService] ObterTodosParaSuperAdminAsync - INÍCIO");
        try
        {
            var query = _context.Produtos.AsQueryable(); // Pega todos inicialmente

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            // Traz as entidades para a memória ANTES de tentar ordenações complexas ou mapeamento
            var produtosDoBanco = await query
                                        .Include(p => p.Categoria) // Inclui para ter acesso seguro a Categoria.Nome
                                        .ToListAsync();
            Debug.WriteLine($"[ProdutoService] ObterTodosParaSuperAdminAsync - {produtosDoBanco.Count} produtos brutos do banco.");

            // Mapeia em memória
            var resultadoDto = produtosDoBanco.Select(p => MapToVerDto(
                                                            p,
                                                            p.Categoria?.Nome, // Passa o nome da categoria de forma segura
                                                            p.MarcaFranquiaId.HasValue && !p.EstabelecimentoId.HasValue // Determina se é template
                                                        )).ToList();

            // Ordena a lista de DTOs em memória (mais seguro para propriedades que podem ser nulas)
            resultadoDto = resultadoDto
                .OrderBy(dto => dto.MarcaFranquiaId == null) // Templates (MarcaId != null) primeiro ou último
                .ThenBy(dto => dto.MarcaFranquiaId)
                .ThenBy(dto => dto.EstabelecimentoId == null)
                .ThenBy(dto => dto.EstabelecimentoId)
                .ThenBy(dto => dto.CategoriaNome == null)
                .ThenBy(dto => dto.CategoriaNome)
                .ThenBy(dto => dto.Nome)
                .ToList();

            Debug.WriteLine($"[ProdutoService] ObterTodosParaSuperAdminAsync - {resultadoDto.Count} produtos mapeados e ordenados.");
            return resultadoDto;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProdutoService] ERRO em ObterTodosParaSuperAdminAsync: {ex.ToString()}");
            throw;
        }
    }
    public async Task<ProdutoVerDto?> ObterPorIdParaSuperAdminAsync(int produtoId)
    {
        var produto = await _context.Produtos.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == produtoId);
        return produto == null ? null : MapToVerDto(produto, produto.Categoria?.Nome, produto.MarcaFranquiaId.HasValue && !produto.EstabelecimentoId.HasValue);
    }

    public async Task<IEnumerable<GrupoOpcaoVerDto>> ObterOpcoesDePersonalizacaoAsync(int produtoId, int estabelecimentoId)
    {
        // 1. Busca o Produto
        // produtoId = 2
        var produto = await _context.Produtos
            .Include(p => p.Categoria) // Inclui a Categoria com ID 3
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == produtoId); // Encontra "Pizza"

        if (produto == null)
        {
            Debug.WriteLine($"[ObterOpcoesPersonalizacao] Produto ID {produtoId} não encontrado.");
            return new List<GrupoOpcaoVerDto>();
        }

        // 2. Verifica se o Produto é acessível/válido no contexto do Estabelecimento
        // estabelecimentoId = 1
        var loja = await _context.Estabelecimentos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == estabelecimentoId);
        if (loja == null)
        {
            Debug.WriteLine($"[ObterOpcoesPersonalizacao] Loja ID {estabelecimentoId} não encontrada.");
            return new List<GrupoOpcaoVerDto>();
        }

        // produto.EstabelecimentoId é NULL, produto.MarcaFranquiaId é 1
        // loja.MarcaFranquiaId é 1
        bool acessoPermitidoAoProduto = (produto.EstabelecimentoId == estabelecimentoId) || // FALSE (null != 1)
                                     (produto.EstabelecimentoId == null && produto.MarcaFranquiaId.HasValue && produto.MarcaFranquiaId == loja.MarcaFranquiaId); // TRUE (null == null && true && 1 == 1)

        if (!acessoPermitidoAoProduto)
        {
            Debug.WriteLine($"[ObterOpcoesPersonalizacao] Acesso ao Produto ID {produtoId} negado para Loja ID {estabelecimentoId}.");
            return new List<GrupoOpcaoVerDto>();
        }

        // produto.CategoriaId é 3
        if (!produto.CategoriaId.HasValue)
        {
            Debug.WriteLine($"[ObterOpcoesPersonalizacao] Produto ID {produtoId} não tem CategoriaId.");
            return new List<GrupoOpcaoVerDto>();
        }

        // 3. Busca a Categoria do Produto e seus Grupos de Opção associados
        // produto.CategoriaId.Value é 3
        var categoriaDoProduto = await _context.Categorias
            .Include(c => c.GruposDeOpcaoAssociados) // Carrega CategoriaGrupoOpcao
                .ThenInclude(cgo => cgo.GrupoOpcao)   // De CategoriaGrupoOpcao, carrega GrupoOpcao
                    .ThenInclude(go => go.ItensOpcao.Where(io => io.Disponivel)) // De GrupoOpcao, carrega SEUS ItensOpcao DISPONÍVEIS
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == produto.CategoriaId.Value);

        if (categoriaDoProduto == null)
        {
            Debug.WriteLine($"[ObterOpcoesPersonalizacao] Categoria ID {produto.CategoriaId.Value} do produto não encontrada.");
            return new List<GrupoOpcaoVerDto>();
        }
        if (categoriaDoProduto.GruposDeOpcaoAssociados == null || !categoriaDoProduto.GruposDeOpcaoAssociados.Any())
        {
            Debug.WriteLine($"[ObterOpcoesPersonalizacao] Categoria ID {produto.CategoriaId.Value} não tem Grupos de Opção associados.");
            return new List<GrupoOpcaoVerDto>(); // Nenhuma opção se a categoria não tiver grupos
        }

        var gruposDeOpcaoParaCliente = new List<GrupoOpcaoVerDto>();
        bool apenasAtivosParaCardapio = true; // Para o cardápio do cliente, geralmente sim

        // Itera sobre as ASSOCIAÇÕES da categoria com grupos de opção
        foreach (var cgo in categoriaDoProduto.GruposDeOpcaoAssociados.OrderBy(c => c.OrdemExibicaoGrupoNaCategoria))
        {
            var grupoOriginal = cgo.GrupoOpcao; // Este é o GrupoOpcao (pode ser template ou local)
            if (grupoOriginal == null)
            {
                Debug.WriteLine($"[ObterOpcoesPersonalizacao] Encontrado CGO com GrupoOpcao nulo para Categoria ID {categoriaDoProduto.Id}. CGO CategoriaId: {cgo.CategoriaId}, CGO GrupoOpcaoId: {cgo.GrupoOpcaoId}");
                continue;
            }

            // Chama o método do _grupoOpcaoService para mapear este grupoOriginal para o cliente,
            // aplicando as configurações da LojaItemConfig para este grupo E para seus itens.
            var grupoDto = await _grupoOpcaoService.MapGrupoOpcaoParaClienteAsync(grupoOriginal, estabelecimentoId, apenasAtivosParaCardapio);

            if (grupoDto != null) // MapGrupoOpcaoParaClienteAsync retorna null se o grupo não deve ser mostrado
            {
                // A checagem se tem itens já foi feita dentro de MapGrupoOpcaoParaClienteAsync
                gruposDeOpcaoParaCliente.Add(grupoDto);
            }
            else
            {
                Debug.WriteLine($"[ObterOpcoesPersonalizacao] GrupoOpcao ID {grupoOriginal.Id} ('{grupoOriginal.Nome}') foi filtrado por MapGrupoOpcaoParaClienteAsync.");
            }
        }
        Debug.WriteLine($"[ObterOpcoesPersonalizacao] Retornando {gruposDeOpcaoParaCliente.Count} grupos de opção para o cliente.");
        return gruposDeOpcaoParaCliente;
    }

    // Helper de Mapeamento
    private ProdutoVerDto MapToVerDto(Produto produto, string? categoriaNome, bool isTemplate = false)
    {
        return new ProdutoVerDto
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Preco = produto.Preco,
            ImagemUrl = produto.ImagemUrl,
            Disponivel = produto.Disponivel,
            CategoriaId = produto.CategoriaId,
            CategoriaNome = categoriaNome,
            EstabelecimentoId = produto.EstabelecimentoId,
            MarcaFranquiaId = produto.MarcaFranquiaId,
            IsTemplateDaFranquia = isTemplate || (produto.MarcaFranquiaId.HasValue && !produto.EstabelecimentoId.HasValue)
        };
    }
}