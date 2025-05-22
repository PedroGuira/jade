// Dtos/AuthDto.cs (Crie esta classe em sua pasta Dtos)
using System.ComponentModel.DataAnnotations;

namespace Jade.Dtos
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Password { get; set; }
    }

    public class RegisterDto // Opcional, se for permitir registro via API
    {
        [Required]
        [StringLength(100)]
        public string NomeCompleto { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        public int? EstabelecimentoId { get; set; } // Anulável
        public int? MarcaFranquiaIdVinculada { get; set; } // Anulável
        [Required]
        public string Role { get; set; } // "AdminMarcaFranquia" ou "AdminLoja"
    }


    public class LoginResponseDto
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string NomeCompleto { get; set; }
        public string Role { get; set; }
        public int? EstabelecimentoId { get; set; }
        public int? MarcaFranquiaId { get; set; }

        // NOVAS PROPRIEDADES
        public string? NomeEstabelecimento { get; set; }
        public string? NomeMarcaFranquia { get; set; }
    }
}
