# Documentação técnica — Adapta

## Visão geral

O Adapta é uma aplicação web para professores enviarem avaliações em PDF ou DOCX, revisarem a extração estruturada do conteúdo e produzirem uma versão de apresentação adaptada. A versão implementada trabalha com os perfis Autismo (TEA) e Dislexia. A aprovação do professor é obrigatória antes do download do PDF adaptado.

O sistema não é um gerenciador escolar. Ele não implementa disciplinas, turmas, notas ou séries.

## Arquitetura e funcionamento

A solução usa ASP.NET Core MVC e está organizada em camadas de Controllers, Services, Models, Data, DTOs, ViewModels e Views Razor.

```text
Professor autenticado
        ↓
Controller MVC → Service → DbContext / Identity / armazenamento privado
        ↓                         ↓
ViewModel e View Razor       SQL Server / arquivos PDF ou DOCX
```

- **Controllers** recebem requisições HTTP, aplicam autorização e retornam Views ou arquivos.
- **Services** concentram regras de conta, processamento documental, geração de adaptações, PDF e consultas do painel.
- **Models** representam entidades persistidas.
- **DTOs** representam a estrutura extraída do documento, como questões, alternativas, textos de apoio e avisos.
- **ViewModels** recebem dados dos formulários e apresentam telas.
- **Data** contém o `AdaptaDbContext`, seed do administrador e migrations do EF Core.

O `Program.cs` configura DI, Identity, Entity Framework Core, rate limiting, autorização global, antiforgery e rotas MVC. A política global exige usuário autenticado. As páginas de login e recuperação de senha são exceções explícitas.

## Tecnologias e bibliotecas

| Tecnologia | Uso no projeto |
| --- | --- |
| C# e .NET 10 | Plataforma e linguagem da aplicação |
| ASP.NET Core MVC | Controllers, Views Razor, rotas e filtros |
| Entity Framework Core 10 | Persistência, Fluent API e migrations |
| SQL Server / LocalDB | Banco de dados relacional |
| ASP.NET Core Identity | Contas, hashes de senha, papéis, lockout e tokens |
| Bootstrap 5, CSS e JavaScript | Interface responsiva e interações de upload e revisão |
| PdfPig 0.1.16 | Leitura de texto e indicadores visuais em PDFs |
| DocumentFormat.OpenXml 3.5.1 | Leitura de documentos DOCX |
| QuestPDF 2026.5.0 | Geração do PDF adaptado após aprovação |
| xUnit | Testes automatizados do módulo documental e autorização |

## Autenticação e autorização

O login utiliza matrícula e senha. A matrícula é armazenada como `UserName` no Identity. A política de senha exige pelo menos seis caracteres, uma letra maiúscula e uma minúscula. Dígitos e símbolos são opcionais.

Após cinco tentativas de login sem sucesso, a conta é bloqueada por cinco minutos. Há também limitação de 15 requisições por cinco minutos para as rotas de conta por IP e caminho.

O administrador é criado por seed com matrícula `4805`, nome `Professor Administrador` e papel `Administrador`. A senha não existe no código. Ela deve ser fornecida por `Seed__SenhaAdministrador` ou pelo User Secret `Seed:SenhaAdministrador`.

Somente a conta com papel Administrador e a matrícula administrativa tem acesso à gestão de usuários. Cada operação de documento e adaptação consulta a propriedade pelo identificador do professor autenticado.

## Processamento de documentos

1. O professor envia um PDF ou DOCX de até 10 MiB.
2. O nome passa por validação contra caracteres de controle e tentativas de path traversal.
3. O arquivo recebe uma chave GUID interna e é gravado fora de `wwwroot`.
4. Um processo .NET filho valida e extrai o documento. O processo tem prazo de 45 segundos e limite de heap configurado.
5. O serviço grava metadados no SQL Server e armazena a extração em JSON.
6. O professor revisa numeração, enunciados, alternativas e instruções.
7. A confirmação libera a avaliação para a geração de adaptação.

O PDF é lido com PdfPig. O DOCX é lido com Open XML SDK. O estruturador usa heurísticas para reconhecer números de questão, alternativas, instruções, textos de apoio, tabelas e espaços de resposta. Elementos visuais de PDF são registrados por página e exibidos como aviso único na interface, pois a associação precisa com uma questão pode exigir revisão humana.

Estados do documento: `Pendente`, `Processando`, `Concluido`, `ConcluidoComAvisos` e `Falhou`. A revisão usa `AguardandoRevisao` ou `Revisado`. Uma falha de extração não exclui o original.

## Adaptação implementada

O método em produção no código é identificado como `Apresentação local v1`. Ele não chama serviços externos e não envia o conteúdo do documento a terceiros.

Os apoios disponíveis são:

- leitura ampliada e maior espaçamento
- comandos mais diretos em expressões conhecidas no início de instruções ou enunciados
- marcadores de acompanhamento nas instruções
- espaço adicional para resposta

O professor escolhe TEA ou Dislexia. Para TEA, o nível de suporte de 1 a 3 é opcional. O nível não altera automaticamente a dificuldade nem aciona regras fixas. A adaptação preserva uma cópia da extração revisada, registra os comandos efetivamente modificados, apresenta comparação antes/depois e exibe a justificativa na área docente.

Depois de aprovada, a folha do estudante pode ser baixada como PDF. A justificativa e o perfil não são incluídos no PDF do estudante.

## Integração com inteligência artificial

Não há integração com API de IA implementada nesta versão. Não existe chave, endpoint, modelo remoto ou SDK de IA configurado no projeto.

O projeto está preparado para uma integração futura. A entidade `AdaptacaoAvaliacao` mantém `ModeloIa`, `VersaoPrompt`, `RevisaoBase` e `Resultado`. A estrutura `ExtractedAssessment` mantém questões, alternativas, apoios, blocos e avisos em formato serializável. A base em Markdown localizada em `KnowledgeBase` possui trechos identificáveis e um exportador JSONL para recuperação futura.

Uma futura API deverá receber apenas a estrutura revisada e os apoios aprovados pelo professor. A resposta deverá ser persistida como uma nova versão, sujeita à revisão e aprovação docente. A escolha do modelo e o contrato da API ainda não foram definidos no código.

## Banco de dados

O `AdaptaDbContext` herda de `IdentityDbContext<Professor>`. Além das tabelas do Identity, o sistema usa:

| Entidade | Finalidade e relação |
| --- | --- |
| `Professor` | Usuário do Identity com o nome exibido |
| `Avaliacao` | Pertence a um professor e representa a avaliação enviada |
| `Documento` | Relação um-para-um com avaliação. Guarda chave privada, formato, tamanho, estados e JSONs de extração e revisão |
| `AdaptacaoAvaliacao` | Pertence a uma avaliação e guarda versão, estado e resultado JSON |
| `HistoricoAdaptacao` | Registra mudanças de estado de uma adaptação |
| `Aluno` | Estrutura preparada para apelido por professor e perfil de adaptação |
| `PerfilAdaptacao` | Catálogo de perfis reutilizável |

Índices incluem professor e data da avaliação, chave única do arquivo, apelido único por professor, perfil único e histórico por adaptação/data. Há constraints para formato do documento e estados de adaptação e histórico. `Documento.Versao` é um `rowversion` usado para detectar revisão concorrente.

As migrations existentes são `FundacaoDocente`, `VerificacaoEmail`, `LoginPorMatricula` e `ProcessamentoDocumentos`.

## Fluxo de comunicação

```text
Browser → DocumentosController → DocumentoService → DocumentStorage
                                      ↓
                               LeitorDocumento
                                      ↓
                            PdfProcessor / DocxProcessor
                                      ↓
                                  SQL Server

Browser → PainelController → AdaptacaoService → SQL Server
                                      ↓
                              FolhaPdf → PDF para download
```

O acesso ao original é mediado por `DocumentosController`. O arquivo não é servido estaticamente. A rota verifica propriedade, define `nosniff` e uma CSP sandbox antes da resposta.

## Limitações técnicas atuais

- Não há OCR para PDFs escaneados.
- Fórmulas, gráficos, colunas, tabelas complexas e objetos incorporados podem exigir revisão no documento original.
- A associação de itens visuais de PDF é por página, não por questão.
- A adaptação atual é baseada em regras locais de apresentação e substituições conhecidas de comandos.
- Não há API de IA, modelo generativo, vetorização ou mecanismo RAG em execução.
- O envio de e-mail real depende de SMTP externo configurado.
- O perfil `Aluno` existe no modelo, mas não há tela de gerenciamento de alunos.

