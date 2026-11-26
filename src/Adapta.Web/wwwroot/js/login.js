const botao = document.getElementById("ver-senha");
const campo = document.getElementById("Senha");
botao.hidden = false;
botao.addEventListener("click", () => {
    const mostrar = campo.type === "password";
    campo.type = mostrar ? "text" : "password";
    botao.setAttribute("aria-pressed", String(mostrar));
});
