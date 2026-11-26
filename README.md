# Adapta

Sistema Inteligente de Adaptação de Avaliações com Inteligência Artificial. A versão atual oferece acesso docente por matrícula, envio privado de avaliações PDF/DOCX, extração estruturada e revisão com persistência em SQL Server. O professor pode gerar, revisar, aprovar, reabrir e imprimir ou baixar em PDF versões com apoios de apresentação para estudantes com TEA ou dislexia.

## Abrir a aplicação neste computador

```powershell
cd "C:\Users\Juan Lopes\Documents\ChatGPT\TCC"
dotnet run --project src/Adapta.Web --launch-profile http
```

Abra a [tela de acesso](http://localhost:5236/Conta/Entrar). Use a matrícula `4805` e a senha inicial entregue ao responsável pelo projeto. Em **Minha conta → Alterar senha**, escolha sua senha pessoal. Se a aplicação já estiver rodando, basta abrir o endereço. Para encerrar o servidor no terminal, pressione Ctrl+C.

## Preparar uma nova instalação

Pré-requisitos de desenvolvimento no Windows: SDK .NET 10, SQL Server Express LocalDB e acesso ao NuGet para a primeira restauração. O banco padrão é `AdaptaTcc` na instância `(localdb)\MSSQLLocalDB`. Para outra instância, configure `ConnectionStrings__Adapta` ou o User Secret `ConnectionStrings:Adapta`. O perfil HTTP é destinado ao desenvolvimento local. Em produção, configure conexão e HTTPS no ambiente.

Execute na pasta da solução:

```powershell
dotnet restore Adapta.sln
dotnet tool restore
dotnet build Adapta.sln
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project src/Adapta.Web
dotnet ef migrations list --project src/Adapta.Web
```

A migração é explícita. Para preparar o administrador em um banco novo, informe uma senha com pelo menos seis caracteres, uma letra maiúscula e uma minúscula. Números e símbolos são opcionais. O exemplo lê a senha sem gravá-la no histórico do terminal.

```powershell
$segredo = Read-Host "Senha inicial do administrador" -AsSecureString
try {
    $env:Seed__SenhaAdministrador = [PSCredential]::new("4805", $segredo).GetNetworkCredential().Password
    dotnet run --project src/Adapta.Web --no-launch-profile -- --seed-admin
} finally {
    Remove-Item Env:Seed__SenhaAdministrador -ErrorAction SilentlyContinue
    $segredo.Dispose()
}
```

O seed também aceita a chave `Seed:SenhaAdministrador` em User Secrets no ambiente Development. Ele cria o professor e o vínculo ao papel Administrador em uma transação. Executá-lo novamente preserva a senha e a conta existentes.

## Recuperação de senha por e-mail

Para entrar, use somente matrícula e senha. O e-mail é opcional. O link **Esqueci minha senha** envia ao endereço já confirmado da conta um link de uso único válido por 20 minutos. Sem endereço confirmado, a recuperação precisa de atendimento pela instituição.

No perfil local de desenvolvimento, a configuração `Email:CaixaLocal` grava cada mensagem em um arquivo .eml em `%LOCALAPPDATA%\Adapta\Emails` quando o host SMTP está vazio. Cole esse caminho no Explorador de Arquivos e abra a mensagem mais recente. Esse modo testa o fluxo no computador sem enviar mensagens pela internet. Use banco e conta de desenvolvimento separados dos de produção.

Para entregar mensagens ao e-mail informado, configure o serviço de envio da instituição. As chaves abaixo aceitam variáveis de ambiente ou User Secrets. Reinicie a aplicação após configurá-las.

| Variável | Valor |
| --- | --- |
| Smtp__Host e Smtp__Porta | Servidor de saída e porta STARTTLS do provedor (padrão 587) |
| Smtp__Remetente | Endereço autorizado pelo provedor |
| Smtp__Usuario e Smtp__Senha | Credenciais SMTP do remetente, quando exigidas |
| App__UrlPublica | Endereço HTTPS público da aplicação, terminado em barra, usado nos links de recuperação |

A presença do host seleciona a entrega SMTP. Em produção, o envio requer STARTTLS.

## Banco de dados

| Entidade | Armazenamento e relações |
| --- | --- |
| Professor | AspNetUsers armazena nome e matrícula (UserName). O índice único de NormalizedUserName protege a unicidade. O Identity mantém hash de senha, tentativas e bloqueio. AspNetRoles e AspNetUserRoles definem os papéis. |
| Aluno | Alunos armazena um apelido único por professor e pode referenciar um perfil pedagógico. |
| PerfilAdaptacao | PerfisAdaptacao é um catálogo de nomes únicos e reutilizáveis por alunos e adaptações. |
| Avaliacao | Avaliacoes pertence a um professor, com índice por professor e data. |
| Documento | Documentos guarda formato PDF/DOCX e chave privada única. Cada avaliação admite até um documento original. |
| AdaptacaoAvaliacao | Adaptacoes pertence a uma avaliação e pode referenciar um perfil. Inclui estado, resultado e campos para futura rastreabilidade. |
| HistoricoAdaptacao | HistoricoAdaptacoes referencia a adaptação e ordena eventos por data. |

As FKs impedem referências órfãs. Restrições CHECK limitam formatos e estados aos valores previstos. A matrícula preserva zeros iniciais. O modelo pedagógico dispensa nome civil do aluno, diagnóstico e laudos.

## Organização e segurança

Após o login, o painel consulta avaliações e adaptações do professor autenticado. Os indicadores contam avaliações originais, adaptações que chegaram à revisão ou aprovação, adaptações em revisão e avaliações distintas com alguma adaptação aprovada. O histórico também inclui as mudanças de estado registradas. Datas são apresentadas no fuso de São Paulo.

## Processar uma avaliação

Em **Minhas avaliações → Enviar avaliação**, selecione ou solte um PDF ou DOCX de até 10 MiB e confirme. O nome do arquivo identifica a avaliação. A tela de revisão oferece download do original, leitura do PDF no navegador quando suportada, blocos na ordem extraída, avisos, editor e exportação do JSON salvo. Reabra a revisão clicando no nome do arquivo na lista. Salvar mantém um rascunho; confirmar marca a avaliação como revisada. A exclusão exige confirmação e fica bloqueada se a avaliação já tiver adaptações.

**Armazenamento.** A implementação de `IDocumentStorageService` usa por padrão `%LOCALAPPDATA%\Adapta\Documentos`. A variável `Documentos__Diretorio` permite definir outra pasta privada. Na hospedagem, conceda acesso ao diretório somente à identidade da aplicação e aos responsáveis pelos backups. Faça backup do banco e dessa pasta juntos. As rotas MVC verificam a propriedade no servidor antes da leitura, revisão, exportação e exclusão.

**Extração.** Os leitores de PDF e DOCX implementam `IDocumentProcessingService`. PdfPig 0.1.16 lê texto e referências visuais por página. Open XML SDK 3.5.1 percorre o corpo do Word, parágrafos, tabelas e elementos especiais. O estruturador conserva os blocos de origem e usa marcadores para reconhecer enunciados, alternativas e apoios. Um processo .NET separado executa cada leitura, com prazo de 45 segundos, heap gerenciado de 256 MiB e até dois leitores por instância web. Outros limites incluem 150 páginas PDF, 1.500 partes e 40 MiB descompactados no DOCX, 8.000 blocos e 500.000 caracteres extraídos.

**Persistência.** A migração `20261125221039_ProcessamentoDocumentos` amplia a entidade Documento. O nome original, o proprietário e a data de envio vêm de Avaliacao. A chave interna identifica o arquivo relativo à pasta privada. Tamanho, formato, estados, datas e rowversion ficam em colunas. Questões, alternativas, avisos e blocos têm ordem e são usados em conjunto, por isso o agregado usa JSON. ExtracaoJson preserva a leitura inicial e RevisaoJson guarda somente os campos editáveis corrigidos. A exportação recompõe a versão efetiva com as referências ao original. A folha adaptada usa uma cópia da extração revisada no momento da geração.

O processamento passa por pendente, processando, concluído, concluído com avisos ou falhou. A revisão tem um estado independente: aguardando revisão ou revisado. Se a extração falhar depois da validação, o original permanece recuperável. Um processamento interrompido pode ser retomado pela tela após dez minutos. A versão de linha impede sobreposição silenciosa de revisões concorrentes.

**Limites de interpretação.** Não há OCR. Colunas, listas herdadas, objetos incorporados, tabelas complexas, fórmulas e gráficos exigem conferência no original. As referências visuais de PDF são por página. Na revisão é possível acrescentar uma questão que o leitor não reconheceu. Os bytes das imagens continuam no documento privado. O JSON contém referências, texto associado e avisos, sem duplicar os binários.

- **Controllers e Views** recebem requisições MVC e renderizam páginas Razor. Os formulários usam ViewModels, validação no servidor e proteção antifalsificação nos POSTs.
- **Services e DTOs** isolam as operações do Identity e levam os dados públicos da conta ao controller. O identificador do professor vem da identidade autenticada.
- **Models e Data** contêm as entidades, o DbContext, o mapeamento relacional, o seed e o histórico de migrações EF Core 10.
- **Rotas** exigem autenticação por padrão. A apresentação, o guia e o login são públicos. A gestão de usuários exige a matrícula 4805 e o papel Administrador. O logout usa POST. Cinco falhas consecutivas de login bloqueiam a conta por cinco minutos.
- **Interface** usa Bootstrap 5 local, CSS responsivo e JavaScript para alternar a visibilidade da senha. A senha vem oculta por padrão.

## Teste de integração

Requer Python 3, sqlcmd, SDK .NET 10 e acesso do usuário Windows à instância LocalDB. Após restaurar as ferramentas, execute:

```powershell
dotnet build Adapta.sln
python tests/test_conta.py
```

O teste cria um banco temporário exclusivo, aplica as migrações e inicia Kestrel e um receptor SMTP de teste em loopback. Verifica o login direto de contas com e sem e-mail, atualização das contas da versão anterior, troca e recuperação da senha, rejeição de link reutilizado, papéis e proteção antifalsificação. Ao terminar, encerra os servidores de teste e remove o banco temporário.

O mesmo teste confere indicadores com registros de dois professores e a contagem de avaliações aprovadas com múltiplas versões. Para incluir navegação em desktop e celular, disponibilize Playwright para Node e Microsoft Edge e defina `ADAPTA_TESTAR_NAVEGADOR=1` antes de executar o teste.

## Arquivos da etapa documental

Os caminhos abaixo são relativos a `src/Adapta.Web`, exceto quando indicada a raiz da solução.

| Alteração | Arquivos |
| --- | --- |
| Novos contratos e processamento | `DTOs/Documentos/ExtractedAssessment.cs`, `Services/Documentos/Processadores.cs`, `DocumentStorage.cs`, `LeitorDocumento.cs`, `PdfProcessor.cs`, `DocxProcessor.cs`, `NumeracaoWord.cs`, `Estruturador.cs`, `DocumentoService.cs` e `RevisaoDocumento.cs` na pasta de serviços documentais |
| Novas rotas, telas e interação | `Controllers/DocumentosController.cs`, `ViewModels/DocumentoViewModel.cs`, `Views/Documentos/Enviar.cshtml`, `Revisar.cshtml`, `_Questao.cshtml`, `Views/Shared/_UploadDocumento.cshtml`, `wwwroot/js/upload.js` e `revisao.js` |
| Persistência | Alterados `Models/Documento.cs`, `Data/AdaptaDbContext.cs` e `Data/Migrations/AdaptaDbContextModelSnapshot.cs`. Criados `20261125221039_ProcessamentoDocumentos.cs` e seu `.Designer.cs` em `Data/Migrations` |
| Integração com o portal | Alterados `Program.cs`, `Adapta.Web.csproj`, `Services/PortalDocenteService.cs`, `ViewModels/PortalViewModels.cs`, `Views/Painel/Index.cshtml`, `Avaliacoes.cshtml`, `NovaAdaptacao.cshtml`, `Views/Home/Guia.cshtml`, `wwwroot/css/portal.css` e `wwwroot/js/nova-adaptacao.js` |
| Solução e validação | Na raiz, alterados `Adapta.sln`, `README.md`, `tests/test_conta.py` e `tests/portal.browser.cjs`. Criados em `tests/Adapta.Tests` o projeto `Adapta.Tests.csproj` e os arquivos `Amostras.cs`, `ExtracaoTests.cs`, `PortalFixture.cs` e `DocumentoHttpTests.cs` |

## Executar os testes documentais

Para testar o módulo documental, execute `dotnet test tests/Adapta.Tests` na raiz com o LocalDB disponível. O xUnit cria e remove um banco temporário e usa dois docentes para verificar o isolamento.

## Criar professores e editar nomes

Entre com a conta administrativa 4805 e abra **Usuários** na barra lateral ou **Meu perfil → Gerenciar usuários**. Em **Criar usuário**, informe nome, matrícula e senha inicial com confirmação. A nova conta entra como professor. Use **Editar nome** na linha de qualquer conta, inclusive a própria, para atualizar o nome exibido no perfil e no cabeçalho. O cadastro e a edição são protegidos pelo Identity e pela autorização do servidor.

## Gerar e aprovar uma adaptação local

1. Em **Nova adaptação**, anexe a prova em PDF ou DOCX e confirme o envio.
2. No passo **Escolher perfil**, selecione **Autismo (TEA)** ou **Dislexia**. Confira a extração e continue para os apoios. Também é possível começar com uma avaliação já revisada.
3. Escolha um ou mais apoios de apresentação e clique em **Gerar adaptação**. Há opções de leitura ampliada, marcadores para acompanhar as instruções, espaço para respostas e comandos mais diretos. O nível de suporte é opcional e aparece apenas para TEA. A escolha de apoios é do professor.
4. No comparativo, veja o conteúdo extraído do original e a folha adaptada para cada questão. Em telas pequenas, o antes e o depois aparecem um abaixo do outro. A impressão inclui a folha adaptada. Confira o resultado e os avisos. Para ajustar a proposta, reveja o conteúdo de origem ou gere outra versão com apoios diferentes. No final do comparativo, consulte a justificativa de cada apoio aplicado. Após **Aprovar esta versão**, use **Baixar PDF adaptado** ou **Imprimir folha**. O PDF inclui a folha destinada ao estudante. A justificativa fica na área docente.
5. Volte a **Nova adaptação → Adaptações salvas** para reabrir o resultado. O painel contabiliza as versões e o histórico registra a aprovação.

O serviço funciona localmente sem chave nem envio do documento a terceiros. A opção de comandos diretos aplica substituições locais conhecidas no início de enunciados e instruções. Os demais trechos conservam a redação revisada. Elementos gráficos de página ficam assinalados na área docente, com links para o original. Consulte e anexe o material original quando houver imagens, gráficos ou fórmulas cuja representação exija conferência. A impressão do rascunho pelo próprio navegador o identifica como tal.

A tabela existente `Adaptacoes` guarda um retrato da folha em JSON, os apoios, a edição da base e o identificador do método. O serviço valida a propriedade do documento na geração, na leitura e na aprovação. Cada solicitação da mesma página gera uma única versão, inclusive ao repetir o POST. A etapa usa o esquema já implantado.

### Prompt de referência para revisão pedagógica

> Adapte a avaliação revisada para o perfil selecionado pelo professor (TEA ou Dislexia). Use os apoios escolhidos e as informações individuais fornecidas. Preserve o objetivo de cada questão, o vocabulário avaliado, os dados, as opções e a resposta correta. Se o professor pedir comandos mais diretos, separe ações quando isso ajudar a compreender a tarefa e mantenha a exigência de explicar, comparar ou justificar. Use níveis de suporte somente no perfil TEA. Quando uma questão depender de uma figura, preserve sua relação com a questão se ela estiver confirmada. Se essa relação for incerta, indique a página original uma única vez na revisão docente. Entregue a folha do estudante e, na área do professor, apresente o antes e o depois, o motivo de cada mudança observável e os materiais que precisam de conferência.

Esse texto documenta a orientação pedagógica. A geração disponível usa regras locais e registra os comandos efetivamente alterados.

## Base pedagógica e consulta documental

A edição **2026.11.4** reúne 20 seções de fundamentação, nove módulos temáticos e 17 referências. Os trechos `DUA-A04` e `TEA-V03` incorporam o e-book público *Pertencer* e a documentação institucional do [Positivo On](https://positivoon.com.br/). A autoria dos exemplos e das escolhas de adaptação é do projeto. A revisão pedagógica especializada continua pendente. [Documento principal](KnowledgeBase/BASE_CONHECIMENTO_ADAPTACAO_TEA.md) · [Referências bibliográficas](KnowledgeBase/REFERENCES.md)

| Pasta | O que consultar |
| --- | --- |
| [TEA](KnowledgeBase/TEA/) | Fundamentos, níveis de suporte, estratégias, acessibilidade visual, edição de enunciados e seis pares de questões. |
| [Dislexia](KnowledgeBase/Dislexia/apresentacao_da_prova.md) | Seleção individual de apoios de leitura, guia da British Dyslexia Association e limites das recomendações. |
| [DUA](KnowledgeBase/DUA/) | Princípios do desenho universal e revisão de equivalência da avaliação. |

### Recuperar trechos com a fonte

O utilitário documental requer Python 3. Ele lê os Markdown, conserva cada seção com suas condições e limitações e resolve os códigos de fonte no catálogo central. A saída JSONL contém um objeto por linha, com caminho e título do documento, título da seção, identificador, categoria, natureza, versão e estado editorial. Cada fonte acompanha o objeto com sua autoria, título, ano, publicação, tipo, alcance e endereço ou DOI. Um ano desconhecido é exportado como `null`. Os exemplos mantêm o par original/adaptado e a justificativa no mesmo objeto.

Na raiz do projeto, consulte apenas a orientação sobre segmentação ou exporte o corpus para um arquivo de trabalho:

```powershell
python -B tools/exportar_base.py --id TEA-L03
python -B tools/exportar_base.py --saida "$env:TEMP\adapta-base-tea.jsonl"
```

Em uma recuperação futura, formule a busca pela habilidade e pela barreira observada. Por exemplo, para “o estudante perde o lugar ao registrar três entregas de um problema”, recupere a segmentação (`TEA-L03`), o exemplo de instrução extensa (`EX-05`) e a revisão da equivalência (`DUA-A02`). Leia o limite junto com a estratégia: se planejar a sequência for parte do objetivo, oferecer os passos prontos altera a evidência de aprendizagem.

Associe a cada proposta o identificador e a versão do trecho efetivamente usado. As seções 13, 14, 16 e 17 do documento principal orientam a preservação curricular e a aprovação docente. Exemplos e hipóteses orientam discussão ou ensaio pedagógico. Quando faltar informação sobre o objetivo, a necessidade individual ou um elemento do original, devolva a decisão ao professor. O exportador opera offline e é independente da aplicação MVC. A recuperação por modelo generativo continua como etapa futura.
