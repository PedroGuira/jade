// Dtos/MarcaFranquiaDto.cs
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Se for retornar DTOs de estabelecimentos/usuários no futuro

namespace Jade.Dtos // Seu namespace
{
    public class MarcaFranquiaCriarDto
    {
        [Required(ErrorMessage = "O nome da marca/franquia é obrigatório.")]
        [StringLength(150)]
        public string Nome { get; set; }

        [StringLength(300)]
        public string? LogoUrl { get; set; }
    }

    public class MarcaFranquiaVerDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string? LogoUrl { get; set; }
        // Poderia adicionar contagem de estabelecimentos, etc. no futuro
        // public int QuantidadeEstabelecimentos { get; set; }
    }
}