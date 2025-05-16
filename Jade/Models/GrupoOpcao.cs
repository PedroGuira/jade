using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jade.Models // Use o seu namespace correto
{
    public class GrupoOpcao
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do grupo de opção é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; } // Ex: "Sabores do Sorvete", "Tamanho da Pizza"

        [StringLength(250)]
        public string? Descricao { get; set; } // Ex: "Escolha até 2 sabores", "Apenas uma opção"

        [Required]
        [Range(0, 100, ErrorMessage = "Mínimo de seleções deve ser entre 0 e 100.")]
        public int MinSelecoes { get; set; } = 0; // 0 se for opcional, 1 se ao menos uma escolha for obrigatória

        [Required]
        [Range(1, 100, ErrorMessage = "Máximo de seleções deve ser entre 1 e 100.")]
        public int MaxSelecoes { get; set; } = 1; // Se MaxSelecoes > 1, geralmente checkboxes. Se == 1, radio/select.

        public int OrdemExibicao { get; set; } = 0; // Para ordenar os grupos no produto/cardápio

        // Futuramente: public string ModoDeExibicao { get; set; } // Ex: "Checkbox", "Radio", "Select"

        [Required]
        public int? EstabelecimentoId { get; set; }
        [ForeignKey("EstabelecimentoId")]
        public virtual Estabelecimento Estabelecimento { get; set; }

        public int? MarcaFranquiaId { get; set; }
        [ForeignKey("MarcaFranquiaId")]
        public virtual MarcaFranquia? MarcaFranquiaOriginadora { get; set; }

        public virtual ICollection<ItemOpcao> ItensOpcao { get; set; } = new List<ItemOpcao>();
        public virtual ICollection<CategoriaGrupoOpcao> CategoriasAssociadas { get; set; } = new List<CategoriaGrupoOpcao>();
    }
}