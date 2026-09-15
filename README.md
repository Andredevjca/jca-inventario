# JCA Soluções — Inventário de equipamentos

Sistema independente em ASP.NET Core 10, C#, Dapper e MySQL. Frontend em HTML, CSS, JavaScript e Bootstrap, com Font Awesome e Nunito. O CSS de layout e de login e os arquivos do Bootstrap foram reaproveitados de `C:\controle-alugueis`; o projeto de referência não foi alterado.

## Executar nesta máquina

### Visual Studio

Abra `JcaInventario.slnx`, selecione o projeto `JcaInventario` como projeto de inicialização e execute o perfil **JcaInventario** com **F5** ou **Ctrl+F5**. A instalação do Visual Studio deve oferecer suporte ao SDK .NET 10 e ter a carga de trabalho de desenvolvimento ASP.NET instalada.

O servidor MySQL configurado precisa estar em execução. Para usar a instância local preparada pelo script enquanto depura no Visual Studio, execute `.\iniciar-local.ps1 -SomenteBanco` em um terminal e mantenha-o aberto.

A página principal está em `Views/Inicio/Index.cshtml`; o layout compartilhado está em `Views/Shared/_Layout.cshtml`. CSS e JavaScript continuam em `wwwroot`, consumindo a API REST.

No PowerShell, dentro desta pasta:

```powershell
.\iniciar-local.ps1
```

O script utiliza o MySQL 8.4 já instalado no Laragon, prepara uma instância própria na porta **3309**, restrita ao endereço local, e inicia o sistema em **http://localhost:5085**. Os dados ficam em `Dados/mysql-local`. Uma senha aleatória para o MySQL é criada e guardada nos arquivos locais ignorados pelo Git. Ao encerrar o script, o servidor MySQL iniciado por ele é desligado.

Se a política do PowerShell bloquear a execução do arquivo, execute:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-local.ps1
```

Esse comando aplica a opção somente ao processo, sem alterar a política permanente da máquina.

Para uma instalação do MySQL em outro caminho:

```powershell
.\iniciar-local.ps1 -ExecutavelMySql 'C:\caminho\mysql\bin\mysqld.exe'
```

## Acesso inicial

- **E-mail:** `admin@admin.com`
- **Senha:** `admin`

O administrador tem acesso completo. A senha inicial solicitada é armazenada somente como hash PBKDF2-SHA256, com sal aleatório e 600.000 iterações. Altere a senha em **Configurações → Usuários** antes de disponibilizar o sistema para outras pessoas. Uma senha já alterada nunca é redefinida pela inicialização.

## Usar um servidor MySQL existente

Pré-requisitos: SDK .NET 10 para desenvolvimento e servidor MySQL 8.0 ou superior em execução. A aplicação cria o banco e sua estrutura; não instala o serviço MySQL do sistema operacional.

Configure `ConnectionStrings:Banco` em `appsettings.Local.json` ou por variável de ambiente. O arquivo local não deve ser versionado. Exemplo para um servidor com TLS:

```powershell
$env:ConnectionStrings__Banco = 'Server=servidor;Port=3306;Database=jca_inventario;User ID=usuario;Password=sua-senha;SslMode=Required'
dotnet restore
dotnet run
```

Para uma conexão apenas local sem TLS, pode-se utilizar `SslMode=None;AllowPublicKeyRetrieval=True`, como faz o script de desenvolvimento. Use TLS e uma conta apropriada na instalação de produção.

A conta de conexão precisa de permissão para criar o banco, tabelas, índices e relacionamentos e para ler e gravar os dados. Não é necessário executar um arquivo SQL manualmente. O “usuário administrador” criado pela aplicação é o usuário do sistema web, separado da conta de conexão do MySQL.

## Inicialização automática

Antes de atender requisições, a aplicação:

1. Cria o banco configurado, se ausente, com `utf8mb4`.
2. Obtém uma trava no MySQL para impedir inicializações concorrentes.
3. Cria tabelas, relacionamentos e índices ausentes.
4. Insere os seis setores e onze tipos iniciais que ainda não existirem.
5. Cria `admin@admin.com` somente se esse e-mail ainda não existir.

Reiniciar não apaga registros, não duplica as cargas iniciais e não troca a senha existente. `CREATE TABLE IF NOT EXISTS` prepara esta primeira versão; futuras alterações em colunas de uma instalação existente devem ser entregues com uma migração específica.

## Funcionalidades

- Dashboard com indicadores, distribuição por setor/local/status e alertas.
- Equipamentos com configuração básica, patrimônio e série únicos quando informados, responsável, foto e histórico.
- Busca, filtros por tipo, setor, funcionário, localização e status, ordenação e paginação.
- Fotos JPG, PNG e WebP de até 5 MB, com prévia, alteração e remoção. Arquivos fora da pasta pública; acesso autenticado.
- Funcionários, setores, tipos e usuários; inativação com validação de vínculos.
- Entrega, devolução, transferência, mudança de localização e transferência de setor com histórico.
- Manutenções com datas, problema, solução, custo e responsável. A entrada envia à manutenção; a conclusão devolve ao estoque sem responsável.
- Conferência física por inventário, com pendências e divergências. Não permite encerrar com itens pendentes nem editar inventário encerrado.
- Termo de responsabilidade em PDF para equipamento com funcionário vinculado.
- Relatório filtrado com exportação CSV e impressão/PDF pelo navegador.

O cadastro e as movimentações são transacionais. Não há exclusão de equipamentos, funcionários ou histórico. Inativação de funcionário exige devolver ou transferir seus equipamentos; setores e tipos com vínculos ativos também são protegidos. Usuários operadores acessam os módulos operacionais; somente administradores gerenciam usuários.

## Estrutura

```text
Controllers/       API REST e autorização
Servicos/          Regras, movimentações, manutenção, senhas e PDF
Repositorios/      Consultas de equipamentos e histórico com Dapper
Modelos/          Entidades e validações
DTOs/             Solicitações da API
Configuracoes/    Opções permitidas no inventário
Infraestrutura/   Conexão e inicialização do MySQL
Views/            Página Razor e layout compartilhado
wwwroot/          CSS, JavaScript e Bootstrap
Testes/           Verificações de integração e inicialização
Dados/            Arquivos locais, fotos, chaves e MySQL de desenvolvimento
```

## Validação

```powershell
dotnet build
Get-Content .\wwwroot\js\aplicacao.js -Raw | node --check
```

Use **um banco exclusivo de testes** para os comandos abaixo. O teste HTTP cria registros e encerra inventários de teste. Não aponte para produção.

```powershell
$env:ConnectionStrings__Banco = 'Server=127.0.0.1;Port=3308;Database=jca_inventario_teste;User ID=root;Password=;SslMode=None'
dotnet run
# Em outro terminal, com a aplicação acima em execução:
.\Testes\integracao.ps1

$env:JCA_CONEXAO_TESTE = 'Server=127.0.0.1;Port=3308;Database=jca_inicializacao_teste;User ID=root;Password=;SslMode=None'
dotnet run --project .\Testes\Verificacoes.csproj
```

Foram executadas **32 verificações HTTP** e **9 verificações de inicialização, hash e PDF**, com MySQL 8.4 isolado. A compilação e a sintaxe JavaScript foram verificadas. A inspeção visual no navegador não pôde ser executada: as ferramentas de navegador e controle nativo não estavam disponíveis na sessão. O PDF foi verificado estruturalmente, sem renderização visual. A consulta de vulnerabilidades do NuGet não ficou disponível devido à restrição de rede do ambiente.

## Publicação e dados

```powershell
dotnet publish -c Release -o publicacao
```

Mantenha `Infraestrutura/estrutura.sql` junto à aplicação publicada. Configure a conexão no ambiente de destino e use HTTPS no servidor/proxy. Em produção Windows, as chaves de sessão persistem em `Dados/Chaves` protegidas por DPAPI para a conta do processo. Em desenvolvimento, as sessões são temporárias e exigem novo login ao reiniciar.

Inclua o banco MySQL e `Dados/Fotos` no backup. Restrinja o acesso aos arquivos de configuração, às chaves e à pasta de dados. Nunito e Font Awesome são carregados por CDN; Bootstrap e o CSS principal estão no próprio projeto.

