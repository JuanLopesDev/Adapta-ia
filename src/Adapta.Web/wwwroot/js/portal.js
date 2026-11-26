const botaoMenu = document.querySelector("#recolher");
function exibirMenu(recolhido) {
    document.body.classList.toggle("compacto", recolhido);
    botaoMenu.setAttribute("aria-expanded", String(!recolhido));
    botaoMenu.setAttribute("aria-label", recolhido ? "Expandir menu" : "Recolher menu");
}
try { exibirMenu(localStorage.getItem("adapta.menu") === "compacto"); } catch {}
botaoMenu.addEventListener("click", () => {
    const compacto = !document.body.classList.contains("compacto");
    exibirMenu(compacto);
    try { localStorage.setItem("adapta.menu", compacto ? "compacto" : "aberto"); } catch {}
});
const carregamento = document.createElement("progress");
carregamento.className = "progresso-pagina";
carregamento.setAttribute("aria-label", "Carregando página");
carregamento.hidden = true;
document.body.append(carregamento);
let espera;
function indicarCarregamento() {
    espera = setTimeout(() => { carregamento.hidden = false; }, 180);
}
document.addEventListener("click", event => {
    const link = event.target.closest("a[href]");
    if (link && !event.defaultPrevented && !event.ctrlKey && !event.metaKey && !event.shiftKey &&
        !event.altKey && event.button === 0 && link.origin === location.origin &&
        !link.hash && !link.target) indicarCarregamento();
});
document.addEventListener("submit", event => { if (!event.defaultPrevented) indicarCarregamento(); });
window.addEventListener("pageshow", () => { clearTimeout(espera); carregamento.hidden = true; });
