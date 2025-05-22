// Models/ItemOpcao.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jade.Models // Seu namespace
{
    public class ItemOpcao
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do item de opção é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecoAdicional { get; set; } = 0.00m;

        public bool Disponivel { get; set; } = true;

        public int OrdemExibicao { get; set; } = 0;

        [Required]
        public int GrupoOpcaoId { get; set; }
        [ForeignKey("GrupoOpcaoId")]
        public virtual GrupoOpcao GrupoOpcao { get; set; }

        // Este EstabelecimentoId indica a qual loja um ItemOpcao LOCAL pertence.
        // Se o GrupoOpcao pai é um template da franquia, este EstabelecimentoId será null.
        public int? EstabelecimentoId { get; set; } // ANULÁVEL
        [ForeignKey("EstabelecimentoId")]
        public virtual Estabelecimento? Estabelecimento { get; set; } // ANULÁVEL

        // --- REMOVER AS SEGUINTES LINHAS ---
        // public int? MarcaFranquiaId { get; set; }
        // [ForeignKey("MarcaFranquiaId")]
        // public virtual MarcaFranquia? MarcaFranquiaOriginadora { get; set; }
    }
}