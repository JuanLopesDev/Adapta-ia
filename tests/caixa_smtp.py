"""Receptor SMTP de teste restrito ao loopback; não encaminha mensagens."""
from email import policy
from email.parser import BytesParser
from queue import Queue
from socketserver import StreamRequestHandler, ThreadingTCPServer
from threading import Thread


class Receber(StreamRequestHandler):
    def handle(self):
        self.wfile.write(b"220 localhost teste\r\n")
        while linha := self.rfile.readline():
            comando = linha.split()[0].upper()
            if comando == b"DATA":
                self.wfile.write(b"354 Ready\r\n")
                linhas = []
                while linha := self.rfile.readline():
                    if linha == b".\r\n":
                        break
                    linhas.append(linha[1:] if linha.startswith(b"..") else linha)
                self.server.mensagens.put(BytesParser(policy=policy.default).parsebytes(b"".join(linhas)))
            elif comando == b"QUIT":
                self.wfile.write(b"221 Bye\r\n")
                break
            self.wfile.write(b"250 OK\r\n")


class CaixaSmtp(ThreadingTCPServer):
    def __init__(self):
        super().__init__(("127.0.0.1", 0), Receber)
        self.mensagens = Queue()
        Thread(target=self.serve_forever, daemon=True).start()

    def fechar(self):
        self.shutdown()
        self.server_close()
