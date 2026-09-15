using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Models;
using JcaInventario.Repositories;
using System.Text.Json;

namespace JcaInventario.Servicos;

public class ServicoManutencoes(Banco banco, IRepositorioEquipamentos repositorio, ServicoEquipamentos servicoEquipamentos) : JcaInventario.Interfaces.Services.IServicoManutencoes
{
    public async Task<int> SalvarAsync(Manutencao manutencao, int usuario)
    {
        if (manutencao.DataEntrada == default || manutencao.DataSaida < manutencao.DataEntrada) throw new ArgumentException("As datas da manutenção são inválidas.");
        if (manutencao.DataSaida.HasValue && string.IsNullOrWhiteSpace(manutencao.Solucao)) throw new ArgumentException("Informe a solução antes de finalizar a manutenção.");
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        var anterior = await repositorio.ObterParaAtualizacaoAsync(conexao, new { Id = manutencao.EquipamentoId }, transacao) ?? throw new KeyNotFoundException();
        var novo = JsonSerializer.Deserialize<Equipamento>(JsonSerializer.Serialize(anterior))!;
        if (manutencao.Id == 0)
        {
            if (anterior.Status is "Baixado" or "Inativo") throw new ArgumentException("Reative o equipamento antes de registrar manutenção.");
            if (await repositorio.PossuiManutencaoAbertaAsync(conexao, manutencao, transacao)) throw new ArgumentException("O equipamento já possui manutenção aberta.");
            manutencao.Id = await repositorio.InserirManutencaoAsync(conexao, new { manutencao.EquipamentoId, manutencao.DataEntrada, manutencao.DataSaida, manutencao.Tipo, manutencao.Problema, manutencao.Solucao, manutencao.Valor, manutencao.Responsavel, manutencao.Observacoes, usuario }, transacao);
            novo.Status = "Em manutenção"; novo.Localizacao = "Manutenção";
            await servicoEquipamentos.RegistrarMovimentacaoAsync(conexao, transacao, anterior, novo, usuario, "Envio para manutenção", manutencao.Problema);
        }
        else
        {
            var existente = await repositorio.ObterManutencaoParaAtualizacaoAsync(conexao, manutencao, transacao) ?? throw new KeyNotFoundException();
            if (existente.DataSaida.HasValue) throw new ArgumentException("Manutenção encerrada não pode ser alterada.");
            await repositorio.AtualizarManutencaoAsync(conexao, manutencao, transacao);
            await servicoEquipamentos.RegistrarHistoricoAsync(conexao, transacao, anterior.Id, usuario, "Manutenção atualizada", existente, manutencao);
        }
        if (manutencao.DataSaida.HasValue)
        {
            var emManutencao = JsonSerializer.Deserialize<Equipamento>(JsonSerializer.Serialize(novo))!;
            novo.Status = "Em estoque"; novo.Localizacao = "Estoque"; novo.ResponsavelId = null;
            await servicoEquipamentos.RegistrarMovimentacaoAsync(conexao, transacao, emManutencao, novo, usuario, "Retorno da manutenção", manutencao.Solucao);
        }
        await repositorio.AtualizarSituacaoAsync(conexao, novo, transacao);
        await servicoEquipamentos.RegistrarHistoricoAsync(conexao, transacao, anterior.Id, usuario, manutencao.DataSaida.HasValue ? "Manutenção finalizada" : "Manutenção registrada", anterior, manutencao);
        await transacao.CommitAsync();
        return manutencao.Id;
    }
}
