"""Verificações do painel, executadas pelo teste de integração no LocalDB temporário."""
import re
from html import unescape


def verificar_painel(teste, cliente, sql, banco):
    status, _, painel = cliente.abrir("/Painel")
    teste.assertEqual(status, 200)
    teste.assertEqual(re.findall(r'data-valor="(\d+)"', painel), ["0"] * 4)

    sql(banco, """
        DECLARE @prof nvarchar(450)=(SELECT Id FROM AspNetUsers WHERE UserName='4805');
        INSERT INTO AspNetUsers(Id,Nome,UserName,NormalizedUserName,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount)
            VALUES ('outro','Docente teste','9999','9999',0,0,0,1,0);
        DECLARE @minha uniqueidentifier=NEWID(), @outra uniqueidentifier=NEWID(), @ad uniqueidentifier=NEWID();
        INSERT INTO Avaliacoes(Id,ProfessorId,Titulo,CriadaEm) VALUES
            (@minha,@prof,'Prova do docente',SYSDATETIMEOFFSET()), (@outra,'outro','Prova de terceiro',SYSDATETIMEOFFSET());
        INSERT INTO Adaptacoes(Id,AvaliacaoId,Estado,ModeloIa,VersaoPrompt,RevisaoBase,CriadaEm) VALUES
            (@ad,@minha,2,'teste','1','1',SYSDATETIMEOFFSET()), (NEWID(),@minha,2,'teste','1','1',SYSDATETIMEOFFSET()),
            (NEWID(),@minha,1,'teste','1','1',SYSDATETIMEOFFSET()), (NEWID(),@outra,2,'teste','1','1',SYSDATETIMEOFFSET());
        INSERT INTO HistoricoAdaptacoes(Id,AdaptacaoId,Estado,RegistradoEm) VALUES (NEWID(),@ad,2,SYSDATETIMEOFFSET());
    """)
    painel = cliente.abrir("/Painel")[2]
    teste.assertEqual(re.findall(r'data-valor="(\d+)"', painel), ["1", "3", "1", "1"])
    for rota in ("/Painel", "/Painel/Avaliacoes", "/Painel/Historico"):
        status, _, html = cliente.abrir(rota)
        teste.assertEqual(status, 200)
        teste.assertIn("Prova do docente", html)
        teste.assertNotIn("Prova de terceiro", html)
    teste.assertIn("Adaptação aprovada", unescape(cliente.abrir("/Painel/Historico")[2]))
