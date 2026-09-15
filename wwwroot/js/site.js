(() => {
  const corpo = document.body;
  const inicializarPagina = () => {
  document.querySelectorAll("[data-vinculo-equipamento]").forEach((formulario) => {
    const responsavel = formulario.querySelector('[name="ResponsavelId"]');
    const status = formulario.querySelector('[name="Status"]');
    const localizacao = formulario.querySelector('[name="Localizacao"]');
    responsavel?.addEventListener("change", () => {
      if (!responsavel.value || !status || !localizacao) return;
      if (status.value !== "Em estoque" && status.value !== "Disponível") return;
      status.value = localizacao.value === "Home Office" ? "Home Office" : "Em uso";
      if (localizacao.value === "Estoque") localizacao.value = "Escritório";
    });
  });
  };
  inicializarPagina();
  document.addEventListener("app:navigated", inicializarPagina);
  const botaoSidebar = document.getElementById("botaoSidebar");
  const backdrop = document.getElementById("sidebarBackdrop");
  const telaPequena = () => window.matchMedia("(max-width: 991.98px)").matches;

  if (!telaPequena() && localStorage.getItem("sidebarRecolhida") === "1") {
    corpo.classList.add("sidebar-collapsed");
  }

  const alternarSidebar = () => {
    if (telaPequena()) {
      corpo.classList.toggle("sidebar-open");
      return;
    }
    corpo.classList.toggle("sidebar-collapsed");
    localStorage.setItem("sidebarRecolhida", corpo.classList.contains("sidebar-collapsed") ? "1" : "0");
  };

  botaoSidebar?.addEventListener("click", alternarSidebar);
  backdrop?.addEventListener("click", () => corpo.classList.remove("sidebar-open"));
  window.addEventListener("resize", () => {
    if (telaPequena()) corpo.classList.remove("sidebar-collapsed");
    else corpo.classList.remove("sidebar-open");
  });

  const modalExclusao = document.getElementById("modalExclusao");
  if (modalExclusao) {
    modalExclusao.addEventListener("show.bs.modal", (evento) => {
      const botao = evento.relatedTarget;
      const form = modalExclusao.querySelector("form");
      const mensagem = modalExclusao.querySelector("[data-mensagem-exclusao]");
      if (botao && form) {
        form.action = botao.getAttribute("data-url") || form.action;
        if (mensagem) mensagem.textContent = botao.getAttribute("data-mensagem") || "Tem certeza que deseja continuar?";
      }
    });
  }
})();
