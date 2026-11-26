# Guia de execução em laboratório — Adapta

## Requisitos

Use uma máquina Windows com:

- SDK .NET 10.0.100 ou compatível com `global.json`
- SQL Server Express LocalDB
- PowerShell
- acesso ao NuGet na primeira restauração
- opcionalmente Python 3 e `sqlcmd` para o teste de integração descrito no projeto

O projeto usa SQL Server. O perfil de desenvolvimento aponta para `(localdb)\\MSSQLLocalDB` e o banco `AdaptaTcc`.

## Preparar uma máquina nova

1. Instale o SDK .NET 10.
2. Instale SQL Server Express LocalDB.
3. Confirme no PowerShell:

```powershell
dotnet --list-sdks
sqllocaldb info
```

4. Clone ou copie o repositório e entre na pasta que contém `Adapta.sln`.
5. Restaure dependências e ferramentas:

```powershell
dotnet restore Adapta.sln
dotnet tool restore
dotnet build Adapta.sln
```

## Banco de dados

Em desenvolvimento, a string padrão é:

```text
Server=(localdb)\MSSQLLocalDB;Database=AdaptaTcc;Trusted_Connection=True;TrustServerCertificate=True
```

Para usar outra instância, defina a variável antes dos comandos:

```powershell
$env:ConnectionStrings__Adapta = "Server=SERVIDOR;Database=AdaptaTcc;Trusted_Connection=True;TrustServerCertificate=True"
```

Ou configure User Secrets para o projeto web:

```powershell
dotnet user-secrets set "ConnectionStrings:Adapta" "Server=SERVIDOR;Database=AdaptaTcc;Trusted_Connection=True;TrustServerCertificate=True" --project src/Adapta.Web
```

Crie ou atualize o banco com as migrations:

```powershell
dotnet ef database update --project src/Adapta.Web
dotnet ef migrations list --project src/Adapta.Web
```

## Criar o administrador inicial

Defina uma senha que tenha no mínimo seis caracteres, uma letra maiúscula e uma minúscula. O procedimento abaixo evita gravar a senha diretamente no código.

```powershell
$senhaAdministrador = Read-Host "Senha inicial do administrador" -AsSecureString
try {
    $env:Seed__SenhaAdministrador = [PSCredential]::new("4805", $senhaAdministrador).GetNetworkCredential().Password
    dotnet run --project src/Adapta.Web --no-launch-profile -- --seed-admin
} finally {
    Remove-Item Env:Seed__SenhaAdministrador -ErrorAction SilentlyContinue
    $senhaAdministrador.Dispose()
}
```

O seed cria a matrícula `4805`, o nome `Professor Administrador` e o papel `Administrador`. Uma execução posterior preserva a conta existente.

## Configurações por ambiente

| Configuração | Uso |
| --- | --- |
| `ConnectionStrings__Adapta` | Conexão SQL Server |
| `Seed__SenhaAdministrador` | Senha usada somente no primeiro seed |
| `Documentos__Diretorio` | Diretório privado de originais. Padrão em `%LOCALAPPDATA%\Adapta\Documentos` |
| `App__UrlPublica` | URL pública usada nos links de recuperação de senha |
| `Smtp__Host` | Host SMTP para e-mail real |
| `Smtp__Porta` | Porta SMTP, padrão 587 |
| `Smtp__Remetente` | Remetente autorizado |
| `Smtp__Usuario` e `Smtp__Senha` | Credenciais SMTP quando necessárias |

O desenvolvimento padrão usa `Email:CaixaLocal=true`. Sem `Smtp:Host`, os e-mails são gravados em `%LOCALAPPDATA%\Adapta\Emails` como arquivos `.eml`.

Não há configuração de API de inteligência artificial na versão atual. Não defina chaves de IA, pois a aplicação não as utiliza.

## Executar a aplicação

Na raiz da solução:

```powershell
dotnet run --project src/Adapta.Web --launch-profile http
```

Abra `http://localhost:5236/Conta/Entrar`.

Para desenvolvimento HTTPS, use o perfil `https` configurado em `launchSettings.json`:

```powershell
dotnet run --project src/Adapta.Web --launch-profile https
```

## Roteiro de teste manual

1. Faça login com matrícula `4805` e a senha definida no seed.
2. Abra **Nova adaptação**.
3. Envie um PDF ou DOCX de até 10 MiB.
4. Revise enunciados, alternativas e instruções.
5. Confirme a revisão.
6. Escolha TEA ou Dislexia.
7. Marque ao menos um apoio de apresentação e gere a adaptação.
8. Confira o comparativo antes/depois e a justificativa.
9. Aprove a versão e teste **Baixar PDF adaptado**.
10. No menu de perfil, teste alteração de senha. Para recuperação, cadastre um e-mail confirmado em uma conta e use a caixa local ou SMTP configurado.

## Testes automatizados

Os testes xUnit do módulo documental usam um banco LocalDB temporário:

```powershell
dotnet test tests/Adapta.Tests
```

O teste de integração adicional requer Python 3, `sqlcmd`, LocalDB e SDK .NET 10:

```powershell
python tests/test_conta.py
```

Para incluir o fluxo de navegador, disponibilize Playwright para Node e Edge e defina `ADAPTA_TESTAR_NAVEGADOR=1` antes de executar o teste de integração.

## Solução de problemas

| Situação | Verificação |
| --- | --- |
| `dotnet` não encontrado ou SDK incompatível | Instale o SDK .NET 10 e confirme `dotnet --list-sdks` |
| Falha ao aplicar migration | Verifique LocalDB com `sqllocaldb info` e a variável `ConnectionStrings__Adapta` |
| Erro ao criar administrador | Confirme `Seed__SenhaAdministrador` e os requisitos de senha |
| Login bloqueado | Após cinco falhas, aguarde cinco minutos ou ajuste a conta diretamente para o ambiente de testes |
| PDF ou DOCX recusado | Confirme extensão, integridade e limite de 10 MiB |
| Documento não é exibido | Verifique as permissões do diretório privado definido em `Documentos__Diretorio` |
| Recuperação de senha não chega | Em desenvolvimento, abra `%LOCALAPPDATA%\Adapta\Emails`. Em SMTP, confira host, remetente, porta e credenciais |
| Link de recuperação inválido | O token expira em 20 minutos. Solicite outro |

## Encerramento

Interrompa a aplicação no terminal com `Ctrl+C`. Mantenha o banco SQL Server e o diretório privado de documentos nos backups do laboratório.

