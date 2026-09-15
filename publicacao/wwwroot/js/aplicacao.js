'use strict';
const elemento = seletor => document.querySelector(seletor);
const escapar = valor => String(valor ?? '').replace(/[&<>"']/g, caractere => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[caractere]));
const moeda = valor => Number(valor || 0).toLocaleString('pt-BR', {style:'currency',currency:'BRL'});
const data = valor => valor ? new Date(String(valor).length === 10 ? `${valor}T12:00:00` : valor).toLocaleDateString('pt-BR') : '—';
const dataHora = valor => valor ? new Date(valor).toLocaleString('pt-BR') : '—';
const dataCampo = valor => valor ? String(valor).slice(0,10) : '';
const hoje = () => { const atual = new Date(); return `${atual.getFullYear()}-${String(atual.getMonth()+1).padStart(2,'0')}-${String(atual.getDate()).padStart(2,'0')}`; };
const normalizar = valor => Array.isArray(valor) ? valor.map(normalizar) : valor && typeof valor === 'object' ? Object.fromEntries(Object.entries(valor).map(([chave,conteudo]) => [chave[0].toLowerCase()+chave.slice(1),normalizar(conteudo)])) : valor;
let token = '', usuarioAtual, referencias = {}, salvarFormulario, tipoFormulario, dadosFormulario, fotoSelecionada, removerFoto = false;
let navegando = false, navegarNovamente = false;
const modal = new bootstrap.Modal(elemento('#modalCadastro'));
const modalConfirmacao = new bootstrap.Modal(elemento('#modalConfirmacao'));
let resolverConfirmacao;
const titulos = {dashboard:'Dashboard',equipamentos:'Equipamentos',movimentacoes:'Movimentações',manutencoes:'Manutenções',inventarios:'Inventários',funcionarios:'Funcionários',setores:'Setores',tipos:'Tipos de equipamento',relatorios:'Relatórios',usuarios:'Usuários'};
const icones = {dashboard:'gauge-high',equipamentos:'laptop',movimentacoes:'arrow-right-arrow-left',manutencoes:'screwdriver-wrench',inventarios:'clipboard-check',funcionarios:'users',setores:'building',tipos:'tags',relatorios:'chart-column',usuarios:'user-gear'};

async function api(caminho, opcoes = {}) {
    const cabecalhos = {...opcoes.headers};
    if (opcoes.method && opcoes.method !== 'GET') cabecalhos['X-CSRF-TOKEN'] = token;
    if (opcoes.body && !(opcoes.body instanceof FormData)) { cabecalhos['Content-Type'] = 'application/json'; opcoes.body = JSON.stringify(opcoes.body); }
    const resposta = await fetch(`/api/${caminho}`, {...opcoes,headers:cabecalhos});
    if (resposta.status === 204) return null;
    const conteudo = await resposta.json().catch(() => ({}));
    if (!resposta.ok) {
        if (resposta.status === 401 && caminho !== 'conta/entrar') mostrarEntrada();
        const validacoes = conteudo.errors ? Object.values(conteudo.errors).flat().join(' ') : '';
        throw new Error(conteudo.mensagem || validacoes || (resposta.status === 429 ? 'Muitas tentativas. Aguarde um minuto.' : resposta.status === 403 ? 'Você não tem permissão para esta ação.' : 'Não foi possível concluir a solicitação.'));
    }
    return normalizar(conteudo);
}
async function atualizarToken() { token = (await api('conta/token')).token; }
function avisar(texto) { elemento('#textoAviso').textContent = texto; bootstrap.Toast.getOrCreateInstance(elemento('#aviso')).show(); }
function confirmar(texto) { elemento('#textoConfirmacao').textContent = texto; modalConfirmacao.show(); return new Promise(resolver => resolverConfirmacao = resolver); }
elemento('#confirmarAcao').onclick = () => { resolverConfirmacao?.(true); resolverConfirmacao = null; modalConfirmacao.hide(); };
elemento('#modalConfirmacao').addEventListener('hidden.bs.modal', () => { resolverConfirmacao?.(false); resolverConfirmacao = null; });
function mostrarEntrada() { usuarioAtual = null; elemento('#sistema').hidden = true; elemento('#entrada').hidden = false; document.body.className = 'login-body'; }
async function carregarReferencias() {
    const [setores,tipos,funcionarios,opcoes] = await Promise.all(['setores','tipos','funcionarios','opcoes'].map(nome => api(`cadastros/${nome}`)));
    referencias = {setores,tipos,funcionarios,...opcoes};
}
async function mostrarSistema() {
    usuarioAtual = await api('conta/atual'); await atualizarToken(); await carregarReferencias();
    elemento('#nomeUsuario').textContent = usuarioAtual.nome;
    elemento('#entrada').hidden = true; elemento('#sistema').hidden = false; document.body.className = 'app-body';
    const grupos = [['Menu principal',['dashboard']],['Inventário',['equipamentos','movimentacoes','manutencoes','inventarios']],['Cadastros',['funcionarios','setores','tipos']],['Consultas',['relatorios']],...(usuarioAtual.administrador ? [['Configurações',['usuarios']]] : [])];
    elemento('#menu').innerHTML = grupos.map(([titulo,itens]) => `<li class="menu-label">${titulo}</li>${itens.map(chave => `<li><a href="#${chave}" title="${titulos[chave]}" data-menu="${chave}"><i class="fa-solid fa-${icones[chave]}"></i><span class="menu-texto">${titulos[chave]}</span></a></li>`).join('')}`).join('');
    await navegar();
}
elemento('#formularioEntrada').onsubmit = async evento => {
    evento.preventDefault(); const botao = evento.target.querySelector('button'); botao.disabled = true;
    elemento('#erroEntrada').textContent = '';
    try { await atualizarToken(); const campos = new FormData(evento.target); await api('conta/entrar',{method:'POST',body:{email:campos.get('email'),senha:campos.get('senha'),lembrar:campos.has('lembrar')}}); evento.target.reset(); await mostrarSistema(); }
    catch (erro) { elemento('#erroEntrada').textContent = erro.message; } finally { botao.disabled = false; }
};
elemento('#sair').onclick = async () => { try { await api('conta/sair',{method:'POST'}); mostrarEntrada(); await atualizarToken(); } catch (erro) { avisar(erro.message); } };
elemento('#alternarMenu').onclick = () => document.body.classList.toggle(window.innerWidth < 992 ? 'sidebar-open' : 'sidebar-collapsed');
elemento('#fundoMenu').onclick = () => document.body.classList.remove('sidebar-open');
window.addEventListener('hashchange', () => usuarioAtual && navegar());
function cabecalho(titulo,subtitulo,acoes = '') { return `<div class="page-header"><div><h1>${escapar(titulo)}</h1><p class="text-muted mb-0 mt-1">${escapar(subtitulo)}</p></div><div class="d-flex gap-2 sem-impressao">${acoes}</div></div>`; }
function botaoNovo(texto = 'Novo registro') { return `<button class="btn btn-success btn-modern" id="novo"><i class="fa-solid fa-plus"></i> ${escapar(texto)}</button>`; }
function badge(valor) { const classe = ['Ativo','Conferido','Disponível','Em uso','Encerrado'].includes(valor) ? 'status-pago' : ['Em manutenção','Pendente','Home Office'].includes(valor) ? 'status-pendente' : ['Divergência','Baixado'].includes(valor) ? 'status-atrasado' : 'status-info'; return `<span class="status-badge ${classe}">${escapar(valor)}</span>`; }
function foto(equipamento,detalhe = false) { return equipamento.foto ? `<img class="${detalhe ? 'foto-detalhe' : 'foto-lista'}" src="/api/equipamentos/${equipamento.id}/foto?v=${encodeURIComponent(equipamento.foto)}" alt="Foto de ${escapar(equipamento.modelo)}" loading="lazy">` : `<span class="${detalhe ? 'foto-detalhe d-grid' : 'foto-lista'} sem-foto"><i class="fa-solid fa-laptop"></i></span>`; }
function acoesRegistro(registro,detalhes = true) { return `${detalhes ? `<button class="btn btn-sm btn-outline-secondary" data-acao="detalhes" data-id="${registro.id}" title="Visualizar"><i class="fa-solid fa-eye"></i></button>` : ''} <button class="btn btn-sm btn-outline-secondary" data-acao="editar" data-id="${registro.id}" title="Editar"><i class="fa-solid fa-pen"></i></button>`; }
function tabela(colunas,itens) { return `<div class="table-responsive"><table class="table table-modern"><thead><tr>${colunas.map(coluna => `<th>${escapar(coluna.titulo)}</th>`).join('')}</tr></thead><tbody>${itens.length ? itens.map(item => `<tr>${colunas.map(coluna => `<td>${coluna.exibir ? coluna.exibir(item) : escapar(item[coluna.chave] ?? '—')}</td>`).join('')}</tr>`).join('') : `<tr><td colspan="${colunas.length}" class="vazio">Nenhum registro encontrado.</td></tr>`}</tbody></table></div>`; }
function listaPaginada(seletor,itens,colunas,aoAcionar,opcoes = {}) {
    const raiz = elemento(seletor); let pagina = 1, busca = '', ordenar = opcoes.ordenar || colunas.find(coluna => coluna.chave)?.chave || 'id', direcao = 1;
    const filtrados = () => itens.filter(item => (!opcoes.filtro || opcoes.filtro(item)) && Object.values(item).some(valor => String(valor ?? '').toLocaleLowerCase('pt-BR').includes(busca))).sort((a,b) => typeof a[ordenar] === 'number' ? (a[ordenar]-b[ordenar])*direcao : String(a[ordenar] ?? '').localeCompare(String(b[ordenar] ?? ''),'pt-BR',{numeric:true})*direcao);
    raiz.innerHTML = `<div class="content-card p-3"><div class="row g-2 mb-3 sem-impressao"><div class="col-md-6"><label class="form-label" for="buscaLista">Buscar</label><input id="buscaLista" type="search" class="form-control" placeholder="Buscar nos registros..."></div><div class="col-md-4"><label class="form-label" for="ordenacaoLista">Ordenar por</label><select id="ordenacaoLista" class="form-select">${colunas.filter(coluna => coluna.chave).map(coluna => `<option value="${coluna.chave}" ${coluna.chave === ordenar ? 'selected' : ''}>${escapar(coluna.titulo)}</option>`).join('')}</select></div><div class="col-md-2 d-flex align-items-end"><button class="btn btn-outline-secondary w-100" id="direcaoLista" title="Inverter ordenação">Crescente</button></div></div><div data-tabela></div><div class="d-flex justify-content-between align-items-center sem-impressao" data-paginacao></div></div>`;
    function desenhar() {
        const registros = filtrados(), totalPaginas = Math.max(1,Math.ceil(registros.length/15)); pagina = Math.min(pagina,totalPaginas);
        raiz.querySelector('[data-tabela]').innerHTML = tabela(colunas,registros.slice((pagina-1)*15,pagina*15));
        raiz.querySelector('[data-paginacao]').innerHTML = `<span class="text-muted">${registros.length} registro(s) · Página ${pagina} de ${totalPaginas}</span><div class="btn-group"><button class="btn btn-outline-secondary" data-pagina="-1" ${pagina===1?'disabled':''}>Anterior</button><button class="btn btn-outline-secondary" data-pagina="1" ${pagina===totalPaginas?'disabled':''}>Próxima</button></div>`;
    }
    raiz.querySelector('#buscaLista').oninput = evento => { busca = evento.target.value.toLocaleLowerCase('pt-BR').trim(); pagina = 1; desenhar(); };
    raiz.querySelector('#ordenacaoLista').onchange = evento => { ordenar = evento.target.value; desenhar(); };
    raiz.querySelector('#direcaoLista').onclick = evento => { direcao *= -1; evento.target.textContent = direcao === 1 ? 'Crescente' : 'Decrescente'; desenhar(); };
    raiz.onclick = evento => { const paginador = evento.target.closest('[data-pagina]'); if (paginador) { pagina += Number(paginador.dataset.pagina); desenhar(); } const acao = evento.target.closest('[data-acao]'); if (acao && aoAcionar) Promise.resolve(aoAcionar(acao.dataset.acao,Number(acao.dataset.id))).catch(erro => avisar(erro.message)); };
    desenhar(); return {atualizar:desenhar,filtrados};
}
const colunasEquipamentos = [
    {titulo:'Foto',exibir:item=>foto(item)}, {titulo:'Patrimônio',chave:'numeroPatrimonio',exibir:item=>`<a class="link-tabela" href="#equipamentos/${item.id}">${escapar(item.numeroPatrimonio || 'Sem patrimônio')}</a>`},
    {titulo:'Tipo',chave:'tipo'},{titulo:'Marca',chave:'marca'},{titulo:'Modelo',chave:'modelo'},{titulo:'Responsável',chave:'responsavel'},{titulo:'Setor',chave:'setor'},{titulo:'Localização',chave:'localizacao'}, {titulo:'Status',chave:'status',exibir:item=>badge(item.status)}, {titulo:'Ações',exibir:item=>acoesRegistro(item)}
];
const colunasMovimentacoes = [{titulo:'Data',chave:'data',exibir:item=>dataHora(item.data)},{titulo:'Patrimônio',chave:'numeroPatrimonio',exibir:item=>`<a href="#equipamentos/${item.equipamentoId}">${escapar(item.numeroPatrimonio || item.modelo)}</a>`},{titulo:'Tipo',chave:'tipo'},{titulo:'Responsável anterior',chave:'responsavelAnterior'},{titulo:'Novo responsável',chave:'novoResponsavel'},{titulo:'Setor anterior',chave:'setorAnterior'},{titulo:'Novo setor',chave:'novoSetor'},{titulo:'Local anterior',chave:'localizacaoAnterior'},{titulo:'Novo local',chave:'novaLocalizacao'},{titulo:'Usuário',chave:'usuario'},{titulo:'Observação',chave:'observacao'}];
const colunasManutencoes = [{titulo:'Patrimônio',chave:'numeroPatrimonio'},{titulo:'Equipamento',chave:'modelo'},{titulo:'Entrada',chave:'dataEntrada',exibir:item=>data(item.dataEntrada)},{titulo:'Saída',chave:'dataSaida',exibir:item=>data(item.dataSaida)},{titulo:'Tipo',chave:'tipo'},{titulo:'Problema',chave:'problema'},{titulo:'Solução',chave:'solucao'},{titulo:'Valor',chave:'valor',exibir:item=>moeda(item.valor)},{titulo:'Responsável',chave:'responsavel'},{titulo:'Situação',exibir:item=>badge(item.dataSaida?'Encerrado':'Em manutenção')},{titulo:'Ações',exibir:item=>item.dataSaida?'—':acoesRegistro(item,false)}];
async function navegar() {
    if (navegando) { navegarNovamente = true; return; }
    navegando = true;
    const [pagina = 'dashboard',id] = location.hash.slice(1).split('/');
    document.body.classList.remove('sidebar-open'); elemento('#tituloPagina').textContent = titulos[pagina] || 'Dashboard'; document.title = `${titulos[pagina] || 'Inventário'} · JCA Soluções`;
    document.querySelectorAll('[data-menu]').forEach(link => link.classList.toggle('active',link.dataset.menu===pagina));
    elemento('#conteudo').innerHTML = '<div class="content-card p-4 text-muted" role="status">Carregando...</div>';
    try {
        if (id && pagina==='equipamentos') await detalhesEquipamento(Number(id));
        else if (id && pagina==='funcionarios') await detalhesFuncionario(Number(id));
        else if (id && pagina==='setores') await detalhesSetor(Number(id));
        else if (id && pagina==='inventarios') await detalhesInventario(Number(id));
        else if (pagina==='dashboard') await dashboard();
        else if (pagina==='equipamentos' || pagina==='relatorios') await equipamentos(pagina==='relatorios');
        else if (pagina==='movimentacoes') await movimentacoes();
        else if (pagina==='manutencoes') await manutencoes();
        else if (pagina==='inventarios') await inventarios();
        else if (['funcionarios','setores','tipos','usuarios'].includes(pagina)) await cadastros(pagina);
        else location.hash='dashboard';
    } catch (erro) { elemento('#conteudo').innerHTML = `<div class="alert alert-danger" role="alert">${escapar(erro.message)}</div><button class="btn btn-outline-secondary" id="tentarNovamente">Tentar novamente</button>`; elemento('#tentarNovamente')?.addEventListener('click',navegar); }
    finally { navegando = false; if (navegarNovamente) { navegarNovamente = false; await navegar(); } }
}
async function dashboard() {
    const dados = await api('dashboard'), totais = dados.totais;
    const indicadores = [['Total de equipamentos',totais.total,'laptop','green'],['Em uso',totais.emUso,'user-check','blue'],['Em estoque',totais.emEstoque,'boxes-stacked','purple'],['Em manutenção',totais.emManutencao,'screwdriver-wrench','yellow'],['Home Office',totais.homeOffice,'house-laptop','blue'],['Baixados',totais.baixados,'box-archive','red']];
    const grafico = (titulo,itens) => `<div class="col-lg-4"><section class="content-card p-3 h-100"><h2 class="fw-bold mb-3">${titulo}</h2>${itens.length ? itens.map(item=>`<div class="mb-3"><div class="d-flex justify-content-between mb-1"><span>${escapar(item.nome)}</span><strong>${item.total}</strong></div><div class="barra-resumo"><span style="width:${Math.round(item.total/Math.max(totais.total,1)*100)}%"></span></div></div>`).join('') : '<p class="text-muted">Cadastre equipamentos para visualizar a distribuição.</p>'}</section></div>`;
    elemento('#conteudo').innerHTML = cabecalho('Dashboard','Visão geral do patrimônio da JCA Soluções','<a class="btn btn-success btn-modern" href="#equipamentos"><i class="fa-solid fa-laptop"></i> Ver equipamentos</a>') + `<div class="row g-3 mb-4">${indicadores.map(([titulo,valor,icone,cor])=>`<div class="col-sm-6 col-xl-2"><div class="card-dashboard"><div class="d-flex justify-content-between align-items-center"><div><div class="label">${titulo}</div><div class="valor-indicador">${valor}</div></div><div class="icon-wrap bg-soft-${cor}"><i class="fa-solid fa-${icone}"></i></div></div></div></div>`).join('')}</div><div class="row g-3 mb-4">${grafico('Equipamentos por setor',dados.porSetor)}${grafico('Equipamentos por localização',dados.porLocalizacao)}${grafico('Equipamentos por status',dados.porStatus)}</div><section class="content-card p-3 mb-4"><h2 class="fw-bold mb-3">Alertas do inventário</h2><div class="row g-2">${[['Sem responsável',totais.semResponsavel],['Em manutenção',totais.emManutencao],['Sem patrimônio',totais.semPatrimonio],['Sem número de série',totais.semSerie],['Pendentes de conferência',dados.pendentes]].map(([titulo,valor])=>`<div class="col"><div class="p-3 bg-soft-yellow rounded"><strong>${valor}</strong><div>${titulo}</div></div></div>`).join('')}</div></section><section class="content-card p-3"><div class="d-flex justify-content-between mb-3"><h2 class="fw-bold">Últimas movimentações</h2><a href="#movimentacoes">Ver todas</a></div>${tabela(colunasMovimentacoes.slice(0,5),dados.recentes)}</section>`;
}
function opcoesSelecao(itens,valor = '',vazio = 'Todos') { return `<option value="">${escapar(vazio)}</option>${itens.map(item => { const codigo = typeof item==='object'?item.id:item; const nome = typeof item==='object'?item.nome:item; return `<option value="${escapar(codigo)}" ${String(codigo)===String(valor)?'selected':''}>${escapar(nome)}</option>`; }).join('')}`; }
async function equipamentos(relatorio = false) {
    const itens = await api('equipamentos');
    elemento('#conteudo').innerHTML = cabecalho(relatorio?'Relatórios':'Equipamentos',relatorio?'Filtre o patrimônio e exporte os resultados.':'Responsáveis, localização e situação dos equipamentos.',relatorio?'<button class="btn btn-success" id="exportar"><i class="fa-solid fa-file-csv"></i> Exportar CSV</button><button class="btn btn-outline-secondary" id="imprimir">Imprimir / PDF</button>':botaoNovo('Novo equipamento')) + `<div class="content-card p-3 mb-3 sem-impressao"><div class="filtros-equipamentos">${[['tipoId','Tipo',referencias.tipos],['setorId','Setor',referencias.setores],['responsavelId','Funcionário',referencias.funcionarios],['localizacao','Localização',referencias.localizacoes],['status','Status',referencias.status],['alerta','Alertas',['Sem responsável','Sem patrimônio','Sem número de série']]].map(([chave,titulo,opcoes])=>`<div><label class="form-label" for="filtro-${chave}">${titulo}</label><select class="form-select" id="filtro-${chave}" data-filtro="${chave}">${opcoesSelecao(opcoes)}</select></div>`).join('')}</div></div><div id="lista"></div>${relatorio?'<div class="content-card p-3 mt-3" id="totalRelatorio"></div>':''}`;
    const filtros = {};
    const lista = listaPaginada('#lista',itens,colunasEquipamentos,async (acao,id)=>{ if(acao==='detalhes') location.hash=`equipamentos/${id}`; else abrirEquipamento(itens.find(item=>item.id===id)); },{filtro:item=>Object.entries(filtros).every(([chave,valor])=>!valor || (chave==='alerta' ? valor==='Sem responsável'?!item.responsavelId:valor==='Sem patrimônio'?!item.numeroPatrimonio:!item.numeroSerie : String(item[chave])===valor))});
    const totalizar = () => { if(relatorio) elemento('#totalRelatorio').textContent = `Equipamentos filtrados: ${lista.filtrados().length} · Valor de aquisição: ${moeda(lista.filtrados().reduce((soma,item)=>soma+item.valorAquisicao,0))}`; };
    document.querySelectorAll('[data-filtro]').forEach(campo=>campo.onchange=()=>{ filtros[campo.dataset.filtro]=campo.value; lista.atualizar(); totalizar(); });
    elemento('#buscaLista').addEventListener('input',totalizar);
    if(relatorio) { elemento('#exportar').onclick=()=>exportarCsv(lista.filtrados()); elemento('#imprimir').onclick=()=>imprimirRelatorio(lista.filtrados()); totalizar(); }
    else elemento('#novo').onclick=()=>abrirEquipamento();
}
function exportarCsv(itens) {
    const colunas = [['numeroPatrimonio','Patrimônio'],['numeroSerie','Número de série'],['tipo','Tipo'],['marca','Marca'],['modelo','Modelo'],['responsavel','Responsável'],['setor','Setor'],['localizacao','Localização'],['status','Status'],['valorAquisicao','Valor de aquisição']];
    const celula = valor => { const texto=String(valor??''); return `"${(/^[=+@\-\t\r]/.test(texto)?"'"+texto:texto).replace(/"/g,'""')}"`; };
    const conteudo = '\uFEFF' + [colunas.map(([,titulo])=>celula(titulo)).join(';'),...itens.map(item=>colunas.map(([chave])=>celula(item[chave])).join(';'))].join('\r\n');
    const endereco=URL.createObjectURL(new Blob([conteudo],{type:'text/csv;charset=utf-8;'})); const link=document.createElement('a'); link.href=endereco; link.download='inventario-jca.csv'; link.click(); setTimeout(()=>URL.revokeObjectURL(endereco),1000);
}
function imprimirRelatorio(itens) {
    const janela=window.open('','_blank'); if(!janela) return avisar('Permita janelas para imprimir o relatório.');
    janela.document.write(`<!DOCTYPE html><html lang="pt-BR"><head><meta charset="utf-8"><title>Relatório de equipamentos · JCA</title><link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.min.css"><style>body{padding:24px;font:11px Arial}td,th{padding:7px;border-bottom:1px solid #ddd}@page{size:landscape}</style></head><body><h1>JCA Soluções · Inventário</h1><p>${data(hoje())} · ${itens.length} equipamentos · ${moeda(itens.reduce((soma,item)=>soma+item.valorAquisicao,0))}</p>${tabela(colunasEquipamentos.slice(1,-1),itens)}</body></html>`); janela.document.close(); janela.onload=()=>janela.print();
}
function campo(chave,titulo,tipo='text',opcoes={}) { return {chave,titulo,tipo,...opcoes}; }
function secao(titulo) { return {secao:titulo}; }
function abrirFormulario(titulo,campos,dados,aoSalvar,tipo='') {
    salvarFormulario=aoSalvar; tipoFormulario=tipo; dadosFormulario={...dados}; fotoSelecionada=null; removerFoto=false;
    elemento('#tituloModal').textContent=titulo; elemento('#erroFormulario').hidden=true;
    elemento('#formularioCadastro').reset();
    elemento('#camposFormulario').innerHTML=campos.map(item=>{
        if(item.secao) return `<div class="col-12"><h3 class="fw-bold border-bottom pb-2 mb-0">${escapar(item.secao)}</h3></div>`;
        const valor=dados[item.chave]??item.padrao??'', identificador=`campo-${item.chave}`;
        const atributos=`id="${identificador}" name="${item.chave}" ${item.obrigatorio?'required':''} ${item.somenteLeitura?'disabled':''} ${item.maximo?`maxlength="${item.maximo}"`:''}`;
        let entrada;
        if(item.tipo==='select') entrada=`<select class="form-select" ${atributos}>${opcoesSelecao(item.opcoes,valor,item.vazio??'Selecione')}</select>`;
        else if(item.tipo==='checkbox') entrada=`<div class="form-check mt-2"><input class="form-check-input" type="checkbox" ${atributos} ${valor?'checked':''}><label class="form-check-label" for="${identificador}">${escapar(item.titulo)}</label></div>`;
        else if(item.tipo==='textarea') entrada=`<textarea class="form-control" rows="3" ${atributos}>${escapar(valor)}</textarea>`;
        else if(item.tipo==='file') entrada=`<input class="form-control" type="file" accept=".jpg,.jpeg,.png,.webp" ${atributos}><div class="form-text">JPG, PNG ou WebP. Até 5 MB.</div><img id="previaFoto" class="previa-foto" alt="Prévia da foto" ${dados.foto?`src="/api/equipamentos/${dados.id}/foto?v=${encodeURIComponent(dados.foto)}"`:'hidden'}><button type="button" class="btn btn-outline-danger mt-2" id="removerFoto" ${dados.foto?'':'hidden'}>Remover foto</button>`;
        else entrada=`<input class="form-control" type="${item.tipo}" value="${escapar(item.tipo==='date'?dataCampo(valor):valor)}" ${atributos} ${item.tipo==='number'?'min="0" step="0.01"':''} ${item.tipo==='password'?'autocomplete="new-password" minlength="8" maxlength="128"':''}>`;
        return `<div class="${item.largura || (item.tipo==='textarea'||item.tipo==='file'?'col-12':'col-md-6')}">${item.tipo==='checkbox'?'':`<label class="form-label" for="${identificador}">${escapar(item.titulo)}${item.obrigatorio?' *':''}</label>`}${entrada}${item.ajuda?`<div class="form-text">${escapar(item.ajuda)}</div>`:''}</div>`;
    }).join('');
    elemento('#formularioCadastro').dataset.campos=JSON.stringify(campos.filter(item=>item.chave).map(item=>({chave:item.chave,tipo:item.tipo})));
    if(tipo==='equipamento') {
        elemento('#campo-arquivo').onchange=evento=>{
            const arquivo=evento.target.files[0]; if(!arquivo) return;
            if(!/\.(jpe?g|png|webp)$/i.test(arquivo.name)||arquivo.size>5*1024*1024) { evento.target.value=''; avisar('Selecione uma foto JPG, PNG ou WebP de até 5 MB.'); return; }
            fotoSelecionada=arquivo; removerFoto=false; const previa=elemento('#previaFoto'); previa.src=URL.createObjectURL(arquivo); previa.onload=()=>URL.revokeObjectURL(previa.src); previa.hidden=false; elemento('#removerFoto').hidden=false;
        };
        elemento('#removerFoto').onclick=()=>{ fotoSelecionada=null; removerFoto=true; elemento('#campo-arquivo').value=''; elemento('#previaFoto').hidden=true; elemento('#removerFoto').hidden=true; };
    }
    modal.show();
}
elemento('#formularioCadastro').onsubmit=async evento=>{
    evento.preventDefault(); const botao=elemento('#salvarCadastro'); botao.disabled=true; elemento('#erroFormulario').hidden=true;
    try {
        const formulario=new FormData(evento.target), dados={...dadosFormulario};
        for(const item of JSON.parse(evento.target.dataset.campos)) {
            if(item.tipo==='file') continue;
            if(item.tipo==='checkbox') dados[item.chave]=formulario.has(item.chave);
            else if(item.tipo==='number') dados[item.chave]=Number(formulario.get(item.chave)||0);
            else if(item.chave.endsWith('Id')) { if(!elemento(`#campo-${item.chave}`).disabled) dados[item.chave]=formulario.get(item.chave)?Number(formulario.get(item.chave)):null; }
            else dados[item.chave]=formulario.get(item.chave)||null;
        }
        if((dados.status==='Baixado'&&dadosFormulario.status!=='Baixado')||(dados.ativo===false&&dadosFormulario.ativo!==false&&dadosFormulario.id)) {
            if(!await confirmar('Confirma a inativação ou baixa deste registro? O histórico será preservado.')) return;
        }
        await salvarFormulario(dados); modal.hide(); await carregarReferencias(); await navegar(); avisar('Registro salvo com sucesso.');
    } catch(erro) { elemento('#erroFormulario').textContent=erro.message; elemento('#erroFormulario').hidden=false; }
    finally { botao.disabled=false; }
};
function abrirEquipamento(equipamento={}) {
    abrirFormulario(equipamento.id?'Editar equipamento':'Novo equipamento',[
        secao('Identificação'),campo('tipoId','Tipo','select',{opcoes:referencias.tipos.filter(item=>item.ativo),obrigatorio:true}),campo('marca','Marca','text',{obrigatorio:true,maximo:100}),campo('modelo','Modelo','text',{obrigatorio:true,maximo:160}),campo('numeroPatrimonio','Número de patrimônio','text',{maximo:100}),campo('numeroSerie','Número de série','text',{maximo:150}),campo('dataAquisicao','Data de aquisição','date'),campo('valorAquisicao','Valor de aquisição (R$)','number',{padrao:0}),
        secao('Situação e responsável'),campo('status','Status','select',{opcoes:referencias.status,obrigatorio:true,padrao:'Em estoque',ajuda:'Envios e retornos de manutenção são registrados na tela Manutenções.'}),campo('localizacao','Localização','select',{opcoes:referencias.localizacoes,obrigatorio:true,padrao:'Estoque'}),campo('responsavelId','Funcionário responsável','select',{opcoes:referencias.funcionarios.filter(item=>item.ativo),vazio:'Sem responsável'}),
        secao('Configuração básica'),campo('processador','Processador','text',{maximo:160}),campo('memoriaRam','Memória RAM','text',{maximo:100}),campo('armazenamento','Armazenamento','text',{maximo:100}),campo('sistemaOperacional','Sistema operacional','text',{maximo:160}),campo('observacoes','Observações','textarea',{maximo:10000}),secao('Foto'),campo('arquivo','Foto do equipamento','file')
    ],equipamento,async dados=>{
        const salvo=await api('equipamentos',{method:'POST',body:dados});
        // Mantém o identificador se o cadastro salvar e o envio da foto precisar ser repetido.
        dadosFormulario.id=salvo.id;
        if(fotoSelecionada) { const arquivo=new FormData(); arquivo.append('arquivo',fotoSelecionada); await api(`equipamentos/${salvo.id}/foto`,{method:'POST',body:arquivo}); }
        else if(removerFoto&&equipamento.foto) await api(`equipamentos/${salvo.id}/foto`,{method:'DELETE'});
    },'equipamento');
}
function abrirMovimentacao(equipamento) {
    abrirFormulario('Movimentar equipamento',[
        campo('tipo','Tipo de movimentação','select',{opcoes:referencias.movimentacoes,obrigatorio:true}),campo('responsavelId','Novo responsável','select',{opcoes:referencias.funcionarios.filter(item=>item.ativo),vazio:'Sem responsável'}),campo('status','Novo status','select',{opcoes:referencias.status.filter(item=>item!=='Em manutenção'),obrigatorio:true}),campo('localizacao','Nova localização','select',{opcoes:referencias.localizacoes.filter(item=>item!=='Manutenção'),obrigatorio:true}),campo('observacao','Observação','textarea',{maximo:10000})
    ],{responsavelId:equipamento.responsavelId,status:equipamento.status,localizacao:equipamento.localizacao,tipo:equipamento.responsavelId?'Transferência':'Entrega'},dados=>api(`equipamentos/${equipamento.id}/movimentacoes`,{method:'POST',body:dados}));
    elemento('#campo-tipo').onchange=evento=>{
        if(['Devolução','Transferência para estoque'].includes(evento.target.value)) { elemento('#campo-responsavelId').value=''; elemento('#campo-status').value='Em estoque'; elemento('#campo-localizacao').value='Estoque'; }
        if(['Entrega','Transferência'].includes(evento.target.value)) { elemento('#campo-status').value='Em uso'; elemento('#campo-localizacao').value='Escritório'; }
    };
}
function quadroDetalhes(titulo,campos) { return `<section class="content-card p-3 mb-3"><h2 class="fw-bold mb-3">${escapar(titulo)}</h2><dl class="row detalhe-campo mb-0">${campos.map(([rotulo,valor])=>`<div class="col-md-6"><dt>${escapar(rotulo)}</dt><dd>${escapar(valor??'—')}</dd></div>`).join('')}</dl></section>`; }
async function detalhesEquipamento(id) {
    const dados=await api(`equipamentos/${id}`), equipamento=dados.equipamento;
    elemento('#conteudo').innerHTML=cabecalho(`${equipamento.marca} ${equipamento.modelo}`,`${equipamento.numeroPatrimonio || 'Sem patrimônio'} · ${equipamento.tipo}`,`<a class="btn btn-outline-secondary" href="#equipamentos">Voltar</a><button class="btn btn-outline-secondary" id="editarEquipamento">Editar</button><button class="btn btn-success" id="movimentar">Movimentar</button>`) + `<div class="row g-3"><div class="col-lg-8">${quadroDetalhes('Identificação',[['Tipo',equipamento.tipo],['Marca',equipamento.marca],['Modelo',equipamento.modelo],['Patrimônio',equipamento.numeroPatrimonio],['Número de série',equipamento.numeroSerie],['Data de aquisição',data(equipamento.dataAquisicao)],['Valor de aquisição',moeda(equipamento.valorAquisicao)],['Status',equipamento.status]])}${quadroDetalhes('Configuração',[['Processador',equipamento.processador],['Memória RAM',equipamento.memoriaRam],['Armazenamento',equipamento.armazenamento],['Sistema operacional',equipamento.sistemaOperacional]])}${quadroDetalhes('Responsável e localização',[['Funcionário',equipamento.responsavel],['Setor',equipamento.setor],['Localização',equipamento.localizacao],['Observações',equipamento.observacoes]])}</div><div class="col-lg-4"><section class="content-card p-3 mb-3"><h2 class="fw-bold mb-3">Foto do equipamento</h2>${foto(equipamento,true)}</section><section class="content-card p-3 mb-3"><h2 class="fw-bold mb-3">Documentos e manutenção</h2><div class="d-grid gap-2"><button class="btn btn-outline-secondary" id="termo" ${equipamento.responsavelId?'':'disabled'}><i class="fa-solid fa-file-pdf"></i> Termo de responsabilidade (PDF)</button><button class="btn btn-outline-secondary" id="novaManutencao"><i class="fa-solid fa-screwdriver-wrench"></i> Registrar manutenção</button></div></section></div></div><section class="content-card p-3 mb-3"><h2 class="fw-bold mb-3">Movimentações</h2>${tabela(colunasMovimentacoes,dados.movimentacoes)}</section><section class="content-card p-3 mb-3"><h2 class="fw-bold mb-3">Manutenções</h2>${tabela(colunasManutencoes.slice(2,-1),dados.manutencoes)}</section><section class="content-card p-3"><h2 class="fw-bold mb-3">Histórico completo</h2><div class="timeline">${dados.historico.map(item=>`<div class="timeline-item"><strong>${escapar(item.descricao)}</strong><div class="text-muted">${dataHora(item.data)} · ${escapar(item.usuario)}</div><details class="mt-2"><summary>Ver dados registrados</summary><div class="row mt-2"><div class="col-md-6"><strong>Antes</strong><pre>${escapar(formatarHistorico(item.dadosAnteriores))}</pre></div><div class="col-md-6"><strong>Depois</strong><pre>${escapar(formatarHistorico(item.dadosNovos))}</pre></div></div></details></div>`).join('')||'<p class="text-muted">Nenhuma alteração registrada.</p>'}</div></section>`;
    elemento('#editarEquipamento').onclick=()=>abrirEquipamento(equipamento); elemento('#movimentar').onclick=()=>abrirMovimentacao(equipamento); elemento('#novaManutencao').onclick=()=>abrirManutencao({equipamentoId:id});
    elemento('#termo').onclick=async()=>{ try { const resposta=await fetch(`/api/equipamentos/${id}/termo`); if(!resposta.ok) { const erro=await resposta.json(); throw new Error(erro.mensagem||'Não foi possível gerar o termo.'); } const endereco=URL.createObjectURL(await resposta.blob()); const link=document.createElement('a'); link.href=endereco; link.download=`termo-equipamento-${id}.pdf`; link.click(); setTimeout(()=>URL.revokeObjectURL(endereco),1000); } catch(erro) { avisar(erro.message); } };
}
function formatarHistorico(valor) { if(!valor)return '—'; try { const objeto=typeof valor==='string'?JSON.parse(valor):valor; return typeof objeto==='object'?Object.entries(objeto).map(([chave,conteudo])=>`${chave.replace(/([a-z])([A-Z])/g,'$1 $2')}: ${conteudo??'—'}`).join('\n'):String(objeto); }catch{return String(valor);} }
async function cadastros(tipo) {
    const itens=await api(`cadastros/${tipo}`);
    elemento('#conteudo').innerHTML=cabecalho(titulos[tipo],'Cadastro, consulta e atualização dos registros.',botaoNovo())+'<div id="lista"></div>';
    const colunas=[{titulo:'Nome',chave:'nome'},...(tipo==='funcionarios'?[{titulo:'Matrícula',chave:'matricula'},{titulo:'Cargo',chave:'cargo'},{titulo:'Setor',chave:'setor'},{titulo:'Trabalho',chave:'tipoTrabalho'}]:[]),...(['usuarios','funcionarios'].includes(tipo)?[{titulo:'E-mail',chave:'email'}]:[]),...(tipo==='usuarios'?[{titulo:'Perfil',exibir:item=>item.administrador?'Administrador':'Operador'}]:[]),{titulo:'Status',chave:'ativo',exibir:item=>badge(item.ativo?'Ativo':'Inativo')},{titulo:'Ações',exibir:item=>acoesRegistro(item,['funcionarios','setores'].includes(tipo))}];
    listaPaginada('#lista',itens,colunas,(acao,id)=>{if(acao==='detalhes')location.hash=`${tipo}/${id}`;else abrirCadastro(tipo,itens.find(item=>item.id===id));});
    elemento('#novo').onclick=()=>abrirCadastro(tipo);
}
function abrirCadastro(tipo,registro={}) {
    const campos=[campo('nome','Nome completo','text',{obrigatorio:true,maximo:160})];
    if(tipo==='funcionarios') campos.push(campo('email','E-mail','email',{maximo:190}),campo('telefone','Telefone','text',{maximo:40}),campo('matricula','Matrícula','text',{maximo:60}),campo('cargo','Cargo','text',{maximo:120}),campo('setorId','Setor','select',{opcoes:referencias.setores.filter(item=>item.ativo),obrigatorio:true}),campo('tipoTrabalho','Tipo de trabalho','select',{opcoes:referencias.tiposTrabalho,obrigatorio:true,padrao:'Presencial'}));
    if(tipo==='usuarios') campos.push(campo('email','E-mail','email',{obrigatorio:true,maximo:190}),campo('senha','Senha','password',{obrigatorio:!registro.id,ajuda:registro.id?'Deixe em branco para manter a senha atual.':'Mínimo de oito caracteres.'}),campo('administrador','Administrador (acesso completo)','checkbox'));
    campos.push(campo('ativo','Ativo','checkbox',{padrao:true}));
    if(tipo!=='usuarios')campos.push(campo('observacoes','Observações','textarea',{maximo:10000}));
    abrirFormulario(`${registro.id?'Editar':'Cadastrar'} · ${titulos[tipo]}`,campos,registro,dados=>api(`cadastros/${tipo}`,{method:'POST',body:dados}));
}
async function detalhesFuncionario(id) {
    const dados=await api(`cadastros/funcionarios/${id}`), funcionario=dados.funcionario;
    elemento('#conteudo').innerHTML=cabecalho(funcionario.nome,`${funcionario.cargo||'Funcionário'} · ${funcionario.setor}`,'<a class="btn btn-outline-secondary" href="#funcionarios">Voltar</a>')+quadroDetalhes('Funcionário',[['E-mail',funcionario.email],['Telefone',funcionario.telefone],['Matrícula',funcionario.matricula],['Tipo de trabalho',funcionario.tipoTrabalho],['Status',funcionario.ativo?'Ativo':'Inativo'],['Observações',funcionario.observacoes]])+`<section class="content-card p-3 mb-3"><h2 class="fw-bold mb-3">Equipamentos atuais</h2>${tabela(colunasEquipamentos.slice(0,-1),dados.equipamentos)}</section><section class="content-card p-3"><h2 class="fw-bold mb-3">Histórico de equipamentos</h2>${tabela(colunasMovimentacoes,dados.historico)}</section>`;
}
async function detalhesSetor(id) {
    const dados=await api(`cadastros/setores/${id}`);
    elemento('#conteudo').innerHTML=cabecalho(dados.setor.nome,'Funcionários e equipamentos vinculados ao setor.','<a class="btn btn-outline-secondary" href="#setores">Voltar</a>')+`<section class="content-card p-3 mb-3"><h2 class="fw-bold mb-3">Funcionários</h2>${tabela([{titulo:'Nome',exibir:item=>`<a href="#funcionarios/${item.id}">${escapar(item.nome)}</a>`},{titulo:'Cargo',chave:'cargo'},{titulo:'E-mail',chave:'email'},{titulo:'Status',exibir:item=>badge(item.ativo?'Ativo':'Inativo')}],dados.funcionarios)}</section><section class="content-card p-3"><h2 class="fw-bold mb-3">Equipamentos</h2>${tabela(colunasEquipamentos.slice(0,-1),dados.equipamentos)}</section>`;
}
async function movimentacoes() {
    const itens=await api('movimentacoes');
    elemento('#conteudo').innerHTML=cabecalho('Movimentações','Histórico de entregas, devoluções, transferências e localizações.','<a href="#equipamentos" class="btn btn-success">Selecionar equipamento para movimentar</a>')+'<div id="lista"></div>';
    listaPaginada('#lista',itens,colunasMovimentacoes,null,{ordenar:'data'});
}
async function manutencoes() {
    const itens=await api('manutencoes');
    elemento('#conteudo').innerHTML=cabecalho('Manutenções','Entradas, soluções, custos e retorno dos equipamentos.',botaoNovo('Nova manutenção'))+'<div id="lista"></div>';
    listaPaginada('#lista',itens,colunasManutencoes,(_,id)=>abrirManutencao(itens.find(item=>item.id===id)));
    elemento('#novo').onclick=()=>abrirManutencao();
}
async function abrirManutencao(registro={}) {
    try {
        const equipamentos=await api('equipamentos');
        abrirFormulario(registro.id?'Atualizar / finalizar manutenção':'Registrar manutenção',[
            campo('equipamentoId','Equipamento','select',{opcoes:equipamentos.map(item=>({id:item.id,nome:`${item.numeroPatrimonio||'Sem patrimônio'} · ${item.marca} ${item.modelo}`})),obrigatorio:true,somenteLeitura:!!registro.id}),campo('tipo','Tipo de manutenção','text',{obrigatorio:true,maximo:100}),campo('dataEntrada','Data de entrada','date',{obrigatorio:true,padrao:hoje()}),campo('dataSaida','Data de saída','date',{ajuda:'Ao informar a saída, o equipamento retorna ao estoque, sem responsável.'}),campo('problema','Problema','textarea',{obrigatorio:true,maximo:10000}),campo('solucao','Solução','textarea',{maximo:10000}),campo('valor','Valor (R$)','number',{padrao:0}),campo('responsavel','Responsável pela manutenção','text',{obrigatorio:true,maximo:160}),campo('observacoes','Observações','textarea',{maximo:10000})
        ],registro,dados=>api('manutencoes',{method:'POST',body:dados}));
    } catch(erro) { avisar(erro.message); }
}
async function inventarios() {
    const itens=await api('inventarios');
    elemento('#conteudo').innerHTML=cabecalho('Inventários físicos','Conferência dos equipamentos ativos em cada período.',botaoNovo('Novo inventário'))+'<div id="lista"></div>';
    listaPaginada('#lista',itens,[{titulo:'Inventário',chave:'nome'},{titulo:'Data',chave:'data',exibir:item=>data(item.data)},{titulo:'Total',chave:'total'},{titulo:'Conferidos',chave:'conferidos'},{titulo:'Pendentes',chave:'pendentes'},{titulo:'Divergências',chave:'divergencias'},{titulo:'Situação',exibir:item=>badge(item.encerrado?'Encerrado':'Em andamento')},{titulo:'Ações',exibir:item=>`<a class="btn btn-outline-secondary" href="#inventarios/${item.id}">Abrir conferência</a>`}]);
    elemento('#novo').onclick=()=>abrirFormulario('Novo inventário',[campo('nome','Nome do inventário','text',{obrigatorio:true,maximo:160,ajuda:'Inclui os equipamentos atuais, exceto inativos e baixados.'})],{nome:`Inventário ${new Date().toLocaleDateString('pt-BR',{month:'long',year:'numeric'})}`},dados=>api('inventarios',{method:'POST',body:dados}));
}
async function detalhesInventario(id) {
    const dados=await api(`inventarios/${id}`), inventario=dados.inventario;
    const totais=['Conferido','Pendente','Divergência'].map(situacao=>`${situacao}: ${dados.itens.filter(item=>item.situacao===situacao).length}`).join(' · ');
    elemento('#conteudo').innerHTML=cabecalho(inventario.nome,`Total: ${dados.itens.length} · ${totais}`,`<a class="btn btn-outline-secondary" href="#inventarios">Voltar</a>${inventario.encerrado?'': '<button class="btn btn-success" id="encerrar">Encerrar inventário</button>'}`)+'<div id="lista"></div>';
    listaPaginada('#lista',dados.itens,[{titulo:'Patrimônio',chave:'numeroPatrimonio'},{titulo:'Equipamento',chave:'modelo'},{titulo:'Série',chave:'numeroSerie'},{titulo:'Responsável',chave:'responsavel'},{titulo:'Localização',chave:'localizacao'},{titulo:'Situação',chave:'situacao',exibir:item=>badge(item.situacao)},{titulo:'Data',chave:'data',exibir:item=>dataHora(item.data)},{titulo:'Usuário',chave:'usuario'},{titulo:'Observação',chave:'observacao'},{titulo:'Ações',exibir:item=>inventario.encerrado?'—':`<button class="btn btn-outline-secondary" data-acao="conferir" data-id="${item.id}">Conferir</button>`}],(_,conferenciaId)=>{
        const item=dados.itens.find(item=>item.id===conferenciaId);
        abrirFormulario(`Conferir · ${item.numeroPatrimonio||item.modelo}`,[campo('situacao','Situação','select',{opcoes:['Conferido','Pendente','Divergência'],obrigatorio:true}),campo('observacao','Observação','textarea',{maximo:10000,ajuda:'Descreva o que foi encontrado em caso de divergência.'})],{...item,situacao:item.situacao==='Pendente'?'Conferido':item.situacao},registro=>api(`inventarios/${id}/conferencias/${item.equipamentoId}`,{method:'POST',body:registro}));
    });
    elemento('#encerrar')?.addEventListener('click',async()=>{ if(!await confirmar('Encerrar este inventário? As conferências ficarão disponíveis apenas para consulta.'))return;try{await api(`inventarios/${id}/encerrar`,{method:'POST'});await navegar();avisar('Inventário encerrado.');}catch(erro){avisar(erro.message);}});
}
(async()=>{try { await atualizarToken(); await mostrarSistema(); }catch(erro){ mostrarEntrada(); if(!erro.message.includes('solicitação')) elemento('#erroEntrada').textContent=erro.message; }})();
