# Adapta — executar após a instalação

Use este guia quando o projeto já estiver baixado, compilado e com o banco atualizado.

## 1. Abrir o PowerShell na pasta do projeto

```powershell
cd "C:\Users\Juan Lopes\Documents\Adapta-ia"
```

Se o seu usuário do Windows tiver outro nome, ajuste apenas a parte `Juan Lopes`.

## 2. Liberar a porta 5236, se necessário

Se o Visual Studio apresentar o erro `address already in use`, descubra qual processo usa a porta:

```powershell
Get-NetTCPConnection -LocalPort 5236 | Select-Object OwningProcess
```

Copie o número exibido em `OwningProcess` e encerre o processo:

```powershell
Stop-Process -Id NUMERO_DO_PROCESSO -Force
```

Exemplo:

```powershell
Stop-Process -Id 12345 -Force
```

Antes de encerrar o processo, confirme se não há outra execução do sistema em uso.

## 3. Criar o administrador

Faça este passo somente na primeira vez, quando ainda não houver a conta administrativa.

```powershell
$senhaAdministrador = Read-Host "Digite a senha do administrador" -AsSecureString
try {
    $env:Seed__SenhaAdministrador = [PSCredential]::new("4805", $senhaAdministrador).GetNetworkCredential().Password
    dotnet run --project src/Adapta.Web --no-launch-profile -- --seed-admin
} finally {
    Remove-Item Env:Seed__SenhaAdministrador -ErrorAction SilentlyContinue
    $senhaAdministrador.Dispose()
}
```

A senha deve ter pelo menos seis caracteres, uma letra maiúscula e uma minúscula.

```text
Matrícula: 4805
Nome: Professor Administrador
Senha: a senha definida no comando anterior
```

## 4. Abrir a solução no Visual Studio

```powershell
Start-Process .\Adapta.sln
```

No Visual Studio:

1. Confirme que `Adapta.Web` é o projeto inicial.
2. Selecione o perfil `http` na barra superior.
3. Pressione `F5` ou clique no botão verde de executar.

## 5. Abrir e entrar no sistema

Abra o navegador em:

```text
http://localhost:5236/Conta/Entrar
```

Entre com a matrícula `4805` e a senha criada no passo 3.

## Próximas execuções

Abra `Adapta.sln` no Visual Studio e pressione `F5`. O banco e o administrador não precisam ser criados novamente.
