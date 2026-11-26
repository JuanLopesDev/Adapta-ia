# Adapta — passo a passo para o laboratório

## 1. Abrir o PowerShell

Abra o **Windows PowerShell**.

Antes de começar, confirme que o Visual Studio está instalado com a carga de trabalho **ASP.NET e desenvolvimento Web**.

## 2. Ir para a pasta onde o projeto ficará

Digite:

```powershell
cd $env:USERPROFILE\Documents
```

Neste ponto, você estará em uma pasta parecida com:

```text
C:\Users\SEU_USUARIO\Documents
```

## 3. Baixar o projeto do GitHub

Ainda nessa pasta, digite:

```powershell
git clone https://github.com/JuanLopesDev/Adapta-ia.git
```

Depois entre na pasta do projeto:

```powershell
cd Adapta-ia
```

Confira se está no lugar certo:

```powershell
Get-Location
```

O resultado deve terminar com:

```text
\Documents\Adapta-ia
```

> Todos os próximos comandos devem ser executados dentro da pasta `Adapta-ia`, onde está o arquivo `Adapta.sln`.

## 4. Conferir se .NET 10 está instalado

Ainda em `Adapta-ia`, digite:

```powershell
dotnet --list-sdks
```

Deve aparecer uma versão começando com `10.`.

## 5. Conferir o banco LocalDB

Ainda em `Adapta-ia`, digite:

```powershell
sqllocaldb info
```

Deve aparecer `MSSQLLocalDB`.

Se não aparecer, instale **SQL Server Express LocalDB** antes de continuar.

## 6. Baixar dependências do projeto

Ainda em `Adapta-ia`, digite um comando por vez:

```powershell
dotnet restore Adapta.sln
dotnet tool restore
```

## 7. Compilar o projeto

Ainda em `Adapta-ia`, digite:

```powershell
dotnet build Adapta.sln
```

Espere aparecer:

```text
Compilação com êxito.
```

## 8. Criar o banco de dados

Ainda em `Adapta-ia`, digite:

```powershell
dotnet ef database update --project src/Adapta.Web
```

Esse comando cria o banco `AdaptaTcc` no computador do laboratório.

## 9. Criar o usuário administrador

Ainda em `Adapta-ia`, copie e cole o bloco completo abaixo:

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

Digite uma senha com pelo menos seis caracteres, uma letra maiúscula e uma letra minúscula.

O usuário será:

```text
Matrícula: 4805
Nome: Professor Administrador
Senha: a senha que você acabou de criar
```

## 10. Abrir pelo Visual Studio

Ainda em `Adapta-ia`, abra a solução:

```powershell
Start-Process .\Adapta.sln
```

O Visual Studio deve abrir o arquivo `Adapta.sln`.

Se preferir fazer manualmente:

1. Abra o **Visual Studio**.
2. Clique em **Open a project or solution**.
3. Vá até `C:\Users\SEU_USUARIO\Documents\Adapta-ia`.
4. Selecione `Adapta.sln`.
5. Aguarde o carregamento da solução.

Confirme que o projeto inicial é `Adapta.Web`. Se não for, clique com o botão direito em `Adapta.Web` no Solution Explorer e escolha **Set as Startup Project**.

## 11. Rodar o sistema pelo Visual Studio

Na barra superior do Visual Studio, selecione o perfil **http** e clique no botão verde de executar ou pressione `F5`.

Também é possível usar `Ctrl + F5` para executar sem o depurador.

## 12. Rodar pelo terminal integrado, se necessário

Ainda em `Adapta-ia`, digite:

```powershell
dotnet run --project src/Adapta.Web --launch-profile http
```

Espere aparecer algo parecido com:

```text
Now listening on: http://localhost:5236
```

## 13. Abrir no navegador

Abra o navegador e entre em:

```text
http://localhost:5236/Conta/Entrar
```

Faça login com a matrícula `4805` e a senha criada no passo 9.

## 14. Parar o sistema

No PowerShell onde o sistema está rodando, pressione:

```text
Ctrl + C
```

## Se o projeto já estiver baixado

Abra o PowerShell e entre na pasta do projeto. Exemplo:

```powershell
cd "C:\Users\SEU_USUARIO\Documents\Adapta-ia"
```

Depois execute somente os passos 8, 9 e 11 se o banco ainda não existir. Se o banco e o administrador já existirem, execute somente o passo 11.

