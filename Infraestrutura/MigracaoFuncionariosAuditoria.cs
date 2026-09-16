using Dapper;
using MySqlConnector;

namespace JcaInventario.Infraestrutura;

// Executada sob a trava de inicialização. DDL no MySQL faz commit implícito:
// cada etapa verifica o estado do banco para permitir retomar após uma falha.
public static class MigracaoFuncionariosAuditoria
{
    public static async Task AplicarAsync(MySqlConnection conexao)
    {
        await conexao.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS funcoes (
                Id INT PRIMARY KEY AUTO_INCREMENT,
                Nome VARCHAR(160) NOT NULL UNIQUE,
                Ativo BOOLEAN NOT NULL DEFAULT 1,
                Observacoes TEXT NULL
            ) ENGINE=InnoDB
            """);

        (string Nome, string Tipo)[] campos =
        [
            ("FuncaoId", "INT NULL"), ("Endereco", "VARCHAR(200) NULL"),
            ("Numero", "VARCHAR(20) NULL"), ("Cep", "VARCHAR(9) NULL"),
            ("Bairro", "VARCHAR(120) NULL"), ("Cidade", "VARCHAR(160) NULL"),
            ("Uf", "VARCHAR(2) NULL"), ("Complemento", "VARCHAR(160) NULL"),
            ("DataNascimento", "DATE NULL"), ("DataAdmissao", "DATE NULL")
        ];
        foreach (var campo in campos)
            await AdicionarColunaAsync(conexao, "funcionarios", campo.Nome, campo.Tipo);

        var tamanhoCargo = await conexao.ExecuteScalarAsync<int>("""
            SELECT CHARACTER_MAXIMUM_LENGTH FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='funcionarios' AND COLUMN_NAME='Cargo'
            """);
        if (tamanhoCargo < 160)
            await conexao.ExecuteAsync("ALTER TABLE funcionarios MODIFY COLUMN Cargo VARCHAR(160) NULL");
        await AdicionarReferenciaAsync(conexao, "funcionarios", "FuncaoId", "funcoes");

        // Os cargos legados não recebem uma autoria inventada.
        await conexao.ExecuteAsync("""
            INSERT INTO funcoes (Nome)
            SELECT DISTINCT TRIM(f.Cargo) FROM funcionarios f
            WHERE NULLIF(TRIM(f.Cargo), '') IS NOT NULL
            AND NOT EXISTS (SELECT 1 FROM funcoes c WHERE c.Nome=TRIM(f.Cargo));
            UPDATE funcionarios f JOIN funcoes c ON c.Nome=TRIM(f.Cargo)
            SET f.FuncaoId=c.Id WHERE f.FuncaoId IS NULL;
            """);

        string[] tabelas = ["usuarios", "setores", "tipos_equipamento", "funcionarios", "funcoes",
            "equipamentos", "movimentacoes", "manutencoes", "inventarios", "conferencias",
            "historico", "inventario_equipamentos"];
        foreach (var tabela in tabelas)
        {
            await AdicionarColunaAsync(conexao, tabela, "DataInclusao", "DATETIME(6) NULL");
            await AdicionarColunaAsync(conexao, tabela, "DataAlteracao", "DATETIME(6) NULL");
            await AdicionarColunaAsync(conexao, tabela, "UsuarioInclusao", "INT NULL");
            await AdicionarColunaAsync(conexao, tabela, "UsuarioAlteracao", "INT NULL");
            await AdicionarReferenciaAsync(conexao, tabela, "UsuarioInclusao", "usuarios");
            await AdicionarReferenciaAsync(conexao, tabela, "UsuarioAlteracao", "usuarios");
            await CriarTriggerAsync(conexao, $"tr_{tabela}_auditoria_inclusao", $"""
                BEFORE INSERT ON `{tabela}` FOR EACH ROW
                SET NEW.DataInclusao=UTC_TIMESTAMP(6),
                    NEW.UsuarioInclusao=@jca_usuario_id,
                    NEW.DataAlteracao=NULL,
                    NEW.UsuarioAlteracao=NULL
                """);
            await CriarTriggerAsync(conexao, $"tr_{tabela}_auditoria_alteracao", $"""
                BEFORE UPDATE ON `{tabela}` FOR EACH ROW
                SET NEW.DataInclusao=OLD.DataInclusao,
                    NEW.UsuarioInclusao=OLD.UsuarioInclusao,
                    NEW.DataAlteracao=UTC_TIMESTAMP(6),
                    NEW.UsuarioAlteracao=@jca_usuario_id
                """);
        }
    }

    private static async Task AdicionarColunaAsync(MySqlConnection conexao, string tabela, string coluna, string tipo)
    {
        var existe = await conexao.ExecuteScalarAsync<bool>("""
            SELECT EXISTS(SELECT 1 FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@tabela AND COLUMN_NAME=@coluna)
            """, new { tabela, coluna });
        if (!existe)
            await conexao.ExecuteAsync($"ALTER TABLE `{tabela}` ADD COLUMN `{coluna}` {tipo}", commandTimeout: 120);
    }

    private static async Task AdicionarReferenciaAsync(MySqlConnection conexao, string tabela, string coluna, string destino)
    {
        var existe = await conexao.ExecuteScalarAsync<bool>("""
            SELECT EXISTS(SELECT 1 FROM information_schema.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@tabela AND COLUMN_NAME=@coluna
                AND REFERENCED_TABLE_NAME=@destino AND REFERENCED_COLUMN_NAME='Id')
            """, new { tabela, coluna, destino });
        if (!existe)
            await conexao.ExecuteAsync($"ALTER TABLE `{tabela}` ADD CONSTRAINT `fk_{tabela}_{coluna}` FOREIGN KEY (`{coluna}`) REFERENCES `{destino}` (`Id`)", commandTimeout: 120);
    }

    private static async Task CriarTriggerAsync(MySqlConnection conexao, string nome, string corpo)
    {
        var existe = await conexao.ExecuteScalarAsync<bool>("""
            SELECT EXISTS(SELECT 1 FROM information_schema.TRIGGERS
                WHERE TRIGGER_SCHEMA=DATABASE() AND TRIGGER_NAME=@nome)
            """, new { nome });
        if (!existe)
            await conexao.ExecuteAsync($"CREATE TRIGGER `{nome}` {corpo}");
    }
}
