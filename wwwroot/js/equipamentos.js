(() => {
  "use strict";

  function iniciar() {
    document.querySelectorAll("[data-vinculo-equipamento]").forEach((formulario) => {
      if (formulario.dataset.vinculoIniciado) return;
      formulario.dataset.vinculoIniciado = "true";

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
  }

  document.addEventListener("app:navigated", iniciar);
  iniciar();
})();
