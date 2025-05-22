// Services/MarcaFranquiaService.cs
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
    public class MarcaFranquiaService : IMarcaFranquiaService
    {
        private readonly AppDbContext _context;

        public MarcaFranquiaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MarcaFranquiaVerDto?> CriarAsync(MarcaFranquiaCriarDto dto)
        {
            // Validação para nome único de marca/franquia
            if (await _context.MarcasFranquia.AnyAsync(mf => mf.Nome == dto.Nome))
            {
                Debug.WriteLine($"[MarcaFranquiaService] Tentativa de criar marca com nome duplicado: {dto.Nome}");
                // Poderia lançar uma exceção ou retornar um resultado que indique o conflito
                return null;
            }

            var marcaFranquia = new MarcaFranquia
            {
                Nome = dto.Nome,
                LogoUrl = dto.LogoUrl
            };

            _context.MarcasFranquia.Add(marcaFranquia);
            await _context.SaveChangesAsync();

            return MapToVerDto(marcaFranquia);
        }

        public async Task<MarcaFranquiaVerDto?> ObterPorIdAsync(int id)
        {
            var marcaFranquia = await _context.MarcasFranquia.FindAsync(id);
            return marcaFranquia == null ? null : MapToVerDto(marcaFranquia);
        }

        public async Task<IEnumerable<MarcaFranquiaVerDto>> ObterTodasAsync()
        {
            return await _context.MarcasFranquia
                .OrderBy(mf => mf.Nome)
                .Select(mf => MapToVerDto(mf))
                .ToListAsync();
        }

        public async Task<bool> AtualizarAsync(int id, MarcaFranquiaCriarDto dto)
        {
            var marcaExistente = await _context.MarcasFranquia.FindAsync(id);
            if (marcaExistente == null) return false;

            // Validação para nome único (exceto para o próprio registro)
            if (await _context.MarcasFranquia.AnyAsync(mf => mf.Nome == dto.Nome && mf.Id != id))
            {
                Debug.WriteLine($"[MarcaFranquiaService] Tentativa de atualizar para nome duplicado: {dto.Nome}");
                return false; // Ou lançar exceção
            }

            marcaExistente.Nome = dto.Nome;
            marcaExistente.LogoUrl = dto.LogoUrl;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletarAsync(int id)
        {
            var marca = await _context.MarcasFranquia
                .Include(mf => mf.EstabelecimentosDaMarca) // Para verificar se há estabelecimentos
                .Include(mf => mf.CategoriasTemplateDaMarca) // Para verificar se há templates
                .Include(mf => mf.ProdutosTemplateDaMarca)
                .Include(mf => mf.GruposOpcaoTemplateDaMarca)
                .FirstOrDefaultAsync(mf => mf.Id == id);

            if (marca == null) return false;

            // Regra de negócio: Não permitir deletar marca se ela tiver estabelecimentos vinculados
            // A FK em Estabelecimento.MarcaFranquiaId é SetNull, então os estabelecimentos ficariam órfãos.
            // Poderíamos impedir a deleção aqui se for a regra de negócio.
            if (marca.EstabelecimentosDaMarca.Any())
            {
                Debug.WriteLine($"[MarcaFranquiaService] Tentativa de deletar marca ID {id} que ainda possui estabelecimentos.");
                // Retornar false ou lançar uma exceção indicando que não pode ser deletada.
                // Para este exemplo, vamos permitir, e os estabelecimentos terão MarcaFranquiaId = null.
            }

            // As regras ON DELETE CASCADE para CategoriasTemplate, ProdutosTemplate, GruposOpcaoTemplate
            // vinculados à MarcaFranquia cuidarão da remoção desses templates.
            // As regras ON DELETE SET NULL para UsuarioAdmin.MarcaFranquiaIdVinculada e
            // Estabelecimento.MarcaFranquiaId também serão aplicadas pelo banco.

            _context.MarcasFranquia.Remove(marca);
            await _context.SaveChangesAsync();
            return true;
        }

        private static MarcaFranquiaVerDto MapToVerDto(MarcaFranquia marcaFranquia)
        {
            return new MarcaFranquiaVerDto
            {
                Id = marcaFranquia.Id,
                Nome = marcaFranquia.Nome,
                LogoUrl = marcaFranquia.LogoUrl
                // QuantidadeEstabelecimentos = marcaFranquia.EstabelecimentosDaMarca?.Count ?? 0 // Se incluído e desejado
            };
        }
    }
}