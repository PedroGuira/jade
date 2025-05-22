// Models/MarcaFranquia.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Jade.Models // Seu namespace
{
    public class MarcaFranquia
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da marca/franquia é obrigatório.")]
        [StringLength(150)]
        public string Nome { get; set; }

        [StringLength(300)]
        public string? LogoUrl { get; set; }

        public virtual ICollection<Estabelecimento> EstabelecimentosDaMarca { get; set; } = new List<Estabelecimento>();
        public virtual ICollection<UsuarioAdmin> AdministradoresDaMarca { get; set; } = new List<UsuarioAdmin>();
        public virtual ICollection<Categoria> CategoriasTemplateDaMarca { get; set; } = new List<Categoria>();
        public virtual ICollection<Produto> ProdutosTemplateDaMarca { get; set; } = new List<Produto>();
        public virtual ICollection<GrupoOpcao> GruposOpcaoTemplateDaMarca { get; set; } = new List<GrupoOpcao>();

        // --- REMOVER SE EXISTIR ---
        // public virtual ICollection<ItemOpcao> ItensOpcaoTemplateDaMarca { get; set; } = new List<ItemOpcao>();
    }
}