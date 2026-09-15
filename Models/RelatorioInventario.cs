namespace JcaInventario.Models;

public sealed record RelatorioInventario(int Id, string Nome, DateTime Data, bool Encerrado,
    IReadOnlyList<ItemRelatorioInventario> Itens);

public sealed class ItemRelatorioInventario
{
    public int EquipamentoId { get; set; }
    public string? NumeroPatrimonio { get; set; }
    public string? NumeroSerie { get; set; }
    public string? Tipo { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Responsavel { get; set; }
    public string? Setor { get; set; }
    public string? Localizacao { get; set; }
    public string? Status { get; set; }
    public string Situacao { get; set; } = "Pendente";
    public string? Observacao { get; set; }
    public string? Usuario { get; set; }
    public DateTime? Data { get; set; }
    public DateTime CapturadoEm { get; set; }
    public bool CapturadoNaCriacao { get; set; }
}
