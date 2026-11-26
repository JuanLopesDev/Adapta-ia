# Adapta

Plataforma web para professores revisarem e adaptarem a apresentação de avaliações escolares em PDF ou DOCX.

O MVP apoia avaliações destinadas a estudantes com **Autismo (TEA)** e **Dislexia**. O professor envia a prova, confere a extração, escolhe os apoios, compara o resultado e aprova a versão antes de baixar o PDF.

> A versão atual executa adaptações locais de apresentação. Ela ainda não utiliza uma API ou modelo externo de inteligência artificial.

## Funcionalidades

- Login de professores com matrícula e senha
- Bloqueio temporário após tentativas excessivas de login
- Recuperação de senha por e-mail
- Gestão de usuários pela conta administrativa
- Upload privado de avaliações PDF e DOCX
- Validação de tipo, tamanho e integridade dos arquivos
- Extração de questões, alternativas, instruções e textos de apoio
- Revisão manual da extração antes da adaptação
- Perfil de adaptação para TEA e Dislexia
- Apoios de apresentação escolhidos pelo professor
  - leitura ampliada e espaçamento
  - comandos mais diretos
  - marcadores nas instruções
  - espaço para resposta
- Comparativo visual entre conteúdo original e versão adaptada
- Justificativa dos apoios aplicados
- Aprovação docente e download da folha adaptada em PDF

## Tecnologias

- C# e .NET 10
- ASP.NET Core MVC
- Entity Framework Core 10
- SQL Server / LocalDB
- ASP.NET Core Identity
- Bootstrap 5, HTML, CSS e JavaScript
- PdfPig para leitura de PDF
- Open XML SDK para leitura de DOCX
- QuestPDF para geração de PDF
- xUnit para testes

## Como funciona

```text
Professor → envia PDF ou DOCX → revisa conteúdo extraído
          → escolhe perfil e apoios → compara a adaptação
          → aprova → baixa o PDF adaptado
```

Os documentos originais são guardados fora de `wwwroot`, em diretório privado. Cada professor acessa apenas suas próprias avaliações e adaptações.

## Executar localmente

### Pré-requisitos

- SDK .NET 10
- SQL Server Express LocalDB
- PowerShell

### Instalação

Na pasta que contém `Adapta.sln`, execute:

```powershell
dotnet restore Adapta.sln
dotnet tool restore
dotnet build Adapta.sln
dotnet ef database update --project src/Adapta.Web
```

### Criar o administrador inicial

A conta inicial usa a matrícula `4805`. Defina a senha por variável de ambiente ou User Secrets.

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

A senha precisa ter pelo menos seis caracteres, uma letra maiúscula e uma minúscula.

### Iniciar a aplicação

```powershell
dotnet run --project src/Adapta.Web --launch-profile http
```

Acesse [http://localhost:5236/Conta/Entrar](http://localhost:5236/Conta/Entrar).

## Configurações importantes

| Variável | Finalidade |
| --- | --- |
| `ConnectionStrings__Adapta` | Conexão com o SQL Server |
| `Seed__SenhaAdministrador` | Senha usada na criação inicial do administrador |
| `Documentos__Diretorio` | Diretório privado para os arquivos originais |
| `App__UrlPublica` | URL usada na recuperação de senha |
| `Smtp__Host`, `Smtp__Porta`, `Smtp__Remetente` | Configuração de envio de e-mails |
| `Smtp__Usuario`, `Smtp__Senha` | Credenciais SMTP quando necessárias |

No ambiente de desenvolvimento, sem um host SMTP configurado, os e-mails de recuperação são gravados em `%LOCALAPPDATA%\Adapta\Emails`.

## Estado da integração com IA

O projeto possui uma base pedagógica em `KnowledgeBase`, estruturada para uso futuro com recuperação de trechos e geração assistida por IA. Ela reúne orientações sobre TEA, Dislexia, Desenho Universal para Aprendizagem e revisão docente.

Ainda não há API, chave, modelo generativo ou RAG integrado à aplicação. Atualmente, a adaptação usa regras locais controladas e mostra ao professor os comandos efetivamente ajustados.

## Limitações atuais

- PDFs escaneados não possuem OCR
- Fórmulas, gráficos, tabelas complexas e colunas podem exigir revisão manual
- Elementos visuais de PDF são sinalizados por página
- A associação automática entre uma imagem e uma questão ainda depende da conferência do professor
- O perfil de aluno existe no modelo de dados, mas não possui tela de gerenciamento

## Testes

```powershell
dotnet test tests/Adapta.Tests
```

## Estrutura da solução

```text
src/Adapta.Web/       Aplicação ASP.NET Core MVC
tests/Adapta.Tests/   Testes xUnit
KnowledgeBase/        Base pedagógica para futura recuperação de conhecimento
tools/                 Utilitários do projeto
```

## Próximos passos

- Integrar uma API de IA com revisão obrigatória do professor
- Implementar recuperação de trechos da base pedagógica
- Adicionar OCR para documentos escaneados
- Melhorar a interpretação de fórmulas, imagens e tabelas complexas
