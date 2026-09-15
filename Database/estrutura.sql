CREATE TABLE IF NOT EXISTS usuarios (
 Id INT PRIMARY KEY AUTO_INCREMENT, Nome VARCHAR(160) NOT NULL, Email VARCHAR(190) NOT NULL UNIQUE,
 SenhaHash VARCHAR(300) NOT NULL, Administrador BOOLEAN NOT NULL DEFAULT 0, Ativo BOOLEAN NOT NULL DEFAULT 1
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS setores (
 Id INT PRIMARY KEY AUTO_INCREMENT, Nome VARCHAR(160) NOT NULL UNIQUE, Ativo BOOLEAN NOT NULL DEFAULT 1, Observacoes TEXT
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS tipos_equipamento (
 Id INT PRIMARY KEY AUTO_INCREMENT, Nome VARCHAR(160) NOT NULL UNIQUE, Ativo BOOLEAN NOT NULL DEFAULT 1, Observacoes TEXT
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS funcionarios (
 Id INT PRIMARY KEY AUTO_INCREMENT, Nome VARCHAR(160) NOT NULL, Email VARCHAR(190), Telefone VARCHAR(40), Matricula VARCHAR(60) UNIQUE,
 Cargo VARCHAR(120), SetorId INT NOT NULL, TipoTrabalho VARCHAR(30) NOT NULL, Ativo BOOLEAN NOT NULL DEFAULT 1, Observacoes TEXT,
 FOREIGN KEY (SetorId) REFERENCES setores(Id), INDEX ix_funcionarios_nome (Nome)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS equipamentos (
 Id INT PRIMARY KEY AUTO_INCREMENT, TipoId INT NOT NULL, Marca VARCHAR(100) NOT NULL, Modelo VARCHAR(160) NOT NULL,
 NumeroPatrimonio VARCHAR(100) UNIQUE, NumeroSerie VARCHAR(150) UNIQUE, DataAquisicao DATE, ValorAquisicao DECIMAL(14,2) NOT NULL DEFAULT 0,
 Status VARCHAR(40) NOT NULL, Localizacao VARCHAR(40) NOT NULL, ResponsavelId INT NULL, Observacoes TEXT,
 Processador VARCHAR(160), MemoriaRam VARCHAR(100), Armazenamento VARCHAR(100), SistemaOperacional VARCHAR(160), Foto VARCHAR(200),
 CriadoEm DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, AtualizadoEm DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 FOREIGN KEY (TipoId) REFERENCES tipos_equipamento(Id), FOREIGN KEY (ResponsavelId) REFERENCES funcionarios(Id),
 INDEX ix_equipamentos_status (Status), INDEX ix_equipamentos_localizacao (Localizacao)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS movimentacoes (
 Id INT PRIMARY KEY AUTO_INCREMENT, EquipamentoId INT NOT NULL, ResponsavelAnteriorId INT NULL, NovoResponsavelId INT NULL,
 SetorAnteriorId INT NULL, NovoSetorId INT NULL, LocalizacaoAnterior VARCHAR(40), NovaLocalizacao VARCHAR(40),
 StatusAnterior VARCHAR(40), NovoStatus VARCHAR(40), Tipo VARCHAR(60) NOT NULL, Data DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 UsuarioId INT NOT NULL, Observacao TEXT,
 FOREIGN KEY (EquipamentoId) REFERENCES equipamentos(Id), FOREIGN KEY (ResponsavelAnteriorId) REFERENCES funcionarios(Id),
 FOREIGN KEY (NovoResponsavelId) REFERENCES funcionarios(Id), FOREIGN KEY (SetorAnteriorId) REFERENCES setores(Id),
 FOREIGN KEY (NovoSetorId) REFERENCES setores(Id), FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id), INDEX ix_movimentacoes_data (EquipamentoId,Data)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS manutencoes (
 Id INT PRIMARY KEY AUTO_INCREMENT, EquipamentoId INT NOT NULL, DataEntrada DATE NOT NULL, DataSaida DATE,
 Tipo VARCHAR(100) NOT NULL, Problema TEXT NOT NULL, Solucao TEXT, Valor DECIMAL(14,2) NOT NULL DEFAULT 0,
 Responsavel VARCHAR(160) NOT NULL, Observacoes TEXT, UsuarioId INT NOT NULL,
 FOREIGN KEY (EquipamentoId) REFERENCES equipamentos(Id), FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id), INDEX ix_manutencoes_abertas (EquipamentoId,DataSaida)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS inventarios (
 Id INT PRIMARY KEY AUTO_INCREMENT, Nome VARCHAR(160) NOT NULL, Data DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 Encerrado BOOLEAN NOT NULL DEFAULT 0, UsuarioId INT NOT NULL, FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS conferencias (
 Id INT PRIMARY KEY AUTO_INCREMENT, InventarioId INT NOT NULL, EquipamentoId INT NOT NULL,
 Situacao VARCHAR(30) NOT NULL DEFAULT 'Pendente', Data DATETIME NULL, UsuarioId INT NULL, Observacao TEXT,
 FOREIGN KEY (InventarioId) REFERENCES inventarios(Id), FOREIGN KEY (EquipamentoId) REFERENCES equipamentos(Id),
 FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id), UNIQUE KEY ux_conferencia (InventarioId,EquipamentoId)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS historico (
 Id INT PRIMARY KEY AUTO_INCREMENT, EquipamentoId INT NOT NULL, Data DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 UsuarioId INT NOT NULL, Descricao VARCHAR(250) NOT NULL, DadosAnteriores JSON NULL, DadosNovos JSON NULL,
 FOREIGN KEY (EquipamentoId) REFERENCES equipamentos(Id), FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id), INDEX ix_historico_data (EquipamentoId,Data)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS inventario_equipamentos (
 ConferenciaId INT PRIMARY KEY,
 NumeroPatrimonio VARCHAR(100), NumeroSerie VARCHAR(150), Tipo VARCHAR(160),
 Marca VARCHAR(100), Modelo VARCHAR(160), Responsavel VARCHAR(160), Setor VARCHAR(160),
 Localizacao VARCHAR(40), Status VARCHAR(40),
 CapturadoEm DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
 CapturadoNaCriacao BOOLEAN NOT NULL,
 FOREIGN KEY (ConferenciaId) REFERENCES conferencias(Id)
) ENGINE=InnoDB;

-- Migra somente registros antigos sem copia; nunca sobrescreve o historico salvo.
INSERT INTO inventario_equipamentos
 (ConferenciaId,NumeroPatrimonio,NumeroSerie,Tipo,Marca,Modelo,Responsavel,Setor,Localizacao,Status,CapturadoNaCriacao)
 SELECT c.Id,e.NumeroPatrimonio,e.NumeroSerie,t.Nome,e.Marca,e.Modelo,f.Nome,s.Nome,e.Localizacao,e.Status,0
 FROM conferencias c JOIN equipamentos e ON e.Id=c.EquipamentoId
 JOIN tipos_equipamento t ON t.Id=e.TipoId
 LEFT JOIN funcionarios f ON f.Id=e.ResponsavelId LEFT JOIN setores s ON s.Id=f.SetorId
 LEFT JOIN inventario_equipamentos copia ON copia.ConferenciaId=c.Id
 WHERE copia.ConferenciaId IS NULL;
