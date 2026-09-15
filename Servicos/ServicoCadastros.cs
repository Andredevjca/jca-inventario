using JcaInventario.Helpers;
using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using JcaInventario.Repositories;
namespace JcaInventario.Servicos;

public class ServicoCadastros(Banco banco, IRepositorioCadastros repositorio, ServicoEquipamentos servicoEquipamentos) : IServicoCadastros
{
    public object Opcoes() => new { status = OpcoesInventario.Status, localizacoes = OpcoesInventario.Localizacoes, movimentacoes = OpcoesInventario.Movimentacoes, tiposTrabalho = OpcoesInventario.TiposTrabalho };
    public async Task<object> ListarAsync(string cadastro)
    {
        await using var conexao = await banco.AbrirAsync();
        return await repositorio.ListarAsync(conexao, cadastro);
    }
    public async Task<object> SalvarCadastroAsync(TipoCadastro tipo, Cadastro cadastro)
    {
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        if (cadastro.Id > 0)
        {
            if (await repositorio.ObterCadastroParaAtualizacaoAsync(conexao, tipo, cadastro.Id, transacao) == null) throw new KeyNotFoundException();
            if (!cadastro.Ativo)
            {
                if (await repositorio.ContarVinculosAtivosAsync(conexao, tipo, cadastro.Id, transacao) > 0) throw new ArgumentException("Existem registros ativos vinculados. Transfira ou inative os vínculos primeiro.");
            }
            await repositorio.AtualizarCadastroAsync(conexao, tipo, cadastro, transacao);
        }
        else cadastro.Id = await repositorio.InserirCadastroAsync(conexao, tipo, cadastro, transacao);
        await transacao.CommitAsync(); return new { cadastro.Id };
    }
    public async Task<object> SalvarFuncionarioAsync(Funcionario funcionario, int usuario)
    {
        if (!OpcoesInventario.TiposTrabalho.Contains(funcionario.TipoTrabalho)) throw new ArgumentException("Tipo de trabalho inválido.");
        funcionario.Matricula = string.IsNullOrWhiteSpace(funcionario.Matricula) ? null : funcionario.Matricula.Trim();
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        if (!await repositorio.SetorAtivoAsync(conexao, funcionario, transacao)) throw new ArgumentException("Selecione um setor ativo.");
        var anterior = funcionario.Id == 0 ? null : await repositorio.ObterFuncionarioParaAtualizacaoAsync(conexao, funcionario, transacao) ?? throw new KeyNotFoundException();
        if (!funcionario.Ativo && await repositorio.ContarEquipamentosResponsavelAsync(conexao, funcionario, transacao) > 0) throw new ArgumentException("Devolva ou transfira os equipamentos antes de inativar o funcionário.");
        if (anterior == null) funcionario.Id = await repositorio.InserirFuncionarioAsync(conexao, funcionario, transacao);
        else
        {
            if (anterior.SetorId != funcionario.SetorId)
            {
                var equipamentos = await repositorio.ListarEquipamentosResponsavelParaAtualizacaoAsync(conexao, funcionario, transacao);
                foreach (var equipamento in equipamentos)
                {
                    await repositorio.InserirTransferenciaSetorAsync(conexao, new { equipamentoId = equipamento.Id, funcionarioId = funcionario.Id, setorAnterior = anterior.SetorId, setorNovo = funcionario.SetorId, local = equipamento.Localizacao, status = equipamento.Status, usuario = usuario }, transacao);
                    await servicoEquipamentos.RegistrarHistoricoAsync(conexao, transacao, equipamento.Id, usuario, "Setor do responsável alterado", anterior, funcionario);
                }
            }
            await repositorio.AtualizarFuncionarioAsync(conexao, funcionario, transacao);
        }
        await transacao.CommitAsync(); return new { funcionario.Id };
    }
    public async Task<object> ObterFuncionarioAsync(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var funcionario = await repositorio.ObterFuncionarioAsync(conexao, new { id }) ?? throw new KeyNotFoundException();
        var equipamentos = await repositorio.ListarEquipamentosFuncionarioAsync(conexao, new { id });
        var historico = await repositorio.ListarHistoricoFuncionarioAsync(conexao, new { id });
        return new { funcionario, equipamentos, historico };
    }
    public async Task<object> ObterSetorAsync(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var setor = await repositorio.ObterSetorAsync(conexao, new { id }) ?? throw new KeyNotFoundException();
        var funcionarios = await repositorio.ListarFuncionariosSetorAsync(conexao, new { id });
        var equipamentos = await repositorio.ListarEquipamentosSetorAsync(conexao, new { id });
        return new { setor, funcionarios, equipamentos };
    }
    public async Task<object> SalvarUsuarioAsync(Usuario usuario, int usuarioAtual)
    {
        if (usuario.Id == 0 && string.IsNullOrWhiteSpace(usuario.Senha)) throw new ArgumentException("Informe uma senha com pelo menos oito caracteres.");
        if (usuario.Id == usuarioAtual && (!usuario.Ativo || !usuario.Administrador)) throw new ArgumentException("Você não pode remover seu próprio acesso administrativo.");
        await using var conexao = await banco.AbrirAsync();
        var resumo = string.IsNullOrWhiteSpace(usuario.Senha) ? null : Senhas.Gerar(usuario.Senha);
        if (usuario.Id == 0) usuario.Id = await repositorio.InserirUsuarioAsync(conexao, new { usuario.Nome, usuario.Email, resumo, usuario.Administrador, usuario.Ativo });
        else if (await repositorio.AtualizarUsuarioAsync(conexao, new { usuario.Id, usuario.Nome, usuario.Email, resumo, usuario.Administrador, usuario.Ativo }) == 0) throw new KeyNotFoundException();
        return new { usuario.Id };
    }
}
