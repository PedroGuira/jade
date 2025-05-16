// Models/UsuarioAdmin.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jade.Models // Use o seu namespace correto
{
    public class UsuarioAdmin : IdentityUser<int>
    {
        [PersonalData]
        [Required(ErrorMessage = "Nome completo é obrigatório")] // Tornando obrigatório
        [StringLength(150)]
        public string NomeCompleto { get; set; }

        // Estabelecimento ao qual este usuário admin está diretamente vinculado (se for AdminLoja)
        // Se for AdminMarcaFranquia, este pode ser null, e ele gerencia via MarcaFranquiaIdVinculada.
        // Se for SuperAdminSistema, ambos podem ser null.
        public int? EstabelecimentoId { get; set; } // Anulável
        [ForeignKey("EstabelecimentoId")]
        public virtual Estabelecimento? Estabelecimento { get; set; } // Anulável

        // NOVO: Marca/Franquia à qual este usuário pode estar vinculado (se for AdminMarcaFranquia)
        public int? MarcaFranquiaIdVinculada { get; set; } // Anulável
        [ForeignKey("MarcaFranquiaIdVinculada")]
        public virtual MarcaFranquia? MarcaFranquiaVinculada { get; set; }

        // NOVO: Role para diferenciar o tipo de administrador
        [Required(ErrorMessage = "O perfil do usuário é obrigatório.")]
        [StringLength(50)]
        public string Role { get; set; } // Ex: "SuperAdmin", "AdminMarca", "AdminLoja"
                                         // Considerar usar o sistema de Roles do Identity no futuro para mais granularidade.
                                         // Por agora, uma string simples para diferenciar os níveis principais.
    }
}