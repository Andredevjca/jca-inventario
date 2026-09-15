using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Interfaces.Services;
using JcaInventario.Repositories;
using JcaInventario.ViewModels;

namespace JcaInventario.Servicos;

public class ServicoTermos(Banco banco, IRepositorioEquipamentos repositorio) : IServicoTermos
{
    public async Task<ArquivoGerado> GerarAsync(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var equipamento = await repositorio.ObterDetalhesAsync(conexao, id) ?? throw new KeyNotFoundException();
        if (equipamento.ResponsavelId == null) throw new ArgumentException("Vincule um funcionário para emitir o termo.");
        string[] linhas = ["JCA SOLUÇÕES", "TERMO DE RESPONSABILIDADE", "", $"Data: {DateTime.Now:dd/MM/yyyy}", $"Funcionário: {equipamento.Responsavel}", $"Setor: {equipamento.Setor}", "", $"Equipamento: {equipamento.Tipo} {equipamento.Marca} {equipamento.Modelo}", $"Patrimônio: {equipamento.NumeroPatrimonio ?? "Não informado"}", $"Número de série: {equipamento.NumeroSerie ?? "Não informado"}", $"Localização: {equipamento.Localizacao}", "", "Declaro ter recebido o equipamento descrito acima para uso profissional.", "Comprometo-me a conservar o bem e comunicar problemas, perdas ou avarias,", "bem como devolvê-lo quando solicitado pela empresa.", "", $"Observações: {equipamento.Observacoes ?? "Sem observações."}", "", "", "____________________________________________________________", "Assinatura do funcionário", "", "", "____________________________________________________________", "Assinatura do representante da JCA Soluções"];
        return new ArquivoGerado(GeradorTermo.Gerar(linhas), "application/pdf", $"termo-equipamento-{id}.pdf");
    }
}
