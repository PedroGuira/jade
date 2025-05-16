// Models/Categoria.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jade.Models // Use o seu namespace correto
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        public int OrdemExibicao { get; set; } = 0;

        public int? EstabelecimentoId { get; set; }
        [ForeignKey("EstabelecimentoId")]
        public virtual Estabelecimento Estabelecimento { get; set; }

        public int? MarcaFranquiaId { get; set; } // Anulável
        [ForeignKey("MarcaFranquiaId")]
        public virtual MarcaFranquia? MarcaFranquiaOriginadora { get; set; }

        public virtual ICollection<Produto> Produtos { get; set; } = new List<Produto>();

        // NOVA PROPRIEDADE DE NAVEGAÇÃO
        public virtual ICollection<CategoriaGrupoOpcao> GruposDeOpcaoAssociados { get; set; } = new List<CategoriaGrupoOpcao>();
    }
}