const form = document.querySelector("#upload");
const arquivo = form.elements.arquivo;
const soltar = document.querySelector("#soltar");
const remover = document.querySelector("#remover");
const erro = document.querySelector("#erro-arquivo");
function atualizar() {
    const f = arquivo.files[0];
    const mensagem = !f ? "" : !/\.(pdf|docx)$/i.test(f.name) ? "Selecione um PDF ou DOCX." :
        f.size === 0 || f.size > 10 * 1024 * 1024 ? "Selecione um arquivo não vazio de até 10 MiB." : "";
    arquivo.setCustomValidity(mensagem);
    erro.textContent = mensagem;
    remover.hidden = !f;
    document.querySelector("#selecao").textContent = f ? f.name + " · " + f.size.toLocaleString("pt-BR") + " bytes" : "";
}
arquivo.addEventListener("change", atualizar);
remover.addEventListener("click", () => {
    arquivo.value = "";
    arquivo.dispatchEvent(new Event("change"));
    arquivo.focus();
});
soltar.addEventListener("dragover", e => { e.preventDefault(); soltar.classList.add("arrastando"); });
soltar.addEventListener("dragleave", () => soltar.classList.remove("arrastando"));
soltar.addEventListener("drop", e => {
    e.preventDefault();
    soltar.classList.remove("arrastando");
    if (e.dataTransfer.files.length !== 1) { erro.textContent = "Selecione um único arquivo."; return; }
    arquivo.files = e.dataTransfer.files;
    arquivo.dispatchEvent(new Event("change"));
});
form.addEventListener("submit", () => {
    document.querySelector("#processando").hidden = false;
    form.querySelector('[type="submit"]').disabled = true;
});
window.addEventListener("pageshow", () => {
    document.querySelector("#processando").hidden = true;
    form.querySelector('[type="submit"]').disabled = false;
    atualizar();
});
