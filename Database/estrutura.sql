-- SQL Server: executar no banco dbActyon_Inventario.
-- Execute criar-banco.sql antes deste arquivo ao instalar pelo SSMS.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

IF OBJECT_ID(N'dbo.usuarios', N'U') IS NULL
BEGIN
CREATE TABLE dbo.usuarios (
 Id INT IDENTITY(1,1) PRIMARY KEY, Nome NVARCHAR(160) NOT NULL, Email NVARCHAR(190) NOT NULL UNIQUE,
 SenhaHash NVARCHAR(300) NOT NULL, Administrador BIT NOT NULL DEFAULT 0, Ativo BIT NOT NULL DEFAULT 1
);

END;
IF OBJECT_ID(N'dbo.setores', N'U') IS NULL
BEGIN
CREATE TABLE dbo.setores (
 Id INT IDENTITY(1,1) PRIMARY KEY, Nome NVARCHAR(160) NOT NULL UNIQUE, Ativo BIT NOT NULL DEFAULT 1, Observacoes NVARCHAR(MAX)
);

END;
IF OBJECT_ID(N'dbo.tipos_equipamento', N'U') IS NULL
BEGIN
CREATE TABLE dbo.tipos_equipamento (
 Id INT IDENTITY(1,1) PRIMARY KEY, Nome NVARCHAR(160) NOT NULL UNIQUE, Ativo BIT NOT NULL DEFAULT 1, Observacoes NVARCHAR(MAX)
);

END;
IF OBJECT_ID(N'dbo.funcionarios', N'U') IS NULL
BEGIN
CREATE TABLE dbo.funcionarios (
 Id INT IDENTITY(1,1) PRIMARY KEY, Nome NVARCHAR(160) NOT NULL, Email NVARCHAR(190), Telefone NVARCHAR(40), Matricula NVARCHAR(60) NULL,
 Cargo NVARCHAR(120), SetorId INT NOT NULL, TipoTrabalho NVARCHAR(30) NOT NULL, Ativo BIT NOT NULL DEFAULT 1, Observacoes NVARCHAR(MAX),
 FOREIGN KEY (SetorId) REFERENCES setores(Id)
);
CREATE INDEX ix_funcionarios_nome ON dbo.funcionarios (Nome);
CREATE UNIQUE INDEX ux_funcionarios_Matricula ON dbo.funcionarios (Matricula) WHERE Matricula IS NOT NULL;
END;
IF OBJECT_ID(N'dbo.equipamentos', N'U') IS NULL
BEGIN
CREATE TABLE dbo.equipamentos (
 Id INT IDENTITY(1,1) PRIMARY KEY, TipoId INT NOT NULL, Marca NVARCHAR(100) NOT NULL, Modelo NVARCHAR(160) NOT NULL,
 NumeroPatrimonio NVARCHAR(100) NULL, NumeroSerie NVARCHAR(150) NULL, DataAquisicao DATE, ValorAquisicao DECIMAL(14,2) NOT NULL DEFAULT 0,
 Status NVARCHAR(40) NOT NULL, Localizacao NVARCHAR(40) NOT NULL, ResponsavelId INT NULL, Observacoes NVARCHAR(MAX),
 Processador NVARCHAR(160), MemoriaRam NVARCHAR(100), Armazenamento NVARCHAR(100), SistemaOperacional NVARCHAR(160), Foto NVARCHAR(200),
 CriadoEm DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP, AtualizadoEm DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
 FOREIGN KEY (TipoId) REFERENCES tipos_equipamento(Id), FOREIGN KEY (ResponsavelId) REFERENCES funcionarios(Id)
);
CREATE INDEX ix_equipamentos_status ON dbo.equipamentos (Status);
CREATE INDEX ix_equipamentos_localizacao ON dbo.equipamentos (Localizacao);
CREATE UNIQUE INDEX ux_equipamentos_NumeroPatrimonio ON dbo.equipamentos (NumeroPatrimonio) WHERE NumeroPatrimonio IS NOT NULL;
CREATE UNIQUE INDEX ux_equipamentos_NumeroSerie ON dbo.equipamentos (NumeroSerie) WHERE NumeroSerie IS NOT NULL;
END;
IF OBJECT_ID(N'dbo.movimentacoes', N'U') IS NULL
BEGIN
CREATE TABLE dbo.movimentacoes (
 Id INT IDENTITY(1,1) PRIMARY KEY, EquipamentoId INT NOT NULL, ResponsavelAnteriorId INT NULL, NovoResponsavelId INT NULL,
 SetorAnteriorId INT NULL, NovoSetorId INT NULL, LocalizacaoAnterior NVARCHAR(40), NovaLocalizacao NVARCHAR(40),
 StatusAnterior NVARCHAR(40), NovoStatus NVARCHAR(40), Tipo NVARCHAR(60) NOT NULL, Data DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
 UsuarioId INT NOT NULL, Observacao NVARCHAR(MAX),
 FOREIGN KEY (EquipamentoId) REFERENCES equipamentos(Id), FOREIGN KEY (ResponsavelAnteriorId) REFERENCES funcionarios(Id),
 FOREIGN KEY (NovoResponsavelId) REFERENCES funcionarios(Id), FOREIGN KEY (SetorAnteriorId) REFERENCES setores(Id),
 FOREIGN KEY (NovoSetorId) REFERENCES setores(Id), FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id)
);
CREATE INDEX ix_movimentacoes_data ON dbo.movimentacoes (EquipamentoId,Data);
END;
IF OBJECT_ID(N'dbo.manutencoes', N'U') IS NULL
BEGIN
CREATE TABLE dbo.manutencoes (
 Id INT IDENTITY(1,1) PRIMARY KEY, EquipamentoId INT NOT NULL, DataEntrada DATE NOT NULL, DataSaida DATE,
 Tipo NVARCHAR(100) NOT NULL, Problema NVARCHAR(MAX) NOT NULL, Solucao NVARCHAR(MAX), Valor DECIMAL(14,2) NOT NULL DEFAULT 0,
 Responsavel NVARCHAR(160) NOT NULL, Observacoes NVARCHAR(MAX), UsuarioId INT NOT NULL,
 FOREIGN KEY (EquipamentoId) REFERENCES equipamentos(Id), FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id)
);
CREATE INDEX ix_manutencoes_abertas ON dbo.manutencoes (EquipamentoId,DataSaida);
END;
IF OBJECT_ID(N'dbo.inventarios', N'U') IS NULL
BEGIN
CREATE TABLE dbo.inventarios (
 Id INT IDENTITY(1,1) PRIMARY KEY, Nome NVARCHAR(160) NOT NULL, Data DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
 Encerrado BIT NOT NULL DEFAULT 0, UsuarioId INT NOT NULL, FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id)
);

END;
IF OBJECT_ID(N'dbo.conferencias', N'U') IS NULL
BEGIN
CREATE TABLE dbo.conferencias (
 Id INT IDENTITY(1,1) PRIMARY KEY, InventarioId INT NOT NULL, EquipamentoId INT NOT NULL,
 Situacao NVARCHAR(30) NOT NULL DEFAULT 'Pendente', Data DATETIME2 NULL, UsuarioId INT NULL, Observacao NVARCHAR(MAX),
 FOREIGN KEY (InventarioId) REFERENCES inventarios(Id), FOREIGN KEY (EquipamentoId) REFERENCES equipamentos(Id),
 FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id), CONSTRAINT ux_conferencia UNIQUE (InventarioId,EquipamentoId)
);

END;
IF OBJECT_ID(N'dbo.historico', N'U') IS NULL
BEGIN
CREATE TABLE dbo.historico (
 Id INT IDENTITY(1,1) PRIMARY KEY, EquipamentoId INT NOT NULL, Data DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
 UsuarioId INT NOT NULL, Descricao NVARCHAR(250) NOT NULL, DadosAnteriores NVARCHAR(MAX) NULL, DadosNovos NVARCHAR(MAX) NULL,
 FOREIGN KEY (EquipamentoId) REFERENCES equipamentos(Id), FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id)
);
CREATE INDEX ix_historico_data ON dbo.historico (EquipamentoId,Data);
END;

IF OBJECT_ID(N'dbo.inventario_equipamentos', N'U') IS NULL
BEGIN
CREATE TABLE dbo.inventario_equipamentos (
 ConferenciaId INT PRIMARY KEY,
 NumeroPatrimonio NVARCHAR(100), NumeroSerie NVARCHAR(150), Tipo NVARCHAR(160),
 Marca NVARCHAR(100), Modelo NVARCHAR(160), Responsavel NVARCHAR(160), Setor NVARCHAR(160),
 Localizacao NVARCHAR(40), Status NVARCHAR(40),
 CapturadoEm DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
 CapturadoNaCriacao BIT NOT NULL,
 FOREIGN KEY (ConferenciaId) REFERENCES conferencias(Id)
);

END;

-- Migra somente registros antigos sem copia; nunca sobrescreve o historico salvo.
INSERT INTO inventario_equipamentos
 (ConferenciaId,NumeroPatrimonio,NumeroSerie,Tipo,Marca,Modelo,Responsavel,Setor,Localizacao,Status,CapturadoNaCriacao)
 SELECT c.Id,e.NumeroPatrimonio,e.NumeroSerie,t.Nome,e.Marca,e.Modelo,f.Nome,s.Nome,e.Localizacao,e.Status,0
 FROM conferencias c JOIN equipamentos e ON e.Id=c.EquipamentoId
 JOIN tipos_equipamento t ON t.Id=e.TipoId
 LEFT JOIN funcionarios f ON f.Id=e.ResponsavelId LEFT JOIN setores s ON s.Id=f.SetorId
 LEFT JOIN inventario_equipamentos copia ON copia.ConferenciaId=c.Id
 WHERE copia.ConferenciaId IS NULL;
