using Dapper;
using JcaInventario.Infraestrutura;
using JcaInventario.Repositories;
using JcaInventario.Servicos;
using Microsoft.Extensions.Configuration;
using System.Text;

var conexaoTeste = Environment.GetEnvironmentVariable("JCA_CONEXAO_TESTE") ?? throw new InvalidOperationException("Informe JCA_CONEXAO_TESTE apontando para um banco exclusivo de testes.");
var configuracaoConexao = new MySqlConnector.MySqlConnectionStringBuilder(conexaoTeste);
if (!configuracaoConexao.Database.EndsWith("_teste")) throw new InvalidOperationException("O nome do banco deve terminar em _teste.");
var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:Banco"] = conexaoTeste }).Build();
var banco = new Banco(configuracao);
var raiz = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
var verificacoes = 0;
void Verificar(bool condicao, string descricao) { if (!condicao) throw new Exception(descricao); verificacoes++; Console.WriteLine("OK: " + descricao); }
await new ServicoInicializacao(banco, new RepositorioInicializacao(banco)).InicializarAsync(raiz);
await using var conexao = await banco.AbrirAsync();
var resumo = await conexao.ExecuteScalarAsync<string>("SELECT SenhaHash FROM usuarios WHERE Email='admin@admin.com'");
Verificar(resumo != "admin" && Senhas.Verificar("admin",resumo!), "Administrador inicial usa hash seguro");
Verificar(!Senhas.Verificar("senha incorreta", resumo!), "Senha incorreta é rejeitada");
Verificar(Senhas.Gerar("admin") != Senhas.Gerar("admin"), "Cada hash utiliza sal aleatório");
var novaSenha = Senhas.Gerar("SenhaAlterada123!");
await conexao.ExecuteAsync("UPDATE usuarios SET SenhaHash=@novaSenha WHERE Email='admin@admin.com'",new { novaSenha });
await new ServicoInicializacao(banco, new RepositorioInicializacao(banco)).InicializarAsync(raiz);
Verificar(await conexao.ExecuteScalarAsync<string>("SELECT SenhaHash FROM usuarios WHERE Email='admin@admin.com'") == novaSenha, "Reinicialização preserva a senha alterada do administrador");
Verificar(await conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM usuarios WHERE Email='admin@admin.com'") == 1, "Reinicialização não duplica o administrador");
Verificar(await conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM setores") == 6 && await conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tipos_equipamento") == 11, "Reinicialização não duplica dados iniciais");
Verificar(await conexao.ExecuteScalarAsync<string>("SELECT DEFAULT_CHARACTER_SET_NAME FROM information_schema.SCHEMATA WHERE SCHEMA_NAME=DATABASE()") == "utf8mb4", "Banco criado com utf8mb4");
var pdf = GeradorTermo.Gerar(Enumerable.Range(1,100).Select(indice => $"Linha {indice}: aquisição, manutenção e responsabilidade."));
var textoPdf = Encoding.Latin1.GetString(pdf);
Verificar(textoPdf.StartsWith("%PDF-1.4") && textoPdf.Contains("/Count 3") && textoPdf.EndsWith("%%EOF"), "Termo longo gera PDF com três páginas");
var posicaoTabela = int.Parse(textoPdf.Split("startxref\n")[1].Split('\n')[0]);
Verificar(textoPdf[posicaoTabela..].StartsWith("xref"), "Tabela de referências do PDF aponta para posição válida");
await conexao.ExecuteAsync("UPDATE usuarios SET SenhaHash=@resumo WHERE Email='admin@admin.com'",new { resumo });
Console.WriteLine($"RESULTADO: {verificacoes} verificações aprovadas.");

