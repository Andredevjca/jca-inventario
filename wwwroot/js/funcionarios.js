(() => {
    "use strict";
    function iniciar() {
        const cep = document.getElementById("Cep");
        const status = document.getElementById("cep-status");
        if (!cep || !status || cep.dataset.viacep) return;
        cep.dataset.viacep = "true";
        let requisicao, ultimoCep = "", versao = 0;
        const normalizar = () => cep.value.replace(/\D/g, "");
        async function consultar() {
            const numero = normalizar();
            if (numero === ultimoCep) return;
            versao++;
            const atual = versao;
            requisicao?.abort();
            ultimoCep = "";
            if (!/^[0-9]{8}$/.test(numero)) {
                status.textContent = numero ? "Informe um CEP com oito dígitos." : "Digite oito dígitos para consultar o endereço.";
                return;
            }
            ultimoCep = numero;
            cep.value = numero.slice(0, 5) + "-" + numero.slice(5);
            requisicao = new AbortController();
            const controlador = requisicao;
            const timeout = setTimeout(() => controlador.abort(), 8000);
            const campos = { Endereco: "logradouro", Bairro: "bairro", Cidade: "localidade", Uf: "uf", Complemento: "complemento" };
            const anteriores = Object.fromEntries(Object.keys(campos).map(id => [id, document.getElementById(id).value]));
            status.textContent = "Consultando CEP…";
            try {
                const resposta = await fetch("https://viacep.com.br/ws/" + numero + "/json/", { signal: controlador.signal, credentials: "omit" });
                if (!resposta.ok) throw new Error("Falha na consulta");
                const dados = await resposta.json();
                if (atual !== versao || !cep.isConnected || normalizar() !== numero) return;
                if (dados.erro) {
                    ultimoCep = "";
                    status.textContent = "CEP não encontrado. Confira o CEP ou preencha o endereço manualmente.";
                    return;
                }
                for (const [id, propriedade] of Object.entries(campos)) {
                    const campo = document.getElementById(id);
                    if (campo.value === anteriores[id]) campo.value = dados[propriedade] || "";
                }
                status.textContent = "Endereço consultado. Confira os dados e informe o número e o complemento, se necessário.";
            } catch (erro) {
                if (atual !== versao || !cep.isConnected) return;
                ultimoCep = "";
                status.textContent = "Não foi possível consultar o CEP. Preencha o endereço manualmente ou tente novamente.";
            } finally { clearTimeout(timeout); }
        }
        cep.addEventListener("input", consultar);
        cep.addEventListener("blur", consultar);
    }
    document.addEventListener("app:navigated", iniciar);
    iniciar();
})();
