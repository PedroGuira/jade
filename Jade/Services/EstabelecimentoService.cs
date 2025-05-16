// Services/EstabelecimentoService.cs
using Jade.Data;
using Jade.Dtos;
using Jade.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jade.Services // Seu namespace
{
    public class EstabelecimentoService : IEstabelecimentoService
    {
        private readonly AppDbContext _context;

        public EstabelecimentoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EstabelecimentoVerDto?> CriarAsync(EstabelecimentoCriarDto dto)
        {
            var estabelecimento = new Estabelecimento
            {
                Nome = dto.Nome,
                LogoUrl = dto.LogoUrl,
                TelefoneWhatsapp = dto.TelefoneWhatsapp
            };

            _context.Estabelecimentos.Add(estabelecimento);
            await _context.SaveChangesAsync();

            return new EstabelecimentoVerDto
            {
                Id = estabelecimento.Id,
                Nome = estabelecimento.Nome,
                LogoUrl = estabelecimento.LogoUrl,
                TelefoneWhatsapp = estabelecimento.TelefoneWhatsapp
            };
        }

        public async Task<EstabelecimentoVerDto?> ObterPorIdAsync(int id)
        {
            var estabelecimento = await _context.Estabelecimentos.FindAsync(id);
            if (estabelecimento == null) return null;

            return new EstabelecimentoVerDto
            {
                Id = estabelecimento.Id,
                Nome = estabelecimento.Nome,
                LogoUrl = estabelecimento.LogoUrl,
                TelefoneWhatsapp = estabelecimento.TelefoneWhatsapp
            };
        }

        public async Task<IEnumerable<EstabelecimentoVerDto>> ObterTodosAsync()
        {
            return await _context.Estabelecimentos
                .Select(e => new EstabelecimentoVerDto
                {
                    Id = e.Id,
                    Nome = e.Nome,
                    LogoUrl = e.LogoUrl,
                    TelefoneWhatsapp = e.TelefoneWhatsapp
                })
                .ToListAsync();
        }
    }
}