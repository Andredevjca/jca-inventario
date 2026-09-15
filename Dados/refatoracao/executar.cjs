const fs = require('fs');
const ler = caminho => fs.readFileSync(caminho,'utf8').replace(/^\uFEFF/,'');
const gravar = (caminho,texto) => fs.writeFileSync(caminho,texto,'utf8');
for (const caminho of ['Controllers/CadastrosController.cs','Controllers/InventarioController.cs','Controllers/EquipamentosController.cs','Controllers/ContaController.cs','Servicos/ServicoEquipamentos.cs','Infraestrutura/InicializadorBanco.cs','Program.cs','Repositorios/RepositorioInventario.cs']) gravar('Dados/refatoracao/'+caminho.replaceAll('/','_'),ler(caminho));
let cadastros=ler('Controllers/CadastrosController.cs');
cadastros=cadastros.replace(/using Microsoft.AspNetCore.*;\n/g,'').replace('using System.Security.Claims;\n','').replace('namespace JcaInventario.Controllers;','namespace JcaInventario.Servicos;').replace(/^\[ApiController.*\]\n/m,'').replace(/    \[.*\]\n/g,'').replace('public class CadastrosController(Banco banco) : ControllerBase','public class ServicoCadastros(Banco banco, RepositorioCadastros repositorio, ServicoEquipamentos servicoEquipamentos)').replace(/    private int UsuarioAtual[^\n]*\n/,'').replace('Task<IActionResult> Listar(string cadastro)','Task<object> ListarAsync(string cadastro)').replace('        if (cadastro == "usuarios" && !User.IsInRole("Administrador")) return Forbid();\n','').replace('return Ok(await conexao.QueryAsync(consulta));','return await conexao.QueryAsync(consulta);').replace('SalvarCadastro(Cadastro cadastro)','SalvarCadastroAsync(TipoCadastro tipo, Cadastro cadastro)').replace('        var tabela = Request.Path.Value!.EndsWith("setores") ? "setores" : "tipos_equipamento";\n','').replace('SalvarFuncionario(Funcionario funcionario)','SalvarFuncionarioAsync(Funcionario funcionario, int usuario)').replace('FuncionarioDetalhes(int id)','ObterFuncionarioAsync(int id)').replace('SetorDetalhes(int id)','ObterSetorAsync(int id)').replace('SalvarUsuario(Usuario usuario)','SalvarUsuarioAsync(Usuario usuario, int usuarioAtual)').replace('usuario.Id == UsuarioAtual','usuario.Id == usuarioAtual').replaceAll('UsuarioAtual','usuario').replaceAll('ServicoEquipamentos.RegistrarHistoricoAsync','servicoEquipamentos.RegistrarHistoricoAsync');
const inicioConsulta=cadastros.indexOf('        var consulta = cadastro switch'); const fimConsulta=cadastros.indexOf('        await using var conexao',inicioConsulta); cadastros=cadastros.slice(0,inicioConsulta)+cadastros.slice(fimConsulta);
cadastros=cadastros.replace('await conexao.QueryAsync(consulta)','await repositorio.ListarAsync(conexao, cadastro)');
cadastros=cadastros.replace('await conexao.QuerySingleOrDefaultAsync($"SELECT Id FROM {tabela} WHERE Id=@Id FOR UPDATE", cadastro, transacao)','await repositorio.ObterCadastroParaAtualizacaoAsync(conexao, tipo, cadastro.Id, transacao)');
const iniVinculo=cadastros.indexOf('                var vinculos ='); const fimVinculo=cadastros.indexOf('\n',iniVinculo); cadastros=cadastros.slice(0,iniVinculo)+cadastros.slice(fimVinculo+1);
cadastros=cadastros.replace('await conexao.ExecuteScalarAsync<int>(vinculos, cadastro, transacao)','await repositorio.ContarVinculosAtivosAsync(conexao, tipo, cadastro.Id, transacao)').replace('await conexao.ExecuteAsync($"UPDATE {tabela} SET Nome=@Nome,Ativo=@Ativo,Observacoes=@Observacoes WHERE Id=@Id", cadastro, transacao)','await repositorio.AtualizarCadastroAsync(conexao, tipo, cadastro, transacao)').replace('await conexao.ExecuteScalarAsync<int>($"INSERT INTO {tabela} (Nome,Ativo,Observacoes) VALUES (@Nome,@Ativo,@Observacoes); SELECT LAST_INSERT_ID()", cadastro, transacao)','await repositorio.InserirCadastroAsync(conexao, tipo, cadastro, transacao)');
gravar('Servicos/ServicoCadastros.cs',cadastros);
let inventario=ler('Controllers/InventarioController.cs');
inventario=inventario.replace(/using Microsoft.AspNetCore.*;\n/g,'').replace('using System.Security.Claims;\n','').replace('namespace JcaInventario.Controllers;','namespace JcaInventario.Servicos;').replace(/^\[ApiController.*\]\n/m,'').replace(/    \[.*\]\n/g,'').replace('public class InventarioController(Banco banco, ServicoEquipamentos servico) : ControllerBase','public class ServicoInventario(Banco banco, RepositorioInventario repositorio, ServicoEquipamentos servicoEquipamentos)').replace(/    private int UsuarioAtual[^\n]*\n/,'').replace(/    public async Task<object> SalvarManutencao[^\n]*\n/,'').replace('Movimentacoes()','ListarMovimentacoesAsync()').replace('Manutencoes()','ListarManutencoesAsync()').replace('Inventarios()','ListarInventariosAsync()').replace('CriarInventario(NovoInventario inventario)','CriarInventarioAsync(NovoInventario inventario, int usuario)').replace('Conferencias(int id)','ObterConferenciasAsync(int id)').replace('Task<IActionResult> Conferir(int id, int equipamentoId, Conferencia conferencia)','Task ConferirAsync(int id, int equipamentoId, Conferencia conferencia, int usuario)').replace('Task<IActionResult> Encerrar(int id)','Task EncerrarAsync(int id)').replace('Dashboard()','ObterDashboardAsync()').replaceAll('UsuarioAtual','usuario').replaceAll('ServicoEquipamentos.RegistrarHistoricoAsync','servicoEquipamentos.RegistrarHistoricoAsync').replaceAll(' return NoContent();','');
gravar('Servicos/ServicoInventario.cs',inventario);
// Extrai chamadas completas de Dapper preservando parâmetros e transações.
function argumentos(texto,inicio) {
 let nivel=0,partes=[],comeco=inicio;
 for(let i=inicio;i<texto.length;i++) {
  if(texto.startsWith('"""',i)){let fim=texto.indexOf('"""',i+3);if(fim<0)throw Error('Literal SQL incompleto');i=fim+2;continue;}
  if(texto[i]==='"'){for(i++;i<texto.length;i++){if(texto[i]==='\\'){i++;continue;}if(texto[i]==='"')break;}continue;}
  if('({['.includes(texto[i]))nivel++;
  if(texto[i]===')'&&nivel===0){partes.push(texto.slice(comeco,i).trim());return {partes,fim:i+1};}
  if(')}]'.includes(texto[i]))nivel--;
  if(texto[i]===','&&nivel===0){partes.push(texto.slice(comeco,i).trim());comeco=i+1;}
 }
 throw Error('Chamada não encerrada');
}
function extrair(caminho,classe,nomes) {
 let texto=ler(caminho), metodos=[], indice=0;
 const expressao=/conexao\.(QuerySingleOrDefaultAsync|QuerySingleAsync|QueryAsync|ExecuteScalarAsync|ExecuteAsync)(?:<([^>]+)>)?\(/g;
 texto=texto.replace('using Dapper;\n','');
 let resultado;
 while((resultado=expressao.exec(texto))) {
  const encontrado=argumentos(texto,expressao.lastIndex), [sql,...demais]=encontrado.partes;
  const nome=nomes[indice++]; if(!nome)throw Error(`Falta nome ${caminho}: ${sql}`);
  const operacao=resultado[1], generico=resultado[2];
  const retorno=operacao==='ExecuteAsync'?'int':operacao==='QueryAsync'?`IEnumerable<${generico||'dynamic'}>`:generico||'dynamic';
  const possuiParametros=demais.length>0&&!demais[0].startsWith('transaction:');
  const parametro=possuiParametros?demais[0]:'null'; const transacao=possuiParametros?demais[1]:demais[0]?.replace('transaction:','').trim();
  const assinatura=`    public Task<${retorno}> ${nome}(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)\n        => conexao.${operacao}${generico?'<'+generico+'>':''}(${sql}, parametros, transacao);\n`;
  if(!metodos.some(item=>item.nome===nome))metodos.push({nome,texto:assinatura});
  const chamada=`repositorio.${nome}(conexao${possuiParametros?', '+parametro:transacao?', null':''}${transacao?', '+transacao:''})`;
  texto=texto.slice(0,resultado.index)+chamada+texto.slice(encontrado.fim);expressao.lastIndex=resultado.index+chamada.length;
 }
 if(indice!==nomes.length)throw Error(`Quantidade incompatível ${caminho}: ${indice}/${nomes.length}`);
 gravar(caminho,texto);
 return metodos.map(item=>item.texto).join('\n');
}
const cabecalho=nome=>`using Dapper;\nusing MySqlConnector;\nusing JcaInventario.Modelos;\nnamespace JcaInventario.Repositorios;\n\npublic class ${nome}\n{\n`;
let metodos=extrair('Servicos/ServicoCadastros.cs','RepositorioCadastros',['SetorAtivoAsync','ObterFuncionarioParaAtualizacaoAsync','ContarEquipamentosResponsavelAsync','InserirFuncionarioAsync','ListarEquipamentosResponsavelParaAtualizacaoAsync','InserirTransferenciaSetorAsync','AtualizarFuncionarioAsync','ObterFuncionarioAsync','ListarEquipamentosFuncionarioAsync','ListarHistoricoFuncionarioAsync','ObterSetorAsync','ListarFuncionariosSetorAsync','ListarEquipamentosSetorAsync','InserirUsuarioAsync','AtualizarUsuarioAsync']);
gravar('Repositorios/RepositorioCadastros.cs',cabecalho('RepositorioCadastros')+metodos+'}\n');
metodos=extrair('Servicos/ServicoInventario.cs','RepositorioInventario',['ListarMovimentacoesAsync','ListarManutencoesAsync','ListarInventariosAsync','InserirInventarioAsync','InserirConferenciasAsync','ObterInventarioAsync','ListarConferenciasAsync','ObterInventarioParaAtualizacaoAsync','ObterConferenciaParaAtualizacaoAsync','AtualizarConferenciaAsync','ObterInventarioParaAtualizacaoAsync','ContarPendentesAsync','EncerrarInventarioAsync','ObterTotaisAsync','AgruparPorSetorAsync','AgruparPorLocalizacaoAsync','AgruparPorStatusAsync','ContarEquipamentosPendentesAsync','ListarMovimentacoesRecentesAsync']);
const originalRepositorio=ler('Repositorios/RepositorioInventario.cs');
const constantes=originalRepositorio.slice(originalRepositorio.indexOf('    public const string'),originalRepositorio.indexOf('    public async Task'));
gravar('Repositorios/RepositorioInventario.cs',cabecalho('RepositorioInventario')+constantes+metodos+'}\n');
let equipamentos=ler('Servicos/ServicoEquipamentos.cs').replace('public class ServicoEquipamentos(Banco banco)','public class ServicoEquipamentos(Banco banco, RepositorioEquipamentos repositorio)').replace('using Dapper;','using Dapper;\nusing JcaInventario.Repositorios;').replaceAll('public static async Task','public async Task').replace('private static async Task','private async Task');
gravar('Servicos/ServicoEquipamentos.cs',equipamentos);
metodos=extrair('Servicos/ServicoEquipamentos.cs','RepositorioEquipamentos',['InserirHistoricoAsync','ResponsavelAtivoAsync','TipoAtivoAsync','ObterSetorResponsavelAsync','ObterSetorResponsavelAsync','InserirMovimentacaoAsync','ObterParaAtualizacaoAsync','InserirAsync','AtualizarAsync','ObterParaMovimentacaoAsync','AtualizarSituacaoAsync','ObterParaAtualizacaoAsync','PossuiManutencaoAbertaAsync','InserirManutencaoAsync','ObterManutencaoParaAtualizacaoAsync','AtualizarManutencaoAsync','AtualizarSituacaoAsync']);
gravar('Repositorios/RepositorioEquipamentos.cs',cabecalho('RepositorioEquipamentos')+metodos+'}\n');
