using Dapper;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Models;
using MySqlConnector;

namespace JcaInventario.Repositories;

public class RepositorioEquipamentos : IRepositorioEquipamentos
{
    public Task<IEnumerable<dynamic>> ListarAsync(MySqlConnection conexao)
        => conexao.QueryAsync(RepositorioInventario.ConsultaEquipamentos + " ORDER BY e.Id DESC");

    public Task<dynamic?> ObterDetalhesAsync(MySqlConnection conexao, int id)
        => conexao.QuerySingleOrDefaultAsync(RepositorioInventario.ConsultaEquipamentos + " WHERE e.Id=@id", new { id });

    public Task<IEnumerable<dynamic>> ListarMovimentacoesAsync(MySqlConnection conexao, int id)
        => conexao.QueryAsync(RepositorioInventario.ConsultaMovimentacoes + " WHERE m.EquipamentoId=@id ORDER BY m.Id DESC", new { id });

    public Task<IEnumerable<dynamic>> ListarManutencoesAsync(MySqlConnection conexao, int id)
        => conexao.QueryAsync("SELECT * FROM manutencoes WHERE EquipamentoId=@id ORDER BY Id DESC", new { id });

    public Task<IEnumerable<dynamic>> ListarHistoricoAsync(MySqlConnection conexao, int id)
        => conexao.QueryAsync("SELECT h.*,u.Nome Usuario FROM historico h JOIN usuarios u ON u.Id=h.UsuarioId WHERE EquipamentoId=@id ORDER BY h.Id DESC", new { id });

    public Task<int> AtualizarFotoAsync(MySqlConnection conexao, int id, string? nome, MySqlTransaction transacao)
        => conexao.ExecuteAsync("UPDATE equipamentos SET Foto=@nome,AtualizadoEm=CURRENT_TIMESTAMP WHERE Id=@id", new { nome, id }, transacao);

    public Task<string?> ObterFotoAsync(MySqlConnection conexao, int id)
        => conexao.ExecuteScalarAsync<string?>("SELECT Foto FROM equipamentos WHERE Id=@id", new { id });

    public Task<int> InserirHistoricoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("INSERT INTO historico (EquipamentoId,UsuarioId,Descricao,DadosAnteriores,DadosNovos) VALUES (@id,@usuario,@descricao,@antes,@depois)", parametros, transacao);

    public Task<bool> ResponsavelAtivoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM funcionarios f JOIN setores s ON s.Id=f.SetorId WHERE f.Id=@Id AND f.Ativo=1 AND s.Ativo=1)", parametros, transacao);

    public Task<bool> TipoAtivoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM tipos_equipamento WHERE Id=@TipoId AND Ativo=1)", parametros, transacao);

    public Task<int?> ObterSetorResponsavelAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int?>("SELECT SetorId FROM funcionarios WHERE Id=@Id", parametros, transacao);

    public Task<int> InserirMovimentacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("""
            INSERT INTO movimentacoes (EquipamentoId,ResponsavelAnteriorId,NovoResponsavelId,SetorAnteriorId,NovoSetorId,LocalizacaoAnterior,NovaLocalizacao,StatusAnterior,NovoStatus,Tipo,UsuarioId,Observacao)
            VALUES (@Id,@anteriorId,@novoId,@setorAnterior,@novoSetor,@localAnterior,@localNovo,@statusAnterior,@statusNovo,@tipo,@usuario,@observacao)
            """, parametros, transacao);

    public Task<Equipamento> ObterParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync<Equipamento>("SELECT * FROM equipamentos WHERE Id=@Id FOR UPDATE", parametros, transacao);

    public Task<int> InserirAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("""
                INSERT INTO equipamentos (TipoId,Marca,Modelo,NumeroPatrimonio,NumeroSerie,DataAquisicao,ValorAquisicao,Status,Localizacao,ResponsavelId,Observacoes,Processador,MemoriaRam,Armazenamento,SistemaOperacional)
                VALUES (@TipoId,@Marca,@Modelo,@NumeroPatrimonio,@NumeroSerie,@DataAquisicao,@ValorAquisicao,@Status,@Localizacao,@ResponsavelId,@Observacoes,@Processador,@MemoriaRam,@Armazenamento,@SistemaOperacional); SELECT LAST_INSERT_ID();
                """, parametros, transacao);

    public Task<int> AtualizarAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("""
            UPDATE equipamentos SET TipoId=@TipoId,Marca=@Marca,Modelo=@Modelo,NumeroPatrimonio=@NumeroPatrimonio,NumeroSerie=@NumeroSerie,DataAquisicao=@DataAquisicao,ValorAquisicao=@ValorAquisicao,Status=@Status,Localizacao=@Localizacao,ResponsavelId=@ResponsavelId,Observacoes=@Observacoes,Processador=@Processador,MemoriaRam=@MemoriaRam,Armazenamento=@Armazenamento,SistemaOperacional=@SistemaOperacional,AtualizadoEm=CURRENT_TIMESTAMP WHERE Id=@Id
            """, parametros, transacao);

    public Task<Equipamento> ObterParaMovimentacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync<Equipamento>("SELECT * FROM equipamentos WHERE Id=@id FOR UPDATE", parametros, transacao);

    public Task<int> AtualizarSituacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("UPDATE equipamentos SET ResponsavelId=@ResponsavelId,Localizacao=@Localizacao,Status=@Status,AtualizadoEm=CURRENT_TIMESTAMP WHERE Id=@Id", parametros, transacao);

    public Task<bool> PossuiManutencaoAbertaAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM manutencoes WHERE EquipamentoId=@EquipamentoId AND DataSaida IS NULL)", parametros, transacao);

    public Task<int> InserirManutencaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("INSERT INTO manutencoes (EquipamentoId,DataEntrada,DataSaida,Tipo,Problema,Solucao,Valor,Responsavel,Observacoes,UsuarioId) VALUES (@EquipamentoId,@DataEntrada,@DataSaida,@Tipo,@Problema,@Solucao,@Valor,@Responsavel,@Observacoes,@usuario); SELECT LAST_INSERT_ID()", parametros, transacao);

    public Task<Manutencao> ObterManutencaoParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync<Manutencao>("SELECT * FROM manutencoes WHERE Id=@Id AND EquipamentoId=@EquipamentoId FOR UPDATE", parametros, transacao);

    public Task<int> AtualizarManutencaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("UPDATE manutencoes SET DataEntrada=@DataEntrada,DataSaida=@DataSaida,Tipo=@Tipo,Problema=@Problema,Solucao=@Solucao,Valor=@Valor,Responsavel=@Responsavel,Observacoes=@Observacoes WHERE Id=@Id", parametros, transacao);
}
