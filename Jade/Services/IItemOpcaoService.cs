// Services/IItemOpcaoService.cs
using Jade.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jade.Services
{
    public interface IItemOpcaoService
    {
        Task<ItemOpcaoVerDto?> CriarAsync(ItemOpcaoCriarDto dto, int grupoOpcaoId,
                                          int? userEstabelecimentoId, string userRole, int? userMarcaFranquiaId);

        Task<IEnumerable<ItemOpcaoVerDto>> ObterTodosPorGrupoAsync(int grupoOpcaoId,
                                                                  int? userEstabelecimentoId, string userRole, int? userMarcaFranquiaId);

        Task<ItemOpcaoVerDto?> ObterPorIdAsync(int itemId, int grupoOpcaoId,
                                              int? userEstabelecimentoId, string userRole, int? userMarcaFranquiaId);

        Task<bool> AtualizarAsync(int itemId, ItemOpcaoCriarDto dto, int grupoOpcaoId,
                                  int? userEstabelecimentoId, string userRole, int? userMarcaFranquiaId);

        Task<bool> DeletarAsync(int itemId, int grupoOpcaoId,
                                int? userEstabelecimentoId, string userRole, int? userMarcaFranquiaId);
    }
}