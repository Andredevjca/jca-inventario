using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Repositories;

namespace JcaInventario.Servicos;

public class ServicoInicializacao(Banco banco, IRepositorioInicializacao repositorio)
{
    public async Task InicializarAsync(string raiz)
    {
        await repositorio.CriarBancoAsync();
        await using var conexao = await banco.AbrirAsync();
        if (await repositorio.ObterTravaAsync(conexao) != 1)
            throw new InvalidOperationException("Não foi possível obter a trava de inicialização.");

        try
        {
            await repositorio.CriarEstruturaAsync(conexao, raiz);
            await using var transacao = (Microsoft.Data.SqlClient.SqlTransaction)await conexao.BeginTransactionAsync();
            foreach (var setor in new[] { "Desenvolvimento", "Suporte", "Financeiro", "Comercial", "Administrativo", "Recursos Humanos" })
                await repositorio.InserirSetorAusenteAsync(conexao, transacao, setor);
            foreach (var tipo in new[] { "Notebook", "Desktop", "Monitor", "Teclado", "Mouse", "Headset", "Impressora", "Celular", "Tablet", "Nobreak", "Outros" })
                await repositorio.InserirTipoAusenteAsync(conexao, transacao, tipo);
            if (!await repositorio.AdministradorExisteAsync(conexao, transacao))
                await repositorio.InserirAdministradorAsync(conexao, transacao, Senhas.Gerar("admin"));
            await transacao.CommitAsync();
        }
        finally
        {
            await repositorio.LiberarTravaAsync(conexao);
        }
    }
}
