# JCA Soluções — Inventário de equipamentos

Sistema independente em ASP.NET Core 10, C#, Dapper e SQL Server. Frontend em HTML, CSS, JavaScript e Bootstrap, com Font Awesome e Nunito. O CSS de layout e de login e os arquivos do Bootstrap foram reaproveitados de `C:\controle-alugueis`; o projeto de referência não foi alterado.

## Executar nesta máquina

### Visual Studio

Abra `JcaInventario.slnx`, selecione o projeto `JcaInventario` como projeto de inicialização e execute o perfil **JcaInventario** com **F5** ou **Ctrl+F5**. A instalação do Visual Studio deve oferecer suporte ao SDK .NET 10 e ter a carga de trabalho de desenvolvimento ASP.NET instalada.

O SQL Server configurado precisa estar acessível. O script abaixo inicia somente a aplicação em **http://localhost:5085**, usando a conexão configurada; ele não instala nem inicia um banco de dados.

```powershell
.\iniciar-local.ps1
```

## Acesso inicial

- **E-mail:** `admin@admin.com`
- **Senha:** `admin`

O administrador tem acesso completo. A senha inicial solicitada é armazenada somente como hash PBKDF2-SHA256, com sal aleatório e 600.000 iterações. Altere a senha em **Configurações → Usuários** antes de disponibilizar o sistema para outras pessoas. Uma senha já alterada nunca é redefinida pela inicialização.

## SQL Server e conexão

Configure `ConnectionStrings:Banco` em `appsettings.json`, `appsettings.Local.json` ou na variável de ambiente `ConnectionStrings__Banco` (nessa ordem de prioridade crescente). O arquivo local não é publicado. O banco configurado nesta instalação é **dbActyon_Inventario**, no servidor **192.168.2.154**.

Exemplo sem credenciais reais:

```text
Server=192.168.2.154;Database=dbActyon_Inventario;User ID=usuario;Password=sua-senha;Encrypt=True;TrustServerCertificate=True
```

A conexão usa TLS e aceita o certificado apresentado pelo servidor interno (`TrustServerCertificate=True`). Se houver um certificado válido para o endereço usado, configure essa opção como `False`.

O projeto usa [Microsoft.Data.SqlClient 6.1.6](https://www.nuget.org/packages/Microsoft.Data.SqlClient/6.1.6). A imagem do SSMS informa a versão da ferramenta, não a versão do mecanismo SQL Server; consulte `SELECT @@VERSION` para identificar o servidor.

## Inicialização automática e instalação manual

A aplicação cria o banco configurado se estiver ausente, obtém uma trava de sessão no SQL Server e cria as tabelas, índices e relacionamentos. Depois insere somente os setores, tipos e administrador inicial que ainda não existirem. A conta de conexão precisa das permissões correspondentes.

Para preparar o banco manualmente no SSMS:

1. Execute `Database/criar-banco.sql`, que cria **dbActyon_Inventario** se necessário.
2. Selecione **dbActyon_Inventario** na janela de consulta e execute `Database/estrutura.sql`.
3. Inicie a aplicação para inserir os cadastros iniciais.

Reexecutar os scripts não apaga registros nem recria tabelas existentes. Patrimônio, série e matrícula usam índices únicos filtrados para permitir vários valores nulos. Textos usam `NVARCHAR`, os IDs usam `IDENTITY`, e os documentos JSON do histórico ficam em `NVARCHAR(MAX)`.

Esta conversão cria a estrutura no SQL Server; **não transfere os registros de uma instalação MySQL**. Preserve o banco antigo e seus backups caso seja necessária uma migração de dados. Não execute um dump MySQL diretamente no SQL Server.

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
Infraestrutura/   Conexão e inicialização do SQL Server
Views/            Página Razor e layout compartilhado
wwwroot/          CSS, JavaScript e Bootstrap
Dados/            Arquivos locais, fotos e chaves
```

## Validação

As pastas `Dados/` e `publicacao/` são locais e ignoradas pelo Git, assim como os artefatos de compilação e as configurações locais. Cada desenvolvedor deve configurar seu próprio SQL Server conforme as instruções acima.

```powershell
dotnet build
Get-Content .\wwwroot\js\aplicacao.js -Raw | node --check
```

## Publicação e dados

```powershell
.\publicar-iis.ps1
```

A publicação fica em `publicacao/win-x64` e inclui o runtime .NET 10 para Windows 64 bits. Copie todo o conteúdo dessa pasta, incluindo o `web.config`, para o site no IIS. Pare o pool durante a substituição e preserve `Dados` e as configurações específicas do servidor. No pool, use **No Managed Code** e **Enable 32-Bit Applications = False**.

O IIS ainda precisa do ASP.NET Core Module V2, instalado pelo [Hosting Bundle](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-10.0). Se o módulo estiver desatualizado, instale ou repare o Hosting Bundle do .NET 10 no servidor e reinicie o IIS. O Windows do servidor também precisa ser compatível com .NET 10. A publicação com runtime incluído resolve a dependência do runtime compartilhado associada ao erro 500.31, mas não substitui o módulo do IIS.

Mantenha `Database/estrutura.sql` junto à aplicação publicada. Configure a conexão no ambiente de destino e use HTTPS no servidor/proxy. Em produção Windows, as chaves de sessão persistem em `Dados/Chaves` protegidas por DPAPI para a conta do processo. Em desenvolvimento, as sessões são temporárias e exigem novo login ao reiniciar.

Inclua o banco SQL Server e `Dados/Fotos` no backup. Restrinja o acesso aos arquivos de configuração, às chaves e à pasta de dados. Nunito e Font Awesome são carregados por CDN; Bootstrap e o CSS principal estão no próprio projeto.


## PDF do inventário por setor

Na lista de inventários, use **PDF por setor**, ou abra um inventário e clique em **Exportar PDF por setor**. A exportação funciona para inventários em andamento e encerrados e inclui resumo geral, totais por setor, identificação dos equipamentos, responsáveis, localização, status, conferências e observações.

Os novos inventários preservam os dados dos equipamentos na criação, na tabela `inventario_equipamentos`. Alterações posteriores nos equipamentos, funcionários e setores não modificam esses dados. A conferência continua editável até o encerramento.

Ao reiniciar a aplicação atualizada, o script `Database/estrutura.sql` cria a tabela e captura os dados disponíveis dos inventários antigos que ainda não possuem histórico detalhado. Essa captura não reconstitui a situação original: a tela e o PDF identificam esses registros como legados, e o PDF informa a data da captura. Novas inicializações preservam as cópias existentes.

## Funcionários, funções e auditoria

O cadastro de funcionários inclui `Endereco`, `Numero`, `Cep`, `Bairro`, `Cidade`, `Uf`, `Complemento`, `DataNascimento` e `DataAdmissao`, seguindo o padrão PascalCase do banco. O campo Cargo seleciona uma função do cadastro **Cadastros > Funções**, vinculado por `FuncaoId`. Funções podem ser criadas, renomeadas e inativadas; funções com funcionários ativos não podem ser inativadas. Os cargos legados são migrados para funções, preservando os vínculos. `Cargo` continua sincronizado para compatibilidade com os relatórios existentes.

A consulta no navegador usa [ViaCEP](https://viacep.com.br/), preenche endereço, bairro, cidade, UF e complemento e permite correções manuais. O número é informado pelo usuário. CEP inexistente, falhas e tempo limite são informados sem impedir o preenchimento manual.

Mantenha também `Database/funcionarios-auditoria.sql` na publicação. A inicialização executa esse script depois de `estrutura.sql`, com trava de inicialização, transação e verificações que permitem executá-lo novamente. Para instalação manual, execute os dois arquivos nessa ordem.

As 12 tabelas da aplicação possuem `DataInclusao`, `DataAlteracao`, `UsuarioInclusao` e `UsuarioAlteracao`. Triggers registram datas UTC e o ID do usuário autenticado, enviado em `SESSION_CONTEXT` a cada conexão aberta por `Banco`. Na inclusão, os campos da última alteração ficam nulos; atualizações preservam a inclusão e registram a última alteração. Os IDs referenciam `usuarios.Id`. Registros legados sem informação confiável mantêm os campos nulos, assim como o usuário em operações automáticas ou SQL executado sem contexto. Para SQL externo auditado, defina `sys.sp_set_session_context @key=N'UsuarioId', @value=<id>` na mesma conexão.

Esses campos identificam a criação e a última alteração; não armazenam todas as versões, exclusões ou quais campos foram modificados. Um histórico completo de auditoria poderá ser implementado posteriormente. Novas tabelas deverão ser adicionadas à lista do script de auditoria.

Validações:

```powershell
dotnet build --no-restore
node Tests/viacep.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File Tests/Validar-Auditoria.ps1
```

O teste de SQL usa a conexão de `appsettings.Local.json`, executa a migração duas vezes, verifica auditoria nas 12 tabelas e sempre desfaz a transação. Não deixa registros de teste persistidos, mas pode avançar sequências de identidade.
