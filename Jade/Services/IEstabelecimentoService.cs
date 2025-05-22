// Services/IEstabelecimentoService.cs
using Jade.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jade.Services // Seu namespace
{
    public interface IEstabelecimentoService
    {
        Task<EstabelecimentoVerDto?> CriarAsync(EstabelecimentoCriarDto dto);
        Task<EstabelecimentoVerDto?> ObterPorIdAsync(int id);
        Task<IEnumerable<EstabelecimentoVerDto>> ObterTodosAsync();
        // Adicionar Update e Delete depois, se necessário para "super admin"
    }
}