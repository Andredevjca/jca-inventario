const assert = require('assert');
const fs = require('fs');
const vm = require('vm');
const codigo = fs.readFileSync('wwwroot/js/funcionarios.js', 'utf8');
function ambiente(fetch) {
    const campos = {};
    for (const id of ['Cep','Endereco','Bairro','Cidade','Uf','Complemento','Numero','cep-status']) {
        campos[id] = { value: '', dataset: {}, isConnected: true, eventos: {}, addEventListener(evento, handler) { this.eventos[evento] = handler; } };
    }
    const eventos = {};
    vm.runInNewContext(codigo, { document: { getElementById: id => campos[id], addEventListener: (e,h) => eventos[e]=h }, fetch, AbortController: class { constructor() { this.signal={}; } abort() {} }, setTimeout, clearTimeout });
    return { campos, eventos, digitar: async valor => { campos.Cep.value=valor; await campos.Cep.eventos.input(); } };
}
(async () => {
    let chamadas=0;
    const ok = ambiente(async url => { chamadas++; assert(url.includes('/01001000/')); return { ok:true, json:async()=>({logradouro:'Praça da Sé',bairro:'Sé',localidade:'São Paulo',uf:'SP',complemento:'lado ímpar'}) }; });
    await ok.digitar('123'); assert.equal(chamadas,0);
    ok.campos.Numero.value='123A'; await ok.digitar('01001000');
    assert.equal(ok.campos.Cidade.value,'São Paulo'); assert.equal(ok.campos.Uf.value,'SP'); assert.equal(ok.campos.Complemento.value,'lado ímpar'); assert.equal(ok.campos.Numero.value,'123A');
    await ok.campos.Cep.eventos.blur(); assert.equal(chamadas,1);
    const desconhecido=ambiente(async()=>({ok:true,json:async()=>({erro:true})}));
    await desconhecido.digitar('99999999'); assert(desconhecido.campos['cep-status'].textContent.includes('não encontrado'));
    const falha=ambiente(async()=>{throw Error('offline')}); await falha.digitar('01001000'); assert(falha.campos['cep-status'].textContent.includes('manualmente'));
    const respostas=[]; const concorrente=ambiente(()=>new Promise(resolve=>respostas.push(resolve)));
    const primeira=concorrente.digitar('01001000'); const segunda=concorrente.digitar('20040002');
    respostas[1]({ok:true,json:async()=>({localidade:'Rio de Janeiro',uf:'RJ'})}); await segunda;
    respostas[0]({ok:true,json:async()=>({localidade:'São Paulo',uf:'SP'})}); await primeira;
    assert.equal(concorrente.campos.Cidade.value,'Rio de Janeiro');
    const pendentes=[]; const manual=ambiente(()=>new Promise(resolve=>pendentes.push(resolve)));
    const pendente=manual.digitar('01001000'); manual.campos.Endereco.value='Editado manualmente';
    pendentes[0]({ok:true,json:async()=>({logradouro:'Praça da Sé'})}); await pendente;
    assert.equal(manual.campos.Endereco.value,'Editado manualmente');
    manual.campos.Cep.dataset={}; manual.eventos['app:navigated'](); assert.equal(manual.campos.Cep.dataset.viacep,'true');
    console.log('OK: CEP válido, incompleto, inexistente, indisponibilidade, respostas fora de ordem, edição manual e navegação.');
})().catch(e=>{console.error(e);process.exitCode=1});
