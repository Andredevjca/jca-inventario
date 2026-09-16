-- Executar dentro de uma transação de teste e desfazer ao terminar.
DECLARE @a INT, @b INT, @setor INT, @funcao INT, @funcionario INT, @tipo INT, @equip INT, @inventario INT, @conferencia INT;
INSERT usuarios (Nome,Email,SenhaHash) VALUES (N'Teste auditoria A',CONVERT(NVARCHAR(36),NEWID())+'@teste.invalid',N'teste');
SET @a=CONVERT(INT,SCOPE_IDENTITY());
INSERT usuarios (Nome,Email,SenhaHash) VALUES (N'Teste auditoria B',CONVERT(NVARCHAR(36),NEWID())+'@teste.invalid',N'teste');
SET @b=CONVERT(INT,SCOPE_IDENTITY());
EXEC sys.sp_set_session_context @key=N'UsuarioId',@value=@a;
INSERT setores (Nome) VALUES (CONVERT(NVARCHAR(36),NEWID())); SET @setor=CONVERT(INT,SCOPE_IDENTITY());
INSERT funcoes (Nome) VALUES (CONVERT(NVARCHAR(36),NEWID())); SET @funcao=CONVERT(INT,SCOPE_IDENTITY());
INSERT tipos_equipamento (Nome) VALUES (CONVERT(NVARCHAR(36),NEWID())); SET @tipo=CONVERT(INT,SCOPE_IDENTITY());
INSERT funcionarios (Nome,Cargo,FuncaoId,SetorId,TipoTrabalho,Endereco,Numero,Cep,Bairro,Cidade,Uf,Complemento,DataNascimento,DataAdmissao)
 VALUES (N'Teste auditoria',N'Teste',@funcao,@setor,N'Presencial',N'Praça da Sé',N'10A',N'01001000',N'Sé',N'São Paulo',N'SP',N'Sala 2','19900101','20200101');
SET @funcionario=CONVERT(INT,SCOPE_IDENTITY());
INSERT equipamentos (TipoId,Marca,Modelo,Status,Localizacao,ResponsavelId) VALUES (@tipo,N'Teste',N'Teste',N'Disponível',N'Estoque',@funcionario);
SET @equip=CONVERT(INT,SCOPE_IDENTITY());
INSERT movimentacoes (EquipamentoId,Tipo,UsuarioId) VALUES (@equip,N'Teste',@a);
INSERT manutencoes (EquipamentoId,DataEntrada,Tipo,Problema,Responsavel,UsuarioId) VALUES (@equip,'20260101',N'Teste',N'Teste',N'Teste',@a);
INSERT inventarios (Nome,UsuarioId) VALUES (N'Teste',@a); SET @inventario=CONVERT(INT,SCOPE_IDENTITY());
INSERT conferencias (InventarioId,EquipamentoId) VALUES (@inventario,@equip); SET @conferencia=CONVERT(INT,SCOPE_IDENTITY());
INSERT historico (EquipamentoId,UsuarioId,Descricao) VALUES (@equip,@a,N'Teste');
INSERT inventario_equipamentos (ConferenciaId,CapturadoNaCriacao) VALUES (@conferencia,1);
INSERT usuarios (Nome,Email,SenhaHash) VALUES (N'Teste auditoria C',CONVERT(NVARCHAR(36),NEWID())+'@teste.invalid',N'teste');
DECLARE @inclusao DATETIME2=(SELECT DataInclusao FROM funcionarios WHERE Id=@funcionario);
IF @inclusao IS NULL OR NOT EXISTS (SELECT 1 FROM funcionarios WHERE Id=@funcionario AND UsuarioInclusao=@a AND DataAlteracao IS NULL AND UsuarioAlteracao IS NULL AND Numero=N'10A' AND DataNascimento='19900101')
 THROW 51000,'Falha na inclusão do funcionário ou auditoria.',1;
EXEC sys.sp_set_session_context @key=N'UsuarioId',@value=@b;
DECLARE @tabela SYSNAME, @sql NVARCHAR(MAX), @total INT=0;
DECLARE teste CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo');
OPEN teste; FETCH NEXT FROM teste INTO @tabela;
WHILE @@FETCH_STATUS=0
BEGIN
 IF COL_LENGTH(N'dbo.'+QUOTENAME(@tabela),'UsuarioInclusao') IS NULL THROW 51000,'Tabela sem auditoria.',1;
 SET @sql=N'IF NOT EXISTS (SELECT 1 FROM dbo.'+QUOTENAME(@tabela)+N' WHERE UsuarioInclusao=@a AND DataInclusao IS NOT NULL) THROW 51000,''Inclusão não auditada.'',1;
 UPDATE dbo.'+QUOTENAME(@tabela)+N' SET DataInclusao=''19000101'',UsuarioInclusao=@b WHERE UsuarioInclusao=@a;
 IF NOT EXISTS (SELECT 1 FROM dbo.'+QUOTENAME(@tabela)+N' WHERE UsuarioInclusao=@a AND UsuarioAlteracao=@b AND DataAlteracao IS NOT NULL AND DataInclusao<>''19000101'') THROW 51000,''Alteração não auditada ou inclusão adulterada.'',1;';
 EXEC sys.sp_executesql @sql,N'@a int,@b int',@a,@b;
 SET @total+=1;
 FETCH NEXT FROM teste INTO @tabela;
END;
CLOSE teste; DEALLOCATE teste;
IF (SELECT DataInclusao FROM funcionarios WHERE Id=@funcionario)<>@inclusao THROW 51000,'Data de inclusão alterada.',1;
-- Atualização sem ator deve limpar a autoria da última alteração, sem reutilizar o usuário anterior.
EXEC sys.sp_set_session_context @key=N'UsuarioId',@value=NULL;
UPDATE funcionarios SET Numero=N'11' WHERE Id=@funcionario;
IF EXISTS (SELECT 1 FROM funcionarios WHERE Id=@funcionario AND UsuarioAlteracao IS NOT NULL) THROW 51000,'Ator anterior reutilizado.',1;
SELECT @total AS TabelasAuditadas;
