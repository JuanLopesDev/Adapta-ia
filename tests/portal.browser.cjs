// Recebe credenciais efêmeras do banco de teste pela entrada padrão.
const { chromium } = require("playwright");
const assert = require("node:assert/strict");
const fs = require("node:fs");

function avaliacaoPdf() {
    const texto = "BT /F1 12 Tf 50 750 Td 18 TL (1. Escolha.) Tj T* (A. Azul) Tj T* (2. Explique.) Tj ET\n10 10 m 30 30 l S\n";
    const objetos = ["<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
        "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>", "<< /Length " + texto.length + " >>\nstream\n" + texto + "endstream"];
    let arquivo = "%PDF-1.4\n";
    const offsets = [];
    for (const objeto of objetos) {
        offsets.push(arquivo.length);
        arquivo += offsets.length + " 0 obj\n" + objeto + "\nendobj\n";
    }
    const xref = arquivo.length;
    return arquivo + "xref\n0 6\n0000000000 65535 f \n" + offsets.map(n => String(n).padStart(10, "0") + " 00000 n \n").join("") +
        "trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n";
}

async function verificar() {
    const { base, senha } = JSON.parse(fs.readFileSync(0, "utf8"));
    const browser = await chromium.launch({ channel: "msedge", headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1440, height: 950 } });
        const erros = [];
        page.on("pageerror", err => erros.push(err.message));
        await page.goto(base + "/Conta/Entrar");
        await page.getByLabel("Matrícula").fill("4805");
        await page.getByLabel("Senha", { exact: true }).fill(senha);
        await page.getByRole("button", { name: "Entrar", exact: true }).click();
        await page.waitForURL("**/Painel");
        await page.getByRole("button", { name: "Recolher menu" }).click();
        assert.equal(await page.locator("#recolher").getAttribute("aria-expanded"), "false");
        await page.getByRole("button", { name: "Expandir menu" }).click();
        for (const width of [1440, 1024, 768, 360]) {
            await page.setViewportSize({ width, height: 950 });
            for (const label of ["Painel", "Minhas avaliações", "Nova adaptação", "Histórico", "Meu perfil"]) {
                if (width < 992) await page.getByRole("button", { name: "Abrir menu" }).click();
                await page.getByRole("navigation", { name: "Navegação docente" }).getByRole("link", { name: label, exact: true }).click();
                await page.getByRole("heading", { level: 1 }).waitFor();
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, width + "px " + label);
                if (label === "Painel" && width === 1440 && process.env.ADAPTA_CAPTURA) {
                    await page.screenshot({ path: process.env.ADAPTA_CAPTURA, fullPage: true });
                }
            }
        }
        await page.goto(base + "/Painel/NovaAdaptacao");
        await page.getByRole("heading", { name: "Anexar prova" }).waitFor();
        await page.locator("#arquivo").setInputFiles({ name: "remover.pdf", mimeType: "application/pdf", buffer: Buffer.from("teste") });
        await page.getByRole("button", { name: "Remover arquivo" }).click();
        assert.equal(await page.locator("#arquivo").inputValue(), "");
        await page.locator("#soltar").evaluate((el, pdf) => {
            const arquivos = new DataTransfer();
            arquivos.items.add(new File([pdf], "Revisao-UI.pdf", { type: "application/pdf" }));
            el.dispatchEvent(new DragEvent("drop", { bubbles: true, dataTransfer: arquivos }));
        }, avaliacaoPdf());
        assert.match(await page.locator("#selecao").innerText(), /Revisao-UI.pdf/);
        await page.getByRole("button", { name: "Confirmar envio" }).click();
        await page.waitForURL("**/Documentos/Revisar/**");
        await page.getByLabel("Perfil de adaptação", { exact:true }).selectOption("TEA");
        const questoes = page.locator("#questoes fieldset");
        assert.equal(await questoes.count(), 2);
        await questoes.nth(1).getByRole("button", { name: "Mover acima" }).click();
        await questoes.first().getByLabel("Enunciado").fill("Explique com suas palavras.");
        await page.getByRole("button", { name: "Salvar revisão" }).click();
        await page.getByText("Revisão salva.", { exact: true }).waitFor();
        await page.reload();
        assert.equal(await questoes.first().getByLabel("Enunciado").inputValue(), "Explique com suas palavras.");
        assert.equal(await page.getByLabel("Perfil de adaptação", { exact:true }).inputValue(), "TEA");
        page.once("dialog", d => d.accept());
        await page.getByRole("button", { name: "Confirmar e continuar para os apoios" }).click();
        await page.getByRole("heading", { name: "Ajustar os apoios" }).waitFor();
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
        await page.getByLabel("Reservar espaço adicional", { exact: false }).check();
        await page.getByRole("button", { name: "Gerar adaptação" }).click();
        await page.getByRole("heading", { name: "Adaptação gerada" }).waitFor();
        assert.match(await page.locator(".folha").innerText(), /Explique com suas palavras/);
        await page.getByText("Material visual no PDF original", { exact: true }).waitFor();
        assert.doesNotMatch(await page.locator(".folha").innerText(), /Elementos vetoriais|pdf:pagina|Visual de página/);
        await page.getByText("Conferir arquivo original", { exact:true }).click();
        assert.ok(await page.getByRole("link", { name:"Baixar original (PDF)" }).isVisible());
        assert.ok(await page.locator("object.pdf-original").isVisible());
        await page.getByText("Conferir arquivo original", { exact:true }).click();
        assert.equal(await page.locator(".espaco-folha").count(), 2);
        const par = page.locator(".questao-folha").first();
        const antes = par.locator(".lado-original");
        const depois = par.locator(".lado-adaptado");
        assert.match(await antes.innerText(), /2\. Explique\./);
        assert.doesNotMatch(await antes.innerText(), /suas palavras/);
        assert.match(await depois.innerText(), /Explique com suas palavras/);
        assert.ok((await depois.boundingBox()).y > (await antes.boundingBox()).y);
        await page.setViewportSize({ width:1440, height:950 });
        assert.ok((await depois.boundingBox()).x > (await antes.boundingBox()).x);
        await page.setViewportSize({ width:360, height:950 });
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
        await page.getByRole("button", { name: "Aprovar esta versão" }).click();
        await page.getByRole("button", { name: "Imprimir folha" }).waitFor();
        const [arquivo] = await Promise.all([page.waitForEvent("download"), page.getByRole("link", { name: "Baixar PDF adaptado" }).click()]);
        assert.equal(arquivo.suggestedFilename(), "Revisao-UI-adaptada.pdf");
        if (process.env.ADAPTA_CAPTURA_PDF) await arquivo.saveAs(process.env.ADAPTA_CAPTURA_PDF);
        await page.emulateMedia({ media: "print" });
        assert.equal(await page.locator(".ferramentas-folha").isVisible(), false);
        assert.equal(await antes.isVisible(), false);
        assert.equal(await depois.isVisible(), true);
        await page.emulateMedia({ media: "screen" });
        await page.getByRole("button", { name: /Menu de perfil/ }).click();
        await page.getByRole("button", { name: "Sair da conta" }).click();
        await page.getByRole("button", { name: "Cancelar" }).click();
        await page.getByRole("button", { name: /Menu de perfil/ }).click();
        await page.getByRole("button", { name: "Sair da conta" }).click();
        await page.getByRole("button", { name: "Sair", exact: true }).click();
        await page.waitForURL("**/Conta/Entrar");
        assert.deepEqual(erros, []);
    } finally { await browser.close(); }
}
verificar().catch(err => { console.error(err); process.exitCode = 1; });
