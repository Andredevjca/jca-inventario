using System.ComponentModel.DataAnnotations;

namespace JcaInventario.Models;

public class Funcionario : Cadastro
{
    [Range(1, int.MaxValue)]
    [Display(Name = "Cargo / função")]
    public int? FuncaoId { get; set; }

    [StringLength(200)]
    [Display(Name = "Endereço")]
    public string? Endereco { get; set; }

    [StringLength(20)]
    [Display(Name = "Número")]
    public string? Numero { get; set; }

    [StringLength(9)]
    [Display(Name = "CEP")]
    [RegularExpression(@"[0-9]{5}-?[0-9]{3}", ErrorMessage = "Informe um CEP com oito dígitos.")]
    public string? Cep { get; set; }

    [StringLength(120)]
    [Display(Name = "Bairro")]
    public string? Bairro { get; set; }

    [StringLength(160)]
    [Display(Name = "Cidade")]
    public string? Cidade { get; set; }

    [StringLength(2)]
    [Display(Name = "UF")]
    [RegularExpression("AC|AL|AP|AM|BA|CE|DF|ES|GO|MA|MT|MS|MG|PA|PB|PR|PE|PI|RJ|RN|RS|RO|RR|SC|SP|SE|TO", ErrorMessage = "Selecione uma UF válida.")]
    public string? Uf { get; set; }

    [StringLength(160)]
    [Display(Name = "Complemento")]
    public string? Complemento { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Data de nascimento")]
    public DateTime? DataNascimento { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Data de admissão")]
    public DateTime? DataAdmissao { get; set; }

    [EmailAddress]
    [StringLength(190)]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [StringLength(40)]
    [Display(Name = "Telefone")]
    public string? Telefone { get; set; }

    [StringLength(60)]
    [Display(Name = "Matrícula")]
    public string? Matricula { get; set; }

    [StringLength(160)]
    [Display(Name = "Cargo")]
    public string? Cargo { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Setor")]
    public int SetorId { get; set; }

    [Display(Name = "Tipo de trabalho")]
    public string TipoTrabalho { get; set; } = "Presencial";
}
