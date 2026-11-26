# Roteiro de apresentação — Adapta

## Abertura

“O Adapta é uma plataforma web para apoiar professores na preparação de avaliações mais acessíveis. O foco do MVP é adaptar a apresentação de avaliações para estudantes com TEA e Dislexia, preservando o objetivo pedagógico e mantendo o professor como responsável pela aprovação.”

## Problema abordado

Avaliações digitais ou impressas podem apresentar barreiras de leitura, organização visual, instruções extensas e espaço insuficiente de resposta. Preparar versões acessíveis manualmente demanda tempo e exige conferência do material original. A aplicação organiza esse fluxo em um único ambiente.

## Funcionalidades principais

- Login por matrícula e senha para professores.
- Administração de usuários pela conta administrativa.
- Upload privado de PDF e DOCX.
- Extração de questões, alternativas, instruções e textos de apoio.
- Revisão manual da extração antes da adaptação.
- Escolha entre TEA e Dislexia.
- Apoios de apresentação configurados pelo professor.
- Comparativo visual entre original extraído e versão adaptada.
- Justificativa dos apoios aplicados.
- Aprovação docente e download em PDF.

## Tecnologias e funções

| Tecnologia | Função na demonstração |
| --- | --- |
| ASP.NET Core MVC | Organiza telas, rotas e requisições |
| C# e .NET 10 | Implementam regras de negócio e serviços |
| Entity Framework Core 10 | Mapeia entidades e aplica migrations |
| SQL Server | Armazena contas, documentos, revisões e adaptações |
| ASP.NET Core Identity | Protege login, senha, papéis e bloqueio por tentativas |
| PdfPig e Open XML SDK | Extraem conteúdo de PDF e DOCX |
| QuestPDF | Produz o PDF adaptado aprovado |
| Bootstrap 5, CSS e JavaScript | Formam a interface responsiva |

## Arquitetura explicada em um minuto

O navegador envia a requisição ao Controller MVC. O Controller delega a regra para um Service. O Service acessa o banco pelo EF Core ou o armazenamento privado de documentos. A View Razor recebe um ViewModel e renderiza a resposta.

```text
View Razor → Controller → Service → EF Core / SQL Server
                                  → armazenamento privado
                                  → processador PDF ou DOCX
```

Essa separação reduz acoplamento e permite testar processamento, persistência e autorização sem depender da interface.

## Banco de dados

O Identity mantém professores, papéis, hashes de senha, tokens e lockout. A aplicação mantém avaliações, documentos, adaptações e histórico. Cada avaliação pertence a um professor. Cada documento está ligado a uma avaliação e é armazenado fora de `wwwroot`. A extração e a revisão ficam em JSON porque questões, alternativas, blocos e avisos formam um agregado com ordem própria.

## Inteligência artificial

É importante apresentar o estado real do projeto. A versão demonstrada ainda não chama uma API de IA e não utiliza modelo remoto. A adaptação atual é local e baseada em apoios de apresentação selecionados pelo professor, com substituições controladas de comandos conhecidos.

A preparação para IA já existe em três pontos:

1. A extração é estruturada em questões, alternativas e avisos, não apenas em texto corrido.
2. A entidade de adaptação registra modelo, versão do prompt, revisão da base e resultado.
3. A base pedagógica Markdown foi estruturada para exportação JSONL e futura recuperação de trechos.

Em uma evolução, uma API receberia a estrutura revisada, os apoios e os trechos recuperados da base. A resposta continuaria sujeita à revisão e aprovação do professor.

## Demonstração prática

1. Abra a tela de login e explique que o acesso usa matrícula, senha e proteção contra tentativas excessivas.
2. Entre com uma conta de professor.
3. Mostre o painel com estados vazios ou dados existentes.
4. Abra **Nova adaptação** e envie um PDF ou DOCX.
5. Mostre que o documento original é privado e que a tela de revisão exibe questões e avisos de extração.
6. Altere um enunciado ou alternativa e confirme a revisão.
7. No passo de perfil, selecione TEA ou Dislexia. Para TEA, mostre que o nível de suporte é opcional.
8. Marque leitura ampliada, comandos mais diretos ou espaço de resposta.
9. Gere a adaptação e apresente o comparativo antes/depois.
10. Mostre a seção que justifica as alterações. Explique que ela só relata apoios efetivamente aplicados.
11. Aprove a versão e baixe o PDF. Explique que o PDF é a folha do estudante.

## Perguntas técnicas frequentes

### “O sistema já usa ChatGPT ou outra IA?”

Ainda não. A versão atual executa adaptações locais de apresentação. A modelagem e a base documental foram preparadas para uma integração futura, que precisará definir provedor, modelo, custos, privacidade e avaliação da qualidade.

### “Como o sistema garante que o professor veja somente seus documentos?”

Cada consulta de documento e adaptação filtra o registro pelo identificador do professor autenticado. O arquivo original fica em pasta privada e é entregue somente por rota autorizada.

### “Por que o arquivo não fica em wwwroot?”

Porque `wwwroot` pode ser servido publicamente pelo servidor web. O armazenamento privado impede acesso direto por URL e permite validar a autorização antes de abrir o arquivo.

### “O sistema altera o objetivo da questão?”

O fluxo busca preservar a versão revisada pelo professor. A implementação atual atua na apresentação, no espaçamento, no espaço de resposta, nos marcadores e em alguns comandos equivalentes conhecidos. O professor aprova antes de disponibilizar o PDF.

### “Como são tratados PDFs com imagens ou gráficos?”

O sistema registra avisos por página. O professor confere o original e, quando necessário, entrega o material visual junto da versão adaptada. A associação automática entre visual e questão ainda não é garantida.

### “Por que usar JSON para a extração?”

Questões, alternativas, apoios, blocos e avisos precisam preservar ordem e composição. O JSON armazena esse agregado e a versão de revisão separadamente, enquanto dados de propriedade, estado e auditoria permanecem relacionais.

## Limitações e evolução

- Implementar OCR para PDFs digitalizados.
- Melhorar a leitura de fórmulas, gráficos, colunas e tabelas complexas.
- Criar integração com API de IA com proteção de dados, logs e avaliação docente.
- Implementar recuperação semântica da base documental.
- Acrescentar métricas de qualidade e revisão especializada das adaptações.
- Criar recursos para outros perfis de acessibilidade após estudo e validação.

## Encerramento

“O principal resultado do MVP é um fluxo controlado: o professor envia, revisa, adapta, compara, aprova e gera a folha. A aplicação organiza o trabalho de acessibilidade sem transferir a decisão pedagógica para o sistema.”

