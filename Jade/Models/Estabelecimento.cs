// Models/Estabelecimento.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jade.Models // Seu namespace
{
    public class Estabelecimento
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(200)]
        public string? LogoUrl { get; set; }

        [StringLength(20)]
        public string? TelefoneWhatsapp { get; set; }

        public int? MarcaFranquiaId { get; set; }
        [ForeignKey("MarcaFranquiaId")]
        public virtual MarcaFranquia? MarcaFranquia { get; set; }

        public virtual ICollection<UsuarioAdmin> UsuariosAdmin { get; set; } = new List<UsuarioAdmin>();
        public virtual ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
        public virtual ICollection<Produto> Produtos { get; set; } = new List<Produto>();
        public virtual ICollection<GrupoOpcao> GruposOpcao { get; set; } = new List<GrupoOpcao>();

        // --- GARANTIR QUE ESTA COLEÇÃO EXISTA ---
        public virtual ICollection<ItemOpcao> ItensOpcao { get; set; } = new List<ItemOpcao>();

        public virtual ICollection<LojaItemConfig> LojaItemConfiguracoes { get; set; } = new List<LojaItemConfig>();
    }
}