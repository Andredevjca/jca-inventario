-- SQL Server. Idempotente: aplicada automaticamente depois de estrutura.sql.
-- Datas em UTC. NULL nos registros antigos significa informação desconhecida.
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.funcoes', N'U') IS NULL
    CREATE TABLE dbo.funcoes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(160) NOT NULL UNIQUE,
        Ativo BIT NOT NULL DEFAULT 1,
        Observacoes NVARCHAR(MAX) NULL
    );
IF COL_LENGTH('dbo.funcionarios', 'FuncaoId') IS NULL
    ALTER TABLE dbo.funcionarios ADD FuncaoId INT NULL REFERENCES dbo.funcoes(Id);
IF COL_LENGTH('dbo.funcionarios', 'Endereco') IS NULL
    ALTER TABLE dbo.funcionarios ADD Endereco NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.funcionarios', 'Numero') IS NULL
    ALTER TABLE dbo.funcionarios ADD Numero NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.funcionarios', 'Cep') IS NULL
    ALTER TABLE dbo.funcionarios ADD Cep NVARCHAR(9) NULL;
IF COL_LENGTH('dbo.funcionarios', 'Bairro') IS NULL
    ALTER TABLE dbo.funcionarios ADD Bairro NVARCHAR(120) NULL;
IF COL_LENGTH('dbo.funcionarios', 'Cidade') IS NULL
    ALTER TABLE dbo.funcionarios ADD Cidade NVARCHAR(160) NULL;
IF COL_LENGTH('dbo.funcionarios', 'Uf') IS NULL
    ALTER TABLE dbo.funcionarios ADD Uf NVARCHAR(2) NULL;
IF COL_LENGTH('dbo.funcionarios', 'Complemento') IS NULL
    ALTER TABLE dbo.funcionarios ADD Complemento NVARCHAR(160) NULL;
IF COL_LENGTH('dbo.funcionarios', 'DataNascimento') IS NULL
    ALTER TABLE dbo.funcionarios ADD DataNascimento DATE NULL;
IF COL_LENGTH('dbo.funcionarios', 'DataAdmissao') IS NULL
    ALTER TABLE dbo.funcionarios ADD DataAdmissao DATE NULL;
IF COL_LENGTH('dbo.funcionarios', 'Cargo') < 320
    ALTER TABLE dbo.funcionarios ALTER COLUMN Cargo NVARCHAR(160) NULL;

-- Preserva os cargos já cadastrados e cria o vínculo antes de ativar a auditoria.
EXEC(N'INSERT INTO dbo.funcoes (Nome)
 SELECT DISTINCT LTRIM(RTRIM(Cargo)) FROM dbo.funcionarios f
 WHERE NULLIF(LTRIM(RTRIM(Cargo)), N'''') IS NOT NULL
 AND NOT EXISTS (SELECT 1 FROM dbo.funcoes c WHERE c.Nome=LTRIM(RTRIM(f.Cargo)));
 UPDATE f SET FuncaoId=c.Id FROM dbo.funcionarios f
 JOIN dbo.funcoes c ON c.Nome=LTRIM(RTRIM(f.Cargo)) WHERE f.FuncaoId IS NULL;');

DECLARE @tabela SYSNAME, @chave SYSNAME, @sql NVARCHAR(MAX), @objeto NVARCHAR(260), @trigger NVARCHAR(260);
DECLARE tabelas CURSOR LOCAL FAST_FORWARD FOR
 SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND name IN
 (N'usuarios',N'setores',N'tipos_equipamento',N'funcionarios',N'funcoes',N'equipamentos',
  N'movimentacoes',N'manutencoes',N'inventarios',N'conferencias',N'historico',N'inventario_equipamentos');
OPEN tabelas;
FETCH NEXT FROM tabelas INTO @tabela;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @objeto=N'dbo.'+QUOTENAME(@tabela);
 SET @chave=CASE WHEN @tabela=N'inventario_equipamentos' THEN N'ConferenciaId' ELSE N'Id' END;
 IF COL_LENGTH(@objeto,N'DataInclusao') IS NULL
 BEGIN
  SET @sql=N'ALTER TABLE '+@objeto+N' ADD DataInclusao DATETIME2 NULL;'; EXEC(@sql);
 END;
 IF COL_LENGTH(@objeto,N'DataAlteracao') IS NULL
 BEGIN
  SET @sql=N'ALTER TABLE '+@objeto+N' ADD DataAlteracao DATETIME2 NULL;'; EXEC(@sql);
 END;
 IF COL_LENGTH(@objeto,N'UsuarioInclusao') IS NULL
 BEGIN
  SET @sql=N'ALTER TABLE '+@objeto+N' ADD UsuarioInclusao INT NULL REFERENCES dbo.usuarios(Id);'; EXEC(@sql);
 END;
 IF COL_LENGTH(@objeto,N'UsuarioAlteracao') IS NULL
 BEGIN
  SET @sql=N'ALTER TABLE '+@objeto+N' ADD UsuarioAlteracao INT NULL REFERENCES dbo.usuarios(Id);'; EXEC(@sql);
 END;
 SET @trigger=N'dbo.'+QUOTENAME(N'tr_'+@tabela+N'_auditoria');
 SET @sql=N'CREATE OR ALTER TRIGGER '+@trigger+N' ON '+@objeto+N' AFTER INSERT, UPDATE AS
 BEGIN
  SET NOCOUNT ON;
  IF TRIGGER_NESTLEVEL(OBJECT_ID(N'''+@trigger+N''')) > 1 RETURN;
  DECLARE @usuario INT=TRY_CONVERT(INT,SESSION_CONTEXT(N''UsuarioId''));
  DECLARE @agora DATETIME2=SYSUTCDATETIME();
  UPDATE alvo SET
   DataInclusao=CASE WHEN anterior.'+QUOTENAME(@chave)+N' IS NULL THEN @agora ELSE anterior.DataInclusao END,
   UsuarioInclusao=CASE WHEN anterior.'+QUOTENAME(@chave)+N' IS NULL THEN @usuario ELSE anterior.UsuarioInclusao END,
   DataAlteracao=CASE WHEN anterior.'+QUOTENAME(@chave)+N' IS NULL THEN NULL ELSE @agora END,
   UsuarioAlteracao=CASE WHEN anterior.'+QUOTENAME(@chave)+N' IS NULL THEN NULL ELSE @usuario END
  FROM '+@objeto+N' alvo
  JOIN inserted novo ON novo.'+QUOTENAME(@chave)+N'=alvo.'+QUOTENAME(@chave)+N'
  LEFT JOIN deleted anterior ON anterior.'+QUOTENAME(@chave)+N'=novo.'+QUOTENAME(@chave)+N';
 END;';
 EXEC(@sql);
 FETCH NEXT FROM tabelas INTO @tabela;
END;
CLOSE tabelas;
DEALLOCATE tabelas;
COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT > 0 ROLLBACK;
 THROW;
END CATCH;
