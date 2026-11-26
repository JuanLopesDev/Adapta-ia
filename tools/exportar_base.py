"""Exporta as seções Markdown com proveniência bibliográfica, sem serviços externos."""
import argparse
import json
import re
import sys
from pathlib import Path

BASE = Path(__file__).resolve().parents[1] / "KnowledgeBase"
META = re.compile(r"<!-- id=([\w-]+) \| categoria=(\w+) \| natureza=(\w+) \| fontes=(F\d+(?:,F\d+)*) -->")
CITACAO = re.compile(r"^\*\*(.+?)\*\*\. \*(.+?)\*\. (.+), (\d{4}|s\. d)\.$", re.M)


def referencias():
    texto = (BASE / "REFERENCES.md").read_text(encoding="utf-8")
    catalogo = {}
    for bloco in re.split(r'(?m)(?=^<a id="F\d+"></a>$)', texto)[1:]:
        codigo = re.match(r'<a id="(F\d+)">', bloco)[1]
        ficha = bloco.split("\n## ", 1)[0]
        citacao = CITACAO.search(ficha)
        classe = re.search(r"(?m)^\*\*Natureza da fonte\*\* · (.+)$", ficha)
        links = dict(re.findall(r"\[([^\]]+)\]\((https://[^)]+)\)", ficha))
        if codigo in catalogo or not citacao or not classe or "Consultar publicação" not in links:
            raise ValueError(f"Ficha bibliográfica inválida ou repetida: {codigo}")
        autoria, titulo, publicacao, ano = citacao.groups()
        escopo = ficha[classe.end():].split("[Consultar publicação]", 1)[0].strip()
        catalogo[codigo] = dict(id=codigo, autoria=autoria, titulo=titulo, publicacao=publicacao,
                                ano=int(ano) if ano.isdigit() else None, natureza=classe[1],
                                url=links["Consultar publicação"], doi=links.get("DOI"),
                                url_alternativa=links.get("Cópia em portal público"), alcance=escopo)
    return catalogo


def trechos():
    catalogo = referencias()
    ids = set()
    for path in sorted(BASE.rglob("*.md")):
        if path.name == "REFERENCES.md":
            continue
        texto = path.read_text(encoding="utf-8")
        cabecalho, *secoes = re.split(r"(?m)^## ", texto)
        titulo = re.search(r"(?m)^# (.+)$", cabecalho)[1]
        campos = dict(re.findall(r"(?m)^(\w+): (.+)$", cabecalho))
        for secao in secoes:
            nome, corpo = secao.split("\n", 1)
            meta = META.search(corpo)
            if not meta:
                raise ValueError(f"Metadados ausentes: {path.name} / {nome}")
            codigo, categoria, natureza, lista = meta.groups()
            fontes = lista.split(",")
            citadas = re.findall(r"REFERENCES\.md#(F\d+)\)", corpo)
            if codigo in ids or fontes != citadas:
                raise ValueError(f"Identificador duplicado ou fontes divergentes: {codigo}")
            ids.add(codigo)
            conteudo = corpo[meta.end():].split("\nFontes de fundamentação ·", 1)[0].strip()
            yield dict(id=codigo, documento=path.relative_to(BASE).as_posix(),
                       titulo_documento=titulo, secao=nome, categoria=categoria, natureza=natureza,
                       versao=campos["versao"].strip("'"), status_editorial=campos["status_editorial"],
                       idioma=campos["idioma"], texto=conteudo, fontes=[catalogo[f] for f in fontes])


def main():
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument("--id", help="Exporta uma seção pelo identificador estável")
    cli.add_argument("--saida", type=Path, help="Destino JSONL em UTF-8 (padrão: saída do terminal)")
    args = cli.parse_args()
    registros = [r for r in trechos() if args.id is None or r["id"] == args.id]
    if not registros:
        cli.error("Nenhum trecho encontrado")
    resultado = "".join(json.dumps(r, ensure_ascii=False) + "\n" for r in registros)
    if args.saida:
        args.saida.write_text(resultado, encoding="utf-8")
    else:
        sys.stdout.buffer.write(resultado.encode("utf-8"))


if __name__ == "__main__":
    main()
