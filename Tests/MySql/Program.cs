using Dapper;
using JcaInventario.Infraestrutura;
using JcaInventario.Models;
using JcaInventario.Repositories;
using JcaInventario.Servicos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Security.Claims;

static void Verificar(bool condicao, string mensagem) { if (!condicao) throw new Exception(mensagem); }
var nome = "jca_teste_" + Guid.NewGuid().ToString("N");
var baseConexao = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("JCA_MYSQL_TEST_CONNECTION") ?? "Server=127.0.0.1;Port=3306;User ID=root;Password=;") { Database = "", AllowUserVariables = true };
Verificar(baseConexao.Server is "127.0.0.1" or "localhost" or "::1", "O teste só permite MySQL local.");
await using var servidor = new MySqlConnection(baseConexao.ConnectionString);
await servidor.OpenAsync();
await servidor.ExecuteAsync($"CREATE DATABASE `{nome}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
try
{
    baseConexao.Database = nome;
    var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:Banco"] = baseConexao.ConnectionString }).Build();
    var contexto = new HttpContextAccessor();
    var banco = new Banco(configuracao, contexto);
    await using var conexao = await banco.AbrirAsync();
    await conexao.ExecuteAsync(await File.ReadAllTextAsync("Database/estrutura.sql"));
    await conexao.ExecuteAsync("INSERT setores (Nome) VALUES ('Setor teste'); INSERT funcionarios (Nome,Cargo,SetorId,TipoTrabalho) VALUES ('Legado','Analista',1,'Presencial')");
    var inicializacao = new RepositorioInicializacao(banco);
    Verificar(await inicializacao.ObterTravaAsync(conexao)==1,"Trava de migração");
    try
    {
        await inicializacao.CriarEstruturaAsync(conexao, Directory.GetCurrentDirectory());
        await inicializacao.CriarEstruturaAsync(conexao, Directory.GetCurrentDirectory());
    }
    finally { await inicializacao.LiberarTravaAsync(conexao); }
    Verificar(await conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM funcionarios f JOIN funcoes c ON c.Id=f.FuncaoId WHERE c.Nome='Analista' AND f.DataInclusao IS NULL AND f.UsuarioInclusao IS NULL")==1,"Migração do cargo legado");
    Verificar(await conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE()")==24,"Triggers duplicados ou ausentes");
    var a=await conexao.ExecuteScalarAsync<int>("INSERT usuarios (Nome,Email,SenhaHash) VALUES ('A','a@teste.invalid','teste'); SELECT LAST_INSERT_ID()");
    var b=await conexao.ExecuteScalarAsync<int>("INSERT usuarios (Nome,Email,SenhaHash) VALUES ('B','b@teste.invalid','teste'); SELECT LAST_INSERT_ID()");
    contexto.HttpContext=new DefaultHttpContext { User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,a.ToString())],"Teste")) };
    var repositorio = new RepositorioCadastros();
    var servico = new ServicoCadastros(banco, repositorio, new ServicoEquipamentos(banco, new RepositorioEquipamentos()));
    var funcao=new Cadastro {Nome="Desenvolvedor"};
    await servico.SalvarCadastroAsync(JcaInventario.Helpers.TipoCadastro.Funcao,funcao);
    var funcionario=new Funcionario {Nome="Teste",SetorId=1,FuncaoId=funcao.Id,Endereco="Praça da Sé",Numero="10A",Cep="01001-000",Bairro="Sé",Cidade="São Paulo",Uf="SP",Complemento="Sala 2",DataNascimento=new(1990,1,1),DataAdmissao=new(2020,1,1)};
    await servico.SalvarFuncionarioAsync(funcionario,a);
    var salvo=await conexao.QuerySingleAsync<Funcionario>("SELECT * FROM funcionarios WHERE Id=@Id",funcionario);
    Verificar(salvo.Cargo==funcao.Nome && salvo.Cep=="01001000" && salvo.Numero=="10A" && salvo.DataAdmissao==funcionario.DataAdmissao,"Persistência dos campos");
    Verificar(await conexao.ExecuteScalarAsync<int>("SELECT UsuarioInclusao FROM funcionarios WHERE Id=@Id",funcionario)==a,"Ator autenticado");
    funcao.Nome="Desenvolvedor pleno";
    await servico.SalvarCadastroAsync(JcaInventario.Helpers.TipoCadastro.Funcao,funcao);
    Verificar(await conexao.ExecuteScalarAsync<string>("SELECT Cargo FROM funcionarios WHERE Id=@Id",funcionario)==funcao.Nome,"Renomear função");
    funcao.Ativo=false;
    bool rejeitou=false;
    try { await servico.SalvarCadastroAsync(JcaInventario.Helpers.TipoCadastro.Funcao,funcao); } catch(ArgumentException) {rejeitou=true;}
    Verificar(rejeitou,"Inativação com funcionário ativo");
    funcionario.FuncaoId=int.MaxValue;
    rejeitou=false;
    try { await servico.SalvarFuncionarioAsync(funcionario,a); } catch(ArgumentException) {rejeitou=true;}
    Verificar(rejeitou,"Função inexistente aceita");
    funcionario.FuncaoId=funcao.Id;
    funcionario.Numero="11";
    contexto.HttpContext.User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,b.ToString())],"Teste"));
    await servico.SalvarFuncionarioAsync(funcionario,b);
    Verificar(await conexao.ExecuteScalarAsync<int>("SELECT UsuarioAlteracao FROM funcionarios WHERE Id=@Id",funcionario)==b,"Troca do ator");
    await conexao.ExecuteAsync("SET @jca_usuario_id=@a",new {a});
    await conexao.ExecuteAsync("""
        INSERT usuarios (Nome,Email,SenhaHash) VALUES ('C','c@teste.invalid','teste');
        INSERT setores (Nome) VALUES ('Auditado');
        INSERT tipos_equipamento (Nome) VALUES ('Teste');
        INSERT equipamentos (TipoId,Marca,Modelo,Status,Localizacao) VALUES (1,'Teste','Teste','Disponível','Estoque');
        INSERT movimentacoes (EquipamentoId,Tipo,UsuarioId) VALUES (1,'Teste',@a);
        INSERT manutencoes (EquipamentoId,DataEntrada,Tipo,Problema,Responsavel,UsuarioId) VALUES (1,'2026-01-01','Teste','Teste','Teste',@a);
        INSERT inventarios (Nome,UsuarioId) VALUES ('Teste',@a);
        INSERT conferencias (InventarioId,EquipamentoId) VALUES (1,1);
        INSERT historico (EquipamentoId,UsuarioId,Descricao) VALUES (1,@a,'Teste');
        INSERT inventario_equipamentos (ConferenciaId,CapturadoNaCriacao) VALUES (1,1);
        """,new {a});
    var tabelas=(await conexao.QueryAsync<string>("SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE'")).ToArray();
    Verificar(tabelas.Length==12,"Quantidade de tabelas");
    await conexao.ExecuteAsync("SET @jca_usuario_id=@b",new {b});
    foreach(var tabela in tabelas)
    {
        Verificar(await conexao.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM `{tabela}` WHERE UsuarioInclusao=@a AND DataInclusao IS NOT NULL",new {a})>0,"Inclusão: "+tabela);
        await conexao.ExecuteAsync($"UPDATE `{tabela}` SET DataInclusao='1900-01-01',UsuarioInclusao=@b WHERE UsuarioInclusao=@a",new {a,b});
        Verificar(await conexao.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM `{tabela}` WHERE UsuarioInclusao=@a AND DataInclusao<>'1900-01-01' AND UsuarioAlteracao=@b AND DataAlteracao IS NOT NULL",new {a,b})>0,"Alteração: "+tabela);
    }
    contexto.HttpContext=null;
    await using(var semAtor=await banco.AbrirAsync())
        Verificar(await semAtor.ExecuteScalarAsync<int?>("SELECT @jca_usuario_id")==null,"Reutilização indevida do ator em conexão");
    Console.WriteLine("OK: migração repetida, cargos legados, CRUD de funcionário/função, validações, 24 triggers em 12 tabelas e contexto de usuário.");
}
finally
{
    MySqlConnection.ClearAllPools();
    await servidor.ExecuteAsync($"DROP DATABASE `{nome}`");
    Console.WriteLine("Banco temporário local removido.");
}
