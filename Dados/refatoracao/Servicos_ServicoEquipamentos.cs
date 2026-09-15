using Dapper;
using JcaInventario.Infraestrutura;
using JcaInventario.Modelos;
using JcaInventario.DTOs;
using JcaInventario.Configuracoes;
using MySqlConnector;
using System.Text.Json;
namespace JcaInventario.Servicos;

public class ServicoEquipamentos(Banco banco)
{
    public static async Task RegistrarHistoricoAsync(MySqlConnection conexao, MySqlTransaction transacao, int id, int usuario, string descricao, object? anterior, object? novo)
        => await conexao.ExecuteAsync("INSERT INTO historico (EquipamentoId,UsuarioId,Descricao,DadosAnteriores,DadosNovos) VALUES (@id,@usuario,@descricao,@antes,@depois)", new { id, usuario, descricao, antes = anterior == null ? null : JsonSerializer.Serialize(anterior), depois = novo == null ? null : JsonSerializer.Serialize(novo) }, transacao);

    private static async Task ValidarAsync(MySqlConnection conexao, MySqlTransaction transacao, Equipamento equipamento)
    {
        if (!OpcoesInventario.Status.Contains(equipamento.Status) || !OpcoesInventario.Localizacoes.Contains(equipamento.Localizacao)) throw new ArgumentException("Status ou localização inválidos.");
        if (equipamento.ValorAquisicao < 0) throw new ArgumentException("O valor não pode ser negativo.");
        if (equipamento.ResponsavelId.HasValue && !await conexao.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM funcionarios f JOIN setores s ON s.Id=f.SetorId WHERE f.Id=@Id AND f.Ativo=1 AND s.Ativo=1)", new { Id = equipamento.ResponsavelId }, transacao)) throw new ArgumentException("Selecione um funcionário ativo em um setor ativo.");
        if (!await conexao.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM tipos_equipamento WHERE Id=@TipoId AND Ativo=1)", equipamento, transacao)) throw new ArgumentException("Selecione um tipo de equipamento ativo.");
        if (equipamento.Status is "Em uso" or "Home Office" && equipamento.ResponsavelId == null) throw new ArgumentException("Equipamentos em uso precisam de um responsável.");
        if (equipamento.Status is "Disponível" or "Em estoque" or "Baixado" or "Inativo" && equipamento.ResponsavelId != null) throw new ArgumentException("Retire o responsável antes de disponibilizar, inativar ou baixar o equipamento.");
        equipamento.NumeroPatrimonio = string.IsNullOrWhiteSpace(equipamento.NumeroPatrimonio) ? null : equipamento.NumeroPatrimonio.Trim();
        equipamento.NumeroSerie = string.IsNullOrWhiteSpace(equipamento.NumeroSerie) ? null : equipamento.NumeroSerie.Trim();
    }

    public static async Task RegistrarMovimentacaoAsync(MySqlConnection conexao, MySqlTransaction transacao, Equipamento? anterior, Equipamento novo, int usuario, string tipo, string? observacao)
    {
        var setorAnterior = anterior?.ResponsavelId == null ? (int?)null : await conexao.ExecuteScalarAsync<int?>("SELECT SetorId FROM funcionarios WHERE Id=@Id", new { Id = anterior.ResponsavelId }, transacao);
        var novoSetor = novo.ResponsavelId == null ? (int?)null : await conexao.ExecuteScalarAsync<int?>("SELECT SetorId FROM funcionarios WHERE Id=@Id", new { Id = novo.ResponsavelId }, transacao);
        await conexao.ExecuteAsync("""
            INSERT INTO movimentacoes (EquipamentoId,ResponsavelAnteriorId,NovoResponsavelId,SetorAnteriorId,NovoSetorId,LocalizacaoAnterior,NovaLocalizacao,StatusAnterior,NovoStatus,Tipo,UsuarioId,Observacao)
            VALUES (@Id,@anteriorId,@novoId,@setorAnterior,@novoSetor,@localAnterior,@localNovo,@statusAnterior,@statusNovo,@tipo,@usuario,@observacao)
            """, new { novo.Id, anteriorId = anterior?.ResponsavelId, novoId = novo.ResponsavelId, setorAnterior, novoSetor, localAnterior = anterior?.Localizacao, localNovo = novo.Localizacao, statusAnterior = anterior?.Status, statusNovo = novo.Status, tipo, usuario, observacao }, transacao);
    }

    public async Task<int> SalvarAsync(Equipamento equipamento, int usuario)
    {
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        var anterior = equipamento.Id == 0 ? null : await conexao.QuerySingleOrDefaultAsync<Equipamento>("SELECT * FROM equipamentos WHERE Id=@Id FOR UPDATE", equipamento, transacao) ?? throw new KeyNotFoundException();
        await ValidarAsync(conexao, transacao, equipamento);
        if (equipamento.Status == "Em manutenção" && anterior?.Status != "Em manutenção") throw new ArgumentException("Utilize o cadastro de manutenção para enviar o equipamento.");
        if (anterior?.Status == "Em manutenção" && (equipamento.Status != anterior.Status || equipamento.ResponsavelId != anterior.ResponsavelId || equipamento.Localizacao != anterior.Localizacao)) throw new ArgumentException("Finalize a manutenção antes de movimentar o equipamento.");
        equipamento.Foto = anterior?.Foto;
        if (anterior == null) {
            equipamento.Id = await conexao.ExecuteScalarAsync<int>("""
                INSERT INTO equipamentos (TipoId,Marca,Modelo,NumeroPatrimonio,NumeroSerie,DataAquisicao,ValorAquisicao,Status,Localizacao,ResponsavelId,Observacoes,Processador,MemoriaRam,Armazenamento,SistemaOperacional)
                VALUES (@TipoId,@Marca,@Modelo,@NumeroPatrimonio,@NumeroSerie,@DataAquisicao,@ValorAquisicao,@Status,@Localizacao,@ResponsavelId,@Observacoes,@Processador,@MemoriaRam,@Armazenamento,@SistemaOperacional); SELECT LAST_INSERT_ID();
                """, equipamento, transacao);
        } else await conexao.ExecuteAsync("""
            UPDATE equipamentos SET TipoId=@TipoId,Marca=@Marca,Modelo=@Modelo,NumeroPatrimonio=@NumeroPatrimonio,NumeroSerie=@NumeroSerie,DataAquisicao=@DataAquisicao,ValorAquisicao=@ValorAquisicao,Status=@Status,Localizacao=@Localizacao,ResponsavelId=@ResponsavelId,Observacoes=@Observacoes,Processador=@Processador,MemoriaRam=@MemoriaRam,Armazenamento=@Armazenamento,SistemaOperacional=@SistemaOperacional,AtualizadoEm=CURRENT_TIMESTAMP WHERE Id=@Id
            """, equipamento, transacao);
        if (anterior == null || anterior.ResponsavelId != equipamento.ResponsavelId || anterior.Localizacao != equipamento.Localizacao || anterior.Status != equipamento.Status)
            await RegistrarMovimentacaoAsync(conexao, transacao, anterior, equipamento, usuario, anterior == null ? "Cadastro" : anterior.ResponsavelId != equipamento.ResponsavelId ? (equipamento.ResponsavelId == null ? "Devolução" : anterior.ResponsavelId == null ? "Entrega" : "Transferência") : "Alteração de localização/status", equipamento.Observacoes);
        await RegistrarHistoricoAsync(conexao, transacao, equipamento.Id, usuario, anterior == null ? "Equipamento cadastrado" : "Cadastro atualizado", anterior, equipamento);
        await transacao.CommitAsync();
        return equipamento.Id;
    }

    public async Task MovimentarAsync(int id, Movimentacao movimento, int usuario)
    {
        if (!OpcoesInventario.Movimentacoes.Contains(movimento.Tipo)) throw new ArgumentException("Tipo de movimentação inválido.");
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        var anterior = await conexao.QuerySingleOrDefaultAsync<Equipamento>("SELECT * FROM equipamentos WHERE Id=@id FOR UPDATE", new { id }, transacao) ?? throw new KeyNotFoundException();
        var novo = JsonSerializer.Deserialize<Equipamento>(JsonSerializer.Serialize(anterior))!;
        if (anterior.Status == "Em manutenção" || movimento.Status == "Em manutenção") throw new ArgumentException("Envie e retorne pela tela de manutenções.");
        novo.ResponsavelId = movimento.ResponsavelId; novo.Localizacao = movimento.Localizacao; novo.Status = movimento.Status;
        if (movimento.Tipo == "Entrega" && (anterior.ResponsavelId != null || novo.ResponsavelId == null)) throw new ArgumentException("Entrega exige equipamento sem responsável e um novo responsável.");
        if (movimento.Tipo == "Transferência" && (anterior.ResponsavelId == null || novo.ResponsavelId == null || anterior.ResponsavelId == novo.ResponsavelId)) throw new ArgumentException("Selecione um responsável diferente para transferir.");
        if (movimento.Tipo == "Devolução" && (anterior.ResponsavelId == null || novo.ResponsavelId != null)) throw new ArgumentException("Devolução deve retirar o responsável atual.");
        if (movimento.Tipo == "Transferência para estoque" && (novo.Localizacao != "Estoque" || novo.Status != "Em estoque" || novo.ResponsavelId != null)) throw new ArgumentException("Transferência para estoque exige status Em estoque, localização Estoque e nenhum responsável.");
        if (movimento.Tipo == "Alteração de localização" && novo.ResponsavelId != anterior.ResponsavelId) throw new ArgumentException("Use entrega, devolução ou transferência para alterar o responsável.");
        await ValidarAsync(conexao, transacao, novo);
        await conexao.ExecuteAsync("UPDATE equipamentos SET ResponsavelId=@ResponsavelId,Localizacao=@Localizacao,Status=@Status,AtualizadoEm=CURRENT_TIMESTAMP WHERE Id=@Id", novo, transacao);
        await RegistrarMovimentacaoAsync(conexao, transacao, anterior, novo, usuario, movimento.Tipo, movimento.Observacao);
        await RegistrarHistoricoAsync(conexao, transacao, id, usuario, movimento.Tipo, anterior, novo);
        await transacao.CommitAsync();
    }

    public async Task<int> SalvarManutencaoAsync(Manutencao manutencao, int usuario)
    {
        if (manutencao.DataEntrada == default || manutencao.DataSaida < manutencao.DataEntrada) throw new ArgumentException("As datas da manutenção são inválidas.");
        if (manutencao.DataSaida.HasValue && string.IsNullOrWhiteSpace(manutencao.Solucao)) throw new ArgumentException("Informe a solução antes de finalizar a manutenção.");
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        var anterior = await conexao.QuerySingleOrDefaultAsync<Equipamento>("SELECT * FROM equipamentos WHERE Id=@Id FOR UPDATE", new { Id = manutencao.EquipamentoId }, transacao) ?? throw new KeyNotFoundException();
        var novo = JsonSerializer.Deserialize<Equipamento>(JsonSerializer.Serialize(anterior))!;
        if (manutencao.Id == 0) {
            if (anterior.Status is "Baixado" or "Inativo") throw new ArgumentException("Reative o equipamento antes de registrar manutenção.");
            if (await conexao.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM manutencoes WHERE EquipamentoId=@EquipamentoId AND DataSaida IS NULL)", manutencao, transacao)) throw new ArgumentException("O equipamento já possui manutenção aberta.");
            manutencao.Id = await conexao.ExecuteScalarAsync<int>("INSERT INTO manutencoes (EquipamentoId,DataEntrada,DataSaida,Tipo,Problema,Solucao,Valor,Responsavel,Observacoes,UsuarioId) VALUES (@EquipamentoId,@DataEntrada,@DataSaida,@Tipo,@Problema,@Solucao,@Valor,@Responsavel,@Observacoes,@usuario); SELECT LAST_INSERT_ID()", new { manutencao.EquipamentoId, manutencao.DataEntrada, manutencao.DataSaida, manutencao.Tipo, manutencao.Problema, manutencao.Solucao, manutencao.Valor, manutencao.Responsavel, manutencao.Observacoes, usuario }, transacao);
            novo.Status = "Em manutenção"; novo.Localizacao = "Manutenção";
            await RegistrarMovimentacaoAsync(conexao, transacao, anterior, novo, usuario, "Envio para manutenção", manutencao.Problema);
        } else {
            var existente = await conexao.QuerySingleOrDefaultAsync<Manutencao>("SELECT * FROM manutencoes WHERE Id=@Id AND EquipamentoId=@EquipamentoId FOR UPDATE", manutencao, transacao) ?? throw new KeyNotFoundException();
            if (existente.DataSaida.HasValue) throw new ArgumentException("Manutenção encerrada não pode ser alterada.");
            await conexao.ExecuteAsync("UPDATE manutencoes SET DataEntrada=@DataEntrada,DataSaida=@DataSaida,Tipo=@Tipo,Problema=@Problema,Solucao=@Solucao,Valor=@Valor,Responsavel=@Responsavel,Observacoes=@Observacoes WHERE Id=@Id", manutencao, transacao);
            await RegistrarHistoricoAsync(conexao, transacao, anterior.Id, usuario, "Manutenção atualizada", existente, manutencao);
        }
        if (manutencao.DataSaida.HasValue) {
            var emManutencao = JsonSerializer.Deserialize<Equipamento>(JsonSerializer.Serialize(novo))!;
            novo.Status = "Em estoque"; novo.Localizacao = "Estoque"; novo.ResponsavelId = null;
            await RegistrarMovimentacaoAsync(conexao, transacao, emManutencao, novo, usuario, "Retorno da manutenção", manutencao.Solucao);
        }
        await conexao.ExecuteAsync("UPDATE equipamentos SET Status=@Status,Localizacao=@Localizacao,ResponsavelId=@ResponsavelId,AtualizadoEm=CURRENT_TIMESTAMP WHERE Id=@Id", novo, transacao);
        await RegistrarHistoricoAsync(conexao, transacao, anterior.Id, usuario, manutencao.DataSaida.HasValue ? "Manutenção finalizada" : "Manutenção registrada", anterior, manutencao);
        await transacao.CommitAsync();
        return manutencao.Id;
    }
}
