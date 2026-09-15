using Dapper;
using JcaInventario.DTOs;
using JcaInventario.Infraestrutura;
using JcaInventario.Modelos;
using JcaInventario.Repositorios;
using JcaInventario.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace JcaInventario.Controllers;

[ApiController, Authorize, Route("api/equipamentos")]
public class EquipamentosController(RepositorioInventario repositorio, ServicoEquipamentos servico, Banco banco, IWebHostEnvironment ambiente) : ControllerBase
{
    private int UsuarioAtual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet] public async Task<object> Listar() => await repositorio.EquipamentosAsync();
    [HttpGet("{id:int}")] public async Task<object> Detalhes(int id) => await repositorio.DetalhesAsync(id);
    [HttpPost] public async Task<object> Salvar(Equipamento equipamento) => new { id = await servico.SalvarAsync(equipamento, UsuarioAtual) };
    [HttpPost("{id:int}/movimentacoes")] public async Task<IActionResult> Movimentar(int id, Movimentacao movimento) { await servico.MovimentarAsync(id, movimento, UsuarioAtual); return NoContent(); }

    [HttpPost("{id:int}/foto"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> SalvarFoto(int id, IFormFile arquivo)
    {
        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extensao) || arquivo.Length == 0 || arquivo.Length > 5 * 1024 * 1024) throw new ArgumentException("Selecione uma imagem JPG, PNG ou WebP de até 5 MB.");
        await using var memoria = new MemoryStream(); await arquivo.CopyToAsync(memoria); var bytes = memoria.ToArray();
        var valido = extensao is ".jpg" or ".jpeg" ? bytes.Length > 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 : extensao == ".png" ? bytes.AsSpan().StartsWith(new byte[] { 137,80,78,71,13,10,26,10 }) : bytes.Length > 12 && System.Text.Encoding.ASCII.GetString(bytes,0,4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes,8,4) == "WEBP";
        if (!valido) throw new ArgumentException("O conteúdo do arquivo não corresponde ao formato da imagem.");
        var pasta = Path.Combine(ambiente.ContentRootPath, "Dados", "Fotos"); Directory.CreateDirectory(pasta);
        var nome = Guid.NewGuid().ToString("N") + extensao;
        await System.IO.File.WriteAllBytesAsync(Path.Combine(pasta,nome), bytes);
        try { await AlterarFotoAsync(id, nome); } catch { System.IO.File.Delete(Path.Combine(pasta,nome)); throw; }
        return Ok(new { foto = nome });
    }
    [HttpDelete("{id:int}/foto")]
    public async Task<IActionResult> RemoverFoto(int id) { await AlterarFotoAsync(id, null); return NoContent(); }
    private async Task AlterarFotoAsync(int id, string? nome)
    {
        await using var conexao = await banco.AbrirAsync(); await using var transacao = await conexao.BeginTransactionAsync();
        var equipamento = await conexao.QuerySingleOrDefaultAsync<Equipamento>("SELECT * FROM equipamentos WHERE Id=@id FOR UPDATE", new { id }, transacao) ?? throw new KeyNotFoundException();
        await conexao.ExecuteAsync("UPDATE equipamentos SET Foto=@nome,AtualizadoEm=CURRENT_TIMESTAMP WHERE Id=@id", new { nome, id }, transacao);
        await ServicoEquipamentos.RegistrarHistoricoAsync(conexao, transacao, id, UsuarioAtual, nome == null ? "Foto removida" : "Foto alterada", equipamento.Foto, nome);
        await transacao.CommitAsync();
        if (equipamento.Foto != null) System.IO.File.Delete(Path.Combine(ambiente.ContentRootPath,"Dados","Fotos",Path.GetFileName(equipamento.Foto)));
    }
    [HttpGet("{id:int}/foto")]
    public async Task<IActionResult> Foto(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var nome = await conexao.ExecuteScalarAsync<string>("SELECT Foto FROM equipamentos WHERE Id=@id", new { id });
        if (nome == null) return NotFound();
        var caminho = Path.Combine(ambiente.ContentRootPath,"Dados","Fotos",Path.GetFileName(nome));
        if (!System.IO.File.Exists(caminho)) return NotFound();
        return PhysicalFile(caminho, Path.GetExtension(nome) == ".png" ? "image/png" : Path.GetExtension(nome) == ".webp" ? "image/webp" : "image/jpeg");
    }
    [HttpGet("{id:int}/termo")]
    public async Task<IActionResult> Termo(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var equipamento = await conexao.QuerySingleOrDefaultAsync(RepositorioInventario.ConsultaEquipamentos + " WHERE e.Id=@id", new { id }) ?? throw new KeyNotFoundException();
        if (equipamento.ResponsavelId == null) throw new ArgumentException("Vincule um funcionário para emitir o termo.");
        string[] linhas = ["JCA SOLUÇÕES", "TERMO DE RESPONSABILIDADE", "", $"Data: {DateTime.Now:dd/MM/yyyy}", $"Funcionário: {equipamento.Responsavel}", $"Setor: {equipamento.Setor}", "", $"Equipamento: {equipamento.Tipo} {equipamento.Marca} {equipamento.Modelo}", $"Patrimônio: {equipamento.NumeroPatrimonio ?? "Não informado"}", $"Número de série: {equipamento.NumeroSerie ?? "Não informado"}", $"Localização: {equipamento.Localizacao}", "", "Declaro ter recebido o equipamento descrito acima para uso profissional.", "Comprometo-me a conservar o bem e comunicar problemas, perdas ou avarias,", "bem como devolvê-lo quando solicitado pela empresa.", "", $"Observações: {equipamento.Observacoes ?? "Sem observações."}", "", "", "____________________________________________________________", "Assinatura do funcionário", "", "", "____________________________________________________________", "Assinatura do representante da JCA Soluções"];
        return File(GeradorTermo.Gerar(linhas), "application/pdf", $"termo-equipamento-{id}.pdf");
    }
}
