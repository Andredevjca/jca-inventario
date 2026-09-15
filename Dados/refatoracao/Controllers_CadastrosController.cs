using Dapper;
using JcaInventario.Infraestrutura;
using JcaInventario.Modelos;
using JcaInventario.Configuracoes;
using JcaInventario.Repositorios;
using JcaInventario.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace JcaInventario.Controllers;

[ApiController, Authorize, Route("api/cadastros")]
public class CadastrosController(Banco banco) : ControllerBase
{
    private int UsuarioAtual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("opcoes")]
    public object Opcoes() => new { status = OpcoesInventario.Status, localizacoes = OpcoesInventario.Localizacoes, movimentacoes = OpcoesInventario.Movimentacoes, tiposTrabalho = OpcoesInventario.TiposTrabalho };
    [HttpGet("{cadastro}")]
    public async Task<IActionResult> Listar(string cadastro)
    {
        if (cadastro == "usuarios" && !User.IsInRole("Administrador")) return Forbid();
        var consulta = cadastro switch {
            "setores" => "SELECT * FROM setores ORDER BY Nome",
            "tipos" => "SELECT * FROM tipos_equipamento ORDER BY Nome",
            "funcionarios" => "SELECT f.*,s.Nome Setor FROM funcionarios f JOIN setores s ON s.Id=f.SetorId ORDER BY f.Nome",
            "usuarios" => "SELECT Id,Nome,Email,Administrador,Ativo FROM usuarios ORDER BY Nome",
            _ => throw new KeyNotFoundException()
        };
        await using var conexao = await banco.AbrirAsync();
        return Ok(await conexao.QueryAsync(consulta));
    }
    [HttpPost("setores"), HttpPost("tipos")]
    public async Task<object> SalvarCadastro(Cadastro cadastro)
    {
        var tabela = Request.Path.Value!.EndsWith("setores") ? "setores" : "tipos_equipamento";
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        if (cadastro.Id > 0) {
            if (await conexao.QuerySingleOrDefaultAsync($"SELECT Id FROM {tabela} WHERE Id=@Id FOR UPDATE", cadastro, transacao) == null) throw new KeyNotFoundException();
            if (!cadastro.Ativo) {
                var vinculos = tabela == "setores" ? "SELECT COUNT(*) FROM funcionarios WHERE SetorId=@Id AND Ativo=1" : "SELECT COUNT(*) FROM equipamentos WHERE TipoId=@Id AND Status NOT IN ('Inativo','Baixado')";
                if (await conexao.ExecuteScalarAsync<int>(vinculos, cadastro, transacao) > 0) throw new ArgumentException("Existem registros ativos vinculados. Transfira ou inative os vínculos primeiro.");
            }
            await conexao.ExecuteAsync($"UPDATE {tabela} SET Nome=@Nome,Ativo=@Ativo,Observacoes=@Observacoes WHERE Id=@Id", cadastro, transacao);
        } else cadastro.Id = await conexao.ExecuteScalarAsync<int>($"INSERT INTO {tabela} (Nome,Ativo,Observacoes) VALUES (@Nome,@Ativo,@Observacoes); SELECT LAST_INSERT_ID()", cadastro, transacao);
        await transacao.CommitAsync(); return new { cadastro.Id };
    }
    [HttpPost("funcionarios")]
    public async Task<object> SalvarFuncionario(Funcionario funcionario)
    {
        if (!OpcoesInventario.TiposTrabalho.Contains(funcionario.TipoTrabalho)) throw new ArgumentException("Tipo de trabalho inválido.");
        funcionario.Matricula = string.IsNullOrWhiteSpace(funcionario.Matricula) ? null : funcionario.Matricula.Trim();
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = await conexao.BeginTransactionAsync();
        if (!await conexao.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM setores WHERE Id=@SetorId AND Ativo=1)", funcionario, transacao)) throw new ArgumentException("Selecione um setor ativo.");
        var anterior = funcionario.Id == 0 ? null : await conexao.QuerySingleOrDefaultAsync<Funcionario>("SELECT * FROM funcionarios WHERE Id=@Id FOR UPDATE", funcionario, transacao) ?? throw new KeyNotFoundException();
        if (!funcionario.Ativo && await conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM equipamentos WHERE ResponsavelId=@Id", funcionario, transacao) > 0) throw new ArgumentException("Devolva ou transfira os equipamentos antes de inativar o funcionário.");
        if (anterior == null) funcionario.Id = await conexao.ExecuteScalarAsync<int>("INSERT INTO funcionarios (Nome,Email,Telefone,Matricula,Cargo,SetorId,TipoTrabalho,Ativo,Observacoes) VALUES (@Nome,@Email,@Telefone,@Matricula,@Cargo,@SetorId,@TipoTrabalho,@Ativo,@Observacoes); SELECT LAST_INSERT_ID()", funcionario, transacao);
        else {
            if (anterior.SetorId != funcionario.SetorId) {
                var equipamentos = await conexao.QueryAsync<Equipamento>("SELECT * FROM equipamentos WHERE ResponsavelId=@Id FOR UPDATE", funcionario, transacao);
                foreach (var equipamento in equipamentos) {
                    await conexao.ExecuteAsync("""
                        INSERT INTO movimentacoes (EquipamentoId,ResponsavelAnteriorId,NovoResponsavelId,SetorAnteriorId,NovoSetorId,LocalizacaoAnterior,NovaLocalizacao,StatusAnterior,NovoStatus,Tipo,UsuarioId,Observacao)
                        VALUES (@equipamentoId,@funcionarioId,@funcionarioId,@setorAnterior,@setorNovo,@local,@local,@status,@status,'Transferência de setor',@usuario,'Alteração do setor do funcionário')
                        """, new { equipamentoId = equipamento.Id, funcionarioId = funcionario.Id, setorAnterior = anterior.SetorId, setorNovo = funcionario.SetorId, local = equipamento.Localizacao, status = equipamento.Status, usuario = UsuarioAtual }, transacao);
                    await ServicoEquipamentos.RegistrarHistoricoAsync(conexao, transacao, equipamento.Id, UsuarioAtual, "Setor do responsável alterado", anterior, funcionario);
                }
            }
            await conexao.ExecuteAsync("UPDATE funcionarios SET Nome=@Nome,Email=@Email,Telefone=@Telefone,Matricula=@Matricula,Cargo=@Cargo,SetorId=@SetorId,TipoTrabalho=@TipoTrabalho,Ativo=@Ativo,Observacoes=@Observacoes WHERE Id=@Id", funcionario, transacao);
        }
        await transacao.CommitAsync(); return new { funcionario.Id };
    }
    [HttpGet("funcionarios/{id:int}")]
    public async Task<object> FuncionarioDetalhes(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var funcionario = await conexao.QuerySingleOrDefaultAsync("SELECT f.*,s.Nome Setor FROM funcionarios f JOIN setores s ON s.Id=f.SetorId WHERE f.Id=@id", new { id }) ?? throw new KeyNotFoundException();
        var equipamentos = await conexao.QueryAsync(RepositorioInventario.ConsultaEquipamentos + " WHERE e.ResponsavelId=@id", new { id });
        var historico = await conexao.QueryAsync(RepositorioInventario.ConsultaMovimentacoes + " WHERE m.ResponsavelAnteriorId=@id OR m.NovoResponsavelId=@id ORDER BY m.Id DESC", new { id });
        return new { funcionario, equipamentos, historico };
    }
    [HttpGet("setores/{id:int}")]
    public async Task<object> SetorDetalhes(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var setor = await conexao.QuerySingleOrDefaultAsync("SELECT * FROM setores WHERE Id=@id", new { id }) ?? throw new KeyNotFoundException();
        var funcionarios = await conexao.QueryAsync("SELECT * FROM funcionarios WHERE SetorId=@id", new { id });
        var equipamentos = await conexao.QueryAsync(RepositorioInventario.ConsultaEquipamentos + " WHERE f.SetorId=@id", new { id });
        return new { setor, funcionarios, equipamentos };
    }
    [Authorize(Roles = "Administrador"), HttpPost("usuarios")]
    public async Task<object> SalvarUsuario(Usuario usuario)
    {
        if (usuario.Id == 0 && string.IsNullOrWhiteSpace(usuario.Senha)) throw new ArgumentException("Informe uma senha com pelo menos oito caracteres.");
        if (usuario.Id == UsuarioAtual && (!usuario.Ativo || !usuario.Administrador)) throw new ArgumentException("Você não pode remover seu próprio acesso administrativo.");
        await using var conexao = await banco.AbrirAsync();
        var resumo = string.IsNullOrWhiteSpace(usuario.Senha) ? null : Senhas.Gerar(usuario.Senha);
        if (usuario.Id == 0) usuario.Id = await conexao.ExecuteScalarAsync<int>("INSERT INTO usuarios (Nome,Email,SenhaHash,Administrador,Ativo) VALUES (@Nome,@Email,@resumo,@Administrador,@Ativo); SELECT LAST_INSERT_ID()", new { usuario.Nome, usuario.Email, resumo, usuario.Administrador, usuario.Ativo });
        else if (await conexao.ExecuteAsync("UPDATE usuarios SET Nome=@Nome,Email=@Email,SenhaHash=COALESCE(@resumo,SenhaHash),Administrador=@Administrador,Ativo=@Ativo WHERE Id=@Id", new { usuario.Id, usuario.Nome, usuario.Email, resumo, usuario.Administrador, usuario.Ativo }) == 0) throw new KeyNotFoundException();
        return new { usuario.Id };
    }
}
