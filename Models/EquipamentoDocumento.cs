namespace JcaInventario.Models;

public class EquipamentoDocumento
{
    public int Id { get; set; }
    public int EquipamentoId { get; set; }
    public string NomeArquivo { get; set; } = "";
    public string NomeOriginal { get; set; } = "";
    public DateTime CriadoEm { get; set; }
}
