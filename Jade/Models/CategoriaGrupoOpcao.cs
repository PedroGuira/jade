// Models/CategoriaGrupoOpcao.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace Jade.Models // Use o seu namespace correto
{
    public class CategoriaGrupoOpcao
    {
        public int CategoriaId { get; set; }
        [ForeignKey("CategoriaId")]
        public virtual Categoria Categoria { get; set; }

        public int GrupoOpcaoId { get; set; }
        [ForeignKey("GrupoOpcaoId")]
        public virtual GrupoOpcao GrupoOpcao { get; set; }

        public int OrdemExibicaoGrupoNaCategoria { get; set; } = 0; // Ordem deste grupo dentro desta categoria específica
    }
}