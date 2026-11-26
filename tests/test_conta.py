"""Teste HTTP ponta a ponta com Kestrel, Identity e um banco LocalDB isolado."""
import http.cookiejar
import json
import os
from pathlib import Path
import re
import secrets
import socket
import subprocess
import tempfile
import time
import unittest
from urllib.error import HTTPError, URLError
from urllib.parse import parse_qs, urlencode, urlsplit
from caixa_smtp import CaixaSmtp
from test_painel_fluxo import verificar_painel
from urllib.request import build_opener, HTTPCookieProcessor, ProxyHandler
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[1]
PROJECT = str(ROOT / "src" / "Adapta.Web")
SERVER = r"(localdb)\MSSQLLocalDB"


class Navegador:
    def __init__(self, base):
        self.base = base
        self.http = build_opener(ProxyHandler({}), HTTPCookieProcessor(http.cookiejar.CookieJar()))

    def abrir(self, caminho, dados=None):
        corpo = urlencode(dados).encode() if dados is not None else None
        try:
            resposta = self.http.open(self.base + caminho, data=corpo, timeout=20)
        except HTTPError as erro:
            resposta = erro
        with resposta:
            return resposta.status, urlsplit(resposta.url).path, resposta.read().decode()

    def enviar(self, pagina, destino, dados):
        status, _, html = self.abrir(pagina)
        if status != 200:
            raise AssertionError(f"Formulário indisponível: {pagina} ({status})")
        token = re.search(r'name="__RequestVerificationToken"[^>]+value="([^"]+)"', html)
        if not token:
            raise AssertionError(f"Token ausente em {pagina}")
        return self.abrir(destino, {**dados, "__RequestVerificationToken": token[1]})

    def login(self, senha):
        return self.enviar("/Conta/Entrar", "/Conta/Entrar", {"Matricula": "4805", "Senha": senha})

    def logout(self):
        return self.enviar("/Conta", "/Conta/Sair", {})


class ContaTests(unittest.TestCase):
    def test_fluxo_real(self):
        banco = "AdaptaTeste_" + uuid4().hex
        senha = secrets.token_urlsafe(26) + "aA1!"
        caixa = CaixaSmtp()
        self.addCleanup(caixa.fechar)
        documentos = tempfile.TemporaryDirectory(prefix="AdaptaPortalTeste-")
        self.addCleanup(documentos.cleanup)
        env = {**os.environ, "ASPNETCORE_ENVIRONMENT": "Development",
               "ConnectionStrings__Adapta": f"Server={SERVER};Database={banco};Integrated Security=true;TrustServerCertificate=true",
               "Seed__SenhaAdministrador": senha, "Documentos__Diretorio": documentos.name, "Smtp__Host": "127.0.0.1",
               "Smtp__Porta": str(caixa.server_address[1]), "Smtp__StartTls": "false",
               "Smtp__Remetente": "portal@example.test", "Smtp__Usuario": ""}

        def executar(*args):
            subprocess.run(args, cwd=ROOT, env=env, check=True, stdout=subprocess.DEVNULL)

        def sql(database, consulta):
            executar("sqlcmd", "-S", SERVER, "-E", "-b", "-d", database, "-Q", consulta)

        # A criação explícita garante que apenas um banco criado por este teste será removido.
        sql("master", f"CREATE DATABASE [{banco}]")
        self.addCleanup(sql, "master", f"ALTER DATABASE [{banco}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{banco}]")
        executar("dotnet", "ef", "database", "update", "VerificacaoEmail", "--no-build", "--project", PROJECT)
        app = str(Path(PROJECT) / "bin/Debug/net10.0/Adapta.Web.dll")
        for _ in range(2):
            executar("dotnet", app, "--contentRoot", PROJECT, "--seed-admin")

        # Reproduz uma conta cadastrada antes da retirada do código obrigatório.
        sql(banco, "UPDATE AspNetUsers SET TwoFactorEnabled=1, Email='docente@example.test', NormalizedEmail='DOCENTE@EXAMPLE.TEST', EmailConfirmed=1 WHERE UserName='4805'")
        executar("dotnet", "ef", "database", "update", "--no-build", "--project", PROJECT)

        with socket.socket() as reserva:
            reserva.bind(("127.0.0.1", 0))
            porta = reserva.getsockname()[1]
        base = f"http://127.0.0.1:{porta}"
        processo = subprocess.Popen(["dotnet", app, "--urls", base], cwd=PROJECT, env=env,
                                    stdout=subprocess.DEVNULL)
        self.addCleanup(processo.wait, 20)
        self.addCleanup(processo.terminate)
        cliente = Navegador(base)
        for _ in range(100):
            try:
                if cliente.abrir("/")[0] == 200:
                    break
            except URLError:
                if processo.poll() is not None:
                    self.fail("Kestrel encerrou durante a inicialização")
                time.sleep(.2)
        else:
            self.fail("Kestrel não iniciou")

        self.assertEqual(cliente.abrir("/Conta")[1], "/Conta/Entrar")
        self.assertEqual(cliente.abrir("/Conta/Entrar", {"Matricula": "4805", "Senha": senha})[0], 400)
        self.assertEqual(cliente.login("invalida")[1], "/Conta/Entrar")
        inscrito = "docente@example.test"
        self.assertNotIn('name="Email"', cliente.abrir("/Conta/Entrar")[2])
        self.assertIn("Professor Administrador", cliente.login(senha)[2])
        verificar_painel(self, cliente, sql, banco)
        if os.getenv("ADAPTA_TESTAR_NAVEGADOR") == "1":
            subprocess.run(["node", "tests/portal.browser.cjs"], cwd=ROOT, env=env, check=True,
                           input=json.dumps({"base": base, "senha": senha}), text=True)
        self.assertTrue(caixa.mensagens.empty())
        self.assertEqual(cliente.abrir("/Conta/Administracao")[0], 200)
        self.assertEqual(cliente.abrir("/Conta/Sair", {})[0], 400)
        nova = "Abcdef"
        mudar = "/Conta/AlterarSenha"
        for invalida in ("Abcde", "abcdef", "ABCDEF"):
            self.assertEqual(cliente.enviar(mudar, mudar, {"Atual": senha, "Nova": invalida, "Confirmacao": invalida})[1], mudar)
        cliente.enviar(mudar, mudar, {"Atual": senha, "Nova": nova, "Confirmacao": nova})
        self.assertEqual(cliente.logout()[1], "/Conta/Entrar")
        self.assertEqual(cliente.abrir(mudar)[1], "/Conta/Entrar")
        self.assertEqual(cliente.login(senha)[1], "/Conta/Entrar")
        self.assertIn("Professor Administrador", cliente.login(nova)[2])
        cliente.logout()

        esqueci = "/Conta/EsqueciSenha"
        cliente.enviar(esqueci, esqueci, {"Matricula": "4805"})
        mensagem = caixa.mensagens.get(timeout=5)
        self.assertEqual(str(mensagem["To"]), inscrito)
        link = urlsplit(re.search(r"https?://\S+", mensagem.get_content())[0])
        dados = {k.title(): v[0] for k, v in parse_qs(link.query).items()}
        nova = "Docent"
        dados.update(Nova=nova, Confirmacao=nova)
        redefinir = link.path + "?" + link.query
        self.assertEqual(cliente.enviar(redefinir, link.path, dados)[1], "/Conta/Entrar")
        self.assertEqual(cliente.enviar(redefinir, link.path, dados)[1], link.path)

        # No banco de teste, a mesma conta agora representa um docente sem papel administrativo.
        sql(banco, "DELETE FROM AspNetUserRoles WHERE UserId=(SELECT Id FROM AspNetUsers WHERE UserName='4805')")
        sql(banco, "UPDATE AspNetUsers SET Email=NULL, NormalizedEmail=NULL, EmailConfirmed=0 WHERE UserName='4805'")
        cliente.enviar(esqueci, esqueci, {"Matricula": "4805"})
        self.assertIn("Professor Administrador", cliente.login(nova)[2])
        self.assertTrue(caixa.mensagens.empty())
        self.assertEqual(cliente.abrir("/Conta/Administracao")[0], 403)
        cliente.logout()
        for _ in range(5):
            self.assertEqual(cliente.login("invalida")[1], "/Conta/Entrar")
        self.assertEqual(cliente.login(nova)[1], "/Conta/Entrar")
        sql(banco, "IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE UserName='4805' AND LockoutEnd > SYSDATETIMEOFFSET()) THROW 50001, 'Bloqueio ausente', 1;")


if __name__ == "__main__":
    unittest.main()
