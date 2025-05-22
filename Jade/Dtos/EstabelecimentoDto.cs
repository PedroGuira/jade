// Dtos/EstabelecimentoDto.cs
using System.ComponentModel.DataAnnotations;

namespace Jade.Dtos // Seu namespace
{
    public class EstabelecimentoCriarDto
    {
        [Required(ErrorMessage = "O nome do estabelecimento é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(200)]
        public string? LogoUrl { get; set; } // Tornando opcional

        [StringLength(20)]
        public string? TelefoneWhatsapp { get; set; } // Tornando opcional
    }

    public class EstabelecimentoVerDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string? LogoUrl { get; set; }
        public string? TelefoneWhatsapp { get; set; }
    }
}