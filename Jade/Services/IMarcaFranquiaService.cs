// Services/IMarcaFranquiaService.cs
using Jade.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jade.Services // Seu namespace
{
    public interface IMarcaFranquiaService
    {
        Task<MarcaFranquiaVerDto?> CriarAsync(MarcaFranquiaCriarDto dto);
        Task<MarcaFranquiaVerDto?> ObterPorIdAsync(int id);
        Task<IEnumerable<MarcaFranquiaVerDto>> ObterTodasAsync();
        Task<bool> AtualizarAsync(int id, MarcaFranquiaCriarDto dto);
        Task<bool> DeletarAsync(int id);
    }
}