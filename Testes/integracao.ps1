param([string]$Endereco='http://localhost:5085')
$ErrorActionPreference='Stop'
$sessao=New-Object Microsoft.PowerShell.Commands.WebRequestSession
$script:token=(Invoke-RestMethod "$Endereco/api/conta/token" -WebSession $sessao).token
$script:verificacoes=0
function Requisitar([string]$Caminho,[string]$Metodo='GET',$Dados=$null) {
    $parametros=@{Uri="$Endereco/api/$Caminho";Method=$Metodo;WebSession=$sessao;Headers=@{'X-CSRF-TOKEN'=$script:token};ContentType='application/json; charset=utf-8'}
    if($null -ne $Dados){$parametros.Body=[System.Text.Encoding]::UTF8.GetBytes(($Dados|ConvertTo-Json -Depth 10 -Compress))}
    try { Invoke-RestMethod @parametros } catch { if($_.Exception.Response){$leitor=New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream()); Write-Host "Resposta em $Caminho : $($leitor.ReadToEnd())"}; throw }
}
function Verificar([bool]$Condicao,[string]$Descricao){if(!$Condicao){throw "FALHOU: $Descricao"};$script:verificacoes++;Write-Output "OK: $Descricao"}
function EsperarErro([string]$Caminho,[string]$Metodo,$Dados,[int]$Codigo){try{Requisitar $Caminho $Metodo $Dados|Out-Null;throw "Esperado HTTP $Codigo"}catch{if(!$_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne $Codigo){throw};Verificar $true "HTTP $Codigo em $Caminho"}}
EsperarErro 'equipamentos' 'GET' $null 401
Requisitar 'conta/entrar' 'POST' @{email='admin@admin.com';senha='admin'}|Out-Null
$script:token=(Requisitar 'conta/token').token
Verificar ((Requisitar 'conta/atual').administrador) 'Administrador inicial autentica com acesso completo'
$setores=Requisitar 'cadastros/setores';$tipos=Requisitar 'cadastros/tipos'
Verificar ($setores.Count -eq 6 -and $tipos.Count -eq 11) 'Dados iniciais presentes sem duplicação após reinicialização'
$marcaTeste=[DateTime]::Now.ToString('yyyyMMddHHmmssfff')
$funcionario=@{nome="Funcionário de teste $marcaTeste";email="teste.$marcaTeste@example.com";matricula=$marcaTeste;setorId=$setores[0].Id;tipoTrabalho='Home Office';ativo=$true}
$funcionario.id=(Requisitar 'cadastros/funcionarios' 'POST' $funcionario).id
$equipamento=@{tipoId=$tipos[0].Id;marca='Dell';modelo='Latitude de teste';numeroPatrimonio="JCA-T-$marcaTeste";numeroSerie="SERIE-$marcaTeste";valorAquisicao=4500;status='Em estoque';localizacao='Estoque';processador='Intel Core i5';memoriaRam='16 GB';armazenamento='512 GB SSD';sistemaOperacional='Windows';observacoes='Teste de acentuação: aquisição e manutenção.'}
$equipamento.id=(Requisitar 'equipamentos' 'POST' $equipamento).id
$detalhe=Requisitar "equipamentos/$($equipamento.id)"
Verificar ($detalhe.equipamento.Observacoes -eq $equipamento.observacoes) 'Dados e acentuação preservados em utf8mb4'
Verificar ($detalhe.historico.Count -eq 1 -and $detalhe.movimentacoes.Count -eq 1) 'Cadastro gera histórico e movimentação'
$copia=$equipamento.Clone();$copia.id=0
EsperarErro 'equipamentos' 'POST' $copia 409
$copia.numeroPatrimonio="OUTRO-$marcaTeste"
EsperarErro 'equipamentos' 'POST' $copia 409
$copia.numeroSerie=$null;$copia.status='Em uso'
EsperarErro 'equipamentos' 'POST' $copia 400
Requisitar "equipamentos/$($equipamento.id)/movimentacoes" 'POST' @{tipo='Entrega';responsavelId=$funcionario.id;status='Em uso';localizacao='Home Office';observacao='Entrega para trabalho remoto'}|Out-Null
$detalhe=Requisitar "equipamentos/$($equipamento.id)"
Verificar ($detalhe.equipamento.ResponsavelId -eq $funcionario.id -and $detalhe.movimentacoes[0].Tipo -eq 'Entrega') 'Entrega atualiza responsável e preserva movimento anterior'
$funcionario.ativo=$false
EsperarErro 'cadastros/funcionarios' 'POST' $funcionario 400
$funcionario.ativo=$true;$funcionario.setorId=$setores[1].Id
Requisitar 'cadastros/funcionarios' 'POST' $funcionario|Out-Null
$detalhe=Requisitar "equipamentos/$($equipamento.id)"
Verificar ($detalhe.movimentacoes[0].SetorAnteriorId -eq $setores[0].Id -and $detalhe.movimentacoes[0].NovoSetorId -eq $setores[1].Id) 'Troca de setor registra os setores anterior e novo'
$termo=Invoke-WebRequest "$Endereco/api/equipamentos/$($equipamento.id)/termo" -WebSession $sessao -UseBasicParsing
Verificar ($termo.Headers['Content-Type'] -like 'application/pdf*') 'Termo de responsabilidade retorna PDF'
[System.IO.File]::WriteAllBytes((Join-Path $PSScriptRoot '..\Dados\termo-teste.pdf'),$termo.Content)
$manutencao=@{equipamentoId=$equipamento.id;dataEntrada='2026-09-15';tipo='Corretiva';problema='Teclado com falha';responsavel='Assistência de teste';valor=150}
$manutencao.id=(Requisitar 'manutencoes' 'POST' $manutencao).id
Verificar ((Requisitar "equipamentos/$($equipamento.id)").equipamento.Status -eq 'Em manutenção') 'Entrada de manutenção atualiza situação'
$copiaManutencao=$manutencao.Clone();$copiaManutencao.id=0
EsperarErro 'manutencoes' 'POST' $copiaManutencao 400
EsperarErro "equipamentos/$($equipamento.id)/movimentacoes" 'POST' @{tipo='Devolução';status='Em estoque';localizacao='Estoque'} 400
$manutencao.dataSaida='2026-09-16';$manutencao.solucao='Teclado substituído'
Requisitar 'manutencoes' 'POST' $manutencao|Out-Null
$detalhe=Requisitar "equipamentos/$($equipamento.id)"
Verificar ($detalhe.equipamento.Status -eq 'Em estoque' -and !$detalhe.equipamento.ResponsavelId) 'Retorno de manutenção envia para estoque sem responsável'
EsperarErro 'manutencoes' 'POST' $manutencao 400
$inventario=(Requisitar 'inventarios' 'POST' @{nome="Inventário de teste $marcaTeste"}).id
EsperarErro "inventarios/$inventario/encerrar" 'POST' $null 400
EsperarErro "inventarios/$inventario/conferencias/$($equipamento.id)" 'POST' @{situacao='Divergência'} 400
$itens=(Requisitar "inventarios/$inventario").itens
foreach($item in $itens){Requisitar "inventarios/$inventario/conferencias/$($item.EquipamentoId)" 'POST' @{situacao='Conferido';observacao='Conferido no teste'}|Out-Null}
Requisitar "inventarios/$inventario/encerrar" 'POST' $null|Out-Null
Verificar ((Requisitar "inventarios/$inventario").inventario.Encerrado -eq 1) 'Inventário encerra após todas as conferências'
EsperarErro "inventarios/$inventario/conferencias/$($equipamento.id)" 'POST' @{situacao='Pendente'} 400
$detalhe=Requisitar "equipamentos/$($equipamento.id)"
Verificar ($detalhe.historico.Count -ge 6 -and $detalhe.movimentacoes.Count -eq 5) 'Histórico completo preservado ao longo do ciclo de vida'
Verificar ((Requisitar "cadastros/funcionarios/$($funcionario.id)").historico.Count -ge 3) 'Funcionário mantém histórico após devolver equipamento'
Add-Type -AssemblyName System.Net.Http
$manipulador=New-Object System.Net.Http.HttpClientHandler
$manipulador.CookieContainer=$sessao.Cookies
$cliente=New-Object System.Net.Http.HttpClient($manipulador)
$cliente.DefaultRequestHeaders.Add('X-CSRF-TOKEN',$script:token)
function EnviarFoto([string]$Nome,[byte[]]$Bytes){
    $conteudo=New-Object System.Net.Http.MultipartFormDataContent
    $imagem=New-Object System.Net.Http.ByteArrayContent -ArgumentList (,$Bytes)
    $conteudo.Add($imagem,'arquivo',$Nome)
    $resposta=$cliente.PostAsync("$Endereco/api/equipamentos/$($equipamento.id)/foto",$conteudo).GetAwaiter().GetResult()
    $conteudo.Dispose()
    return [int]$resposta.StatusCode
}
$imagemPng=[Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a5n8AAAAASUVORK5CYII=')
Verificar ((EnviarFoto 'foto.png' $imagemPng) -eq 200) 'Foto PNG pode ser enviada'
$foto=Invoke-WebRequest "$Endereco/api/equipamentos/$($equipamento.id)/foto" -WebSession $sessao -UseBasicParsing
Verificar ($foto.Headers['Content-Type'] -eq 'image/png') 'Foto pode ser consultada com autenticação'
Verificar ((EnviarFoto 'arquivo.exe' $imagemPng) -eq 400) 'Extensão de foto inválida é rejeitada'
Verificar ((EnviarFoto 'foto.png' ([System.Text.Encoding]::UTF8.GetBytes('nao e imagem'))) -eq 400) 'Conteúdo incompatível com a extensão é rejeitado'
$grande=New-Object byte[] (5*1024*1024+1)
Verificar ((EnviarFoto 'foto.png' $grande) -eq 400) 'Foto acima de 5 MB é rejeitada'
Requisitar "equipamentos/$($equipamento.id)/foto" 'DELETE' $null|Out-Null
Verificar (!(Requisitar "equipamentos/$($equipamento.id)").equipamento.Foto) 'Foto removida sem apagar o equipamento'
$cliente.Dispose()
$operador=@{nome='Operador de teste';email="operador.$marcaTeste@example.com";senha='SenhaTeste123!';administrador=$false;ativo=$true}
$operador.id=(Requisitar 'cadastros/usuarios' 'POST' $operador).id
$tokenValido=$script:token;$script:token='invalido'
EsperarErro 'cadastros/setores' 'POST' @{nome='Setor inválido'} 400
$script:token=$tokenValido
Requisitar 'conta/sair' 'POST' $null|Out-Null
$script:token=(Requisitar 'conta/token').token
Requisitar 'conta/entrar' 'POST' @{email=$operador.email;senha=$operador.senha}|Out-Null
$script:token=(Requisitar 'conta/token').token
EsperarErro 'cadastros/usuarios' 'GET' $null 403
Verificar ((Requisitar 'equipamentos').Count -ge 1) 'Operador acessa o inventário'
Requisitar 'conta/sair' 'POST' $null|Out-Null
Write-Output "RESULTADO: $script:verificacoes verificações aprovadas. Dados criados exclusivamente no banco configurado para os testes."



