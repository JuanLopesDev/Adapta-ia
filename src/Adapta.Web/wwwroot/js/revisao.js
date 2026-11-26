const form = document.querySelector("#revisao");
if (form) {
    const lista = document.querySelector("#questoes");
    const instrucoes = document.querySelector("#instrucoes");
    const campo = (titulo, classe, tag) => {
        const label = document.createElement("label");
        label.textContent = titulo;
        const input = document.createElement(tag);
        input.className = "form-control " + classe;
        label.append(input);
        return label;
    };
    function numerar() {
        [...lista.children].forEach((q, i) => {
            q.querySelector(".posicao").textContent = i + 1;
            q.querySelector('[data-acao="subir"]').disabled = i === 0;
            q.querySelector('[data-acao="descer"]').disabled = i === lista.children.length - 1;
        });
    }
    numerar();
    lista.addEventListener("click", e => {
        const botao = e.target.closest("button[data-acao]");
        if (!botao) return;
        const q = botao.closest("fieldset");
        if (botao.dataset.acao === "remover-alternativa" && confirm("Remover esta alternativa da revisão?"))
            botao.closest(".alternativa").remove();
        if (botao.dataset.acao === "subir" && q.previousElementSibling) q.previousElementSibling.before(q);
        if (botao.dataset.acao === "descer" && q.nextElementSibling) q.nextElementSibling.after(q);
        if (botao.dataset.acao === "alternativa") {
            const div = document.createElement("div");
            div.className = "alternativa";
            div.append(campo("Rótulo", "rotulo", "input"), campo("Alternativa", "texto-alt", "textarea"));
            const remover = document.createElement("button");
            remover.type = "button";
            remover.className = "btn btn-light";
            remover.dataset.acao = "remover-alternativa";
            remover.textContent = "Remover alternativa";
            div.append(remover);
            q.querySelector(".alternativas").append(div);
            div.querySelector("input").focus();
        }
        numerar();
    });
    document.querySelector("#add-questao").onclick = () => {
        lista.append(document.querySelector("#modelo-questao").content.cloneNode(true));
        numerar();
        lista.lastElementChild.querySelector(".numero").focus();
    };
    document.querySelector("#add-instrucao").onclick = () => {
        const label = campo("Instrução", "", "textarea");
        instrucoes.append(label);
        label.lastElementChild.focus();
    };
    form.addEventListener("submit", e => {
        if (e.submitter?.value === "true" && !confirm("Confirmar que a extração foi conferida com o original?")) { e.preventDefault(); return; }
        const questoes = [...lista.children].map(q => ({
            id: q.dataset.id || null, numero: q.querySelector(".numero").value,
            enunciado: q.querySelector(".enunciado").value,
            alternativas: [...q.querySelectorAll(".alternativa")].map(a => ({ rotulo: a.querySelector(".rotulo").value, texto: a.querySelector(".texto-alt").value }))
        }));
        form.elements.edicao.value = JSON.stringify({ instrucoes: [...instrucoes.querySelectorAll("textarea")].map(i => i.value), questoes });
    });
}
