// Services/ProdutoService.cs
// ... (usings, construtor) ...

using Jade.Data;
using Jade.Dtos;
using Jade.Models;
using Jade.Services;
using System.Data.Entity;
using System.Diagnostics;

public class ProdutoService : IProdutoService
{
    private readonly AppDbContext _context;
    public ProdutoService(AppDbContext context) { _context = context; }

    // --- MÉTODOS PARA PRODUTOS DE LOJA (EstabelecimentoId específico) ---
    public async Task<ProdutoVerDto?> CriarAsync(ProdutoCriarDto dto, int estabelecimentoId)
    {
        var estabelecimento = await _context.Estabelecimentos.FindAsync(estabelecimentoId);
        if (estabelecimento == null) return null;

        Categoria? categoriaDb = null;
        if (dto.CategoriaId.HasValue)
        {
            categoriaDb = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == dto.CategoriaId.Value && c.EstabelecimentoId == estabelecimentoId && c.MarcaFranquiaId == null);
            if (categoriaDb == null) return null; // Categoria de loja inválida
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
            MarcaFranquiaId = null // Produto de Loja
        };
        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();
        return MapToVerDto(produto, categoriaDb?.Nome);
    }

    public async Task<bool> AtualizarAsync(int id, ProdutoCriarDto dto, int estabelecimentoId)
    {
        var produtoExistente = await _context.Produtos.FirstOrDefaultAsync(p => p.Id == id && p.EstabelecimentoId == estabelecimentoId && p.MarcaFranquiaId == null);
        if (produtoExistente == null) return false;

        Categoria? categoriaDb = null;
        if (dto.CategoriaId.HasValue)
        {
            categoriaDb = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == dto.CategoriaId.Value && c.EstabelecimentoId == estabelecimentoId && c.MarcaFranquiaId == null);
            if (categoriaDb == null) return false;
        }

        produtoExistente.Nome = dto.Nome; /* ... outras atribuições ... */ produtoExistente.CategoriaId = categoriaDb?.Id;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ProdutoVerDto>> ObterTodosAsync(int estabelecimentoId, int? categoriaId = null, bool apenasAtivosParaCardapio = false)
    {
        var query = _context.Produtos
            .Where(p => p.EstabelecimentoId == estabelecimentoId && p.MarcaFranquiaId == null) // Apenas produtos DA LOJA
            .Include(p => p.Categoria)
            .AsQueryable();
        if (categoriaId.HasValue) query = query.Where(p => p.CategoriaId == categoriaId.Value);
        if (apenasAtivosParaCardapio) query = query.Where(p => p.Disponivel);

        var produtos = await query.OrderBy(p => p.Categoria.OrdemExibicao).ThenBy(p => p.Categoria.Nome).ThenBy(p => p.Nome).ToListAsync();
        return produtos.Select(p => MapToVerDto(p, p.Categoria?.Nome)).ToList();
    }

    public async Task<ProdutoVerDto?> ObterPorIdAsync(int id, int estabelecimentoId)
    {
        var produto = await _context.Produtos.Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id && p.EstabelecimentoId == estabelecimentoId && p.MarcaFranquiaId == null);
        return produto == null ? null : MapToVerDto(produto, produto.Categoria?.Nome);
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
        if (categoriaTemplateId.HasValue)
        {
            categoriaTemplateDb = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == categoriaTemplateId.Value && c.MarcaFranquiaId == marcaFranquiaId && c.EstabelecimentoId == null);
            if (categoriaTemplateDb == null) return null; // Categoria template inválida
        }

        var produtoTemplate = new Produto
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Preco = dto.Preco,
            ImagemUrl = dto.ImagemUrl, // Preço aqui seria "sugerido"
            Disponivel = dto.Disponivel,
            CategoriaId = categoriaTemplateDb?.Id,
            MarcaFranquiaId = marcaFranquiaId,
            EstabelecimentoId = null // É um template
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
        if (categoriaTemplateId.HasValue)
        {
            categoriaTemplateDb = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == categoriaTemplateId.Value && c.MarcaFranquiaId == marcaFranquiaId && c.EstabelecimentoId == null);
            if (categoriaTemplateDb == null) return false;
        }
        produtoTemplate.Nome = dto.Nome; /* ... etc ... */ produtoTemplate.CategoriaId = categoriaTemplateDb?.Id;
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

        var produtos = await query.OrderBy(p => p.Categoria.OrdemExibicao).ThenBy(p => p.Categoria.Nome).ThenBy(p => p.Nome).ToListAsync();
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
        var produtoTemplate = await _context.Produtos.FirstOrDefaultAsync(p => p.Id == produtoTemplateId && p.MarcaFranquiaId == marcaFranquiaId && p.EstabelecimentoId == null);
        if (produtoTemplate == null) return false;
        // TODO: Validar se este template está em uso por alguma LojaItemConfig antes de deletar
        _context.Produtos.Remove(produtoTemplate);
        await _context.SaveChangesAsync();
        return true;
    }


    // --- MÉTODO COMBINADO PARA ADMINLOJA DE FRANQUIA ---
    public async Task<IEnumerable<ProdutoVerDto>> ObterTodosDaLojaEMarcaAsync(int estabelecimentoId, int? categoriaId = null, bool apenasAtivosParaCardapio = false)
    {
        var loja = await _context.Estabelecimentos.FindAsync(estabelecimentoId);
        if (loja == null) return new List<ProdutoVerDto>();

        var resultadoFinal = new List<ProdutoVerDto>();

        // 1. Produtos locais da loja
        var queryLocais = _context.Produtos
            .Where(p => p.EstabelecimentoId == estabelecimentoId && p.MarcaFranquiaId == null)
            .Include(p => p.Categoria)
            .AsQueryable();
        if (categoriaId.HasValue) queryLocais = queryLocais.Where(p => p.CategoriaId == categoriaId.Value);
        if (apenasAtivosParaCardapio) queryLocais = queryLocais.Where(p => p.Disponivel);

        var produtosLocais = await queryLocais.ToListAsync();
        resultadoFinal.AddRange(produtosLocais.Select(p => MapToVerDto(p, p.Categoria?.Nome, false)));

        // 2. Produtos template da marca, aplicando configurações de LojaItemConfig
        if (loja.MarcaFranquiaId.HasValue)
        {
            var queryTemplates = _context.Produtos
                .Where(p => p.MarcaFranquiaId == loja.MarcaFranquiaId.Value && p.EstabelecimentoId == null) // Templates da marca
                .Include(p => p.Categoria)
                .AsQueryable();
            if (categoriaId.HasValue) // Se um filtro de categoria é aplicado, precisa ser uma categoria template
            {
                queryTemplates = queryTemplates.Where(p => p.CategoriaId == categoriaId.Value && p.Categoria.MarcaFranquiaId == loja.MarcaFranquiaId.Value);
            }
            if (apenasAtivosParaCardapio) queryTemplates = queryTemplates.Where(p => p.Disponivel); // Disponibilidade base do template

            var produtosTemplate = await queryTemplates.ToListAsync();

            foreach (var pt in produtosTemplate)
            {
                var configLoja = await _context.LojaItemConfiguracoes
                    .FirstOrDefaultAsync(lic => lic.EstabelecimentoId == estabelecimentoId &&
                                                lic.TipoItem == TipoItemConfiguravel.Produto &&
                                                lic.ItemOriginalId == pt.Id);

                if (configLoja != null && !configLoja.AtivoNaLoja) continue; // Loja desativou este template
                if (apenasAtivosParaCardapio && configLoja != null && configLoja.DisponivelLocalmente.HasValue && !configLoja.DisponivelLocalmente.Value) continue;
                if (apenasAtivosParaCardapio && configLoja == null && !pt.Disponivel) continue; // Template base não disponível e sem config local


                resultadoFinal.Add(new ProdutoVerDto
                {
                    Id = pt.Id, // Usa ID do template para referência
                    Nome = configLoja?.NomeLocal ?? pt.Nome,
                    Descricao = pt.Descricao, // Descrição do template
                    Preco = configLoja?.PrecoLocal ?? pt.Preco, // Preço da loja ou "preço sugerido" do template
                    ImagemUrl = pt.ImagemUrl,
                    Disponivel = configLoja?.DisponivelLocalmente ?? pt.Disponivel,
                    CategoriaId = pt.CategoriaId,
                    CategoriaNome = pt.Categoria?.Nome,
                    EstabelecimentoId = estabelecimentoId, // Contexto da loja que está visualizando/usando
                    MarcaFranquiaId = pt.MarcaFranquiaId,
                    IsTemplateDaFranquia = true // Indica que a base é um template
                });
            }
        }
        return resultadoFinal.OrderBy(p => p.CategoriaNome).ThenBy(p => p.Nome).ToList();
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

    // Helper de Mapeamento
    private ProdutoVerDto MapToVerDto(Produto produto, string? categoriaNome, bool isTemplate = false)
    {
        // BREAKPOINT AQUI
        return new ProdutoVerDto
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Preco = produto.Preco,
            ImagemUrl = produto.ImagemUrl,
            Disponivel = produto.Disponivel,
            CategoriaId = produto.CategoriaId,
            CategoriaNome = categoriaNome, // Passado como parâmetro
            EstabelecimentoId = produto.EstabelecimentoId,
            MarcaFranquiaId = produto.MarcaFranquiaId,
            IsTemplateDaFranquia = isTemplate // Passado como parâmetro
                                              // ou: produto.MarcaFranquiaId.HasValue && !produto.EstabelecimentoId.HasValue
        };
    }
}