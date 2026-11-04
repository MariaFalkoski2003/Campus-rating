# Ajustes nas telas existentes

Alterações feitas no mesmo projeto Campus Rating. O visual roxo e branco e os quatro locais predefinidos foram aproveitados.

## Comportamento das telas

- Cadastro: checkbox obrigatório, leitura das regras em modal e registro da data/versão do aceite no servidor. Contas antigas são preservadas sem inventar um aceite anterior.
- Locais: pesquisa e filtros existentes; a contagem é consultada novamente ao retornar dos detalhes.
- Detalhes: formulário modal de envio, texto obrigatório com até 2.000 caracteres, autor da sessão, status inicial Pendente, confirmação e atualização da lista após salvar. Respostas existentes aparecem abaixo do relato.
- Perfil: e-mail privado, alteração de e-mail institucional e senha com senha atual, apelido original preservado, Meus relatos, regras e exclusão da conta.
- Meus relatos: listagem dentro do perfil, RadioButtons para Todos/Pendentes/Em análise/Resolvidos e modal de consulta com respostas. Não há edição ou exclusão individual de relatos.
- Exclusão: confirmação, senha atual e aviso de remoção da conta e de seus relatos/respostas. Os dados de outros usuários e os locais são preservados.
- Avisos: consulta das respostas reais aos relatos do aluno, indicação lido/não lido, atualização e abertura do relato correspondente.

## Arquivos do aplicativo

| Arquivo | Motivo |
|---|---|
| `CampusRating/SignPage.xaml` | Checkbox e acesso às regras no cadastro. |
| `CampusRating/SignPage.xaml.cs` | Validação e envio do aceite. |
| `CampusRating/HomePage.xaml` | Controles do perfil, Meus relatos e lista de avisos. |
| `CampusRating/HomePage.xaml.cs` | Conexão dos controles com a API, filtros, confirmação da exclusão e limpeza de dados ao sair. |
| `CampusRating/DetalhesLocalPage.xaml` | Envio habilitado e apresentação das respostas. |
| `CampusRating/DetalhesLocalPage.xaml.cs` | Envio pelo modal e atualização dos comentários. |
| `CampusRating/ModaisDaPlataforma.cs` | Formulários reutilizáveis, regras e consulta do relato; proteção contra envio duplicado e exibição de erros. |
| `CampusRating/Servicos/ApiCampusRating.cs` | Chamadas HTTP das novas operações. |
| `CampusRating/Servicos/ModelosDaApi.cs` | Contratos JSON do aceite, relatos, respostas e avisos. |
| `CampusRating/Servicos/FormatacaoDoRelato.cs` | Formatação compartilhada de datas e andamentos. |

## Arquivos da API

| Arquivo | Motivo |
|---|---|
| `CampusRating.Api/src/aplicacao.js` | Registro das rotas de relatos e avisos. |
| `CampusRating.Api/src/banco/esquema.sql` | Tabelas de respostas e avisos lidos, com exclusão das referências vinculadas. |
| `CampusRating.Api/src/banco/conexao.js` | Migração aditiva dos campos de aceite e versão da sessão. |
| `CampusRating.Api/src/banco/repositorioDeUsuarios.js` | Registro do aceite, atualização de credenciais e exclusão em transação. |
| `CampusRating.Api/src/banco/repositorioDeLocais.js` | Inclusão das respostas nos comentários do local. |
| `CampusRating.Api/src/banco/repositorioDeRelatos.js` | Gravação, consulta dos próprios relatos, respostas e leitura dos avisos. |
| `CampusRating.Api/src/servicos/autenticacao.js` | Regras do aceite e das alterações/exclusão da conta. |
| `CampusRating.Api/src/servicos/token.js` | Versão da sessão no token. |
| `CampusRating.Api/src/middlewares/exigirAutenticacao.js` | Rejeição de contas excluídas e de tokens anteriores às mudanças de credenciais. |
| `CampusRating.Api/src/rotas/autenticacao.js` | Endpoints de edição e exclusão da conta. |
| `CampusRating.Api/src/rotas/locais.js` | Endpoint autenticado de envio de relatos. |
| `CampusRating.Api/src/rotas/relatos.js` | Endpoints de consulta pessoal e leitura de avisos. |
| `CampusRating.Api/test/telas-existentes.test.js` | Quatro testes de integração focados nas novas operações, em SQLite em memória. |
| `CampusRating.Api/test/email-institucional.test.js` e `test/locais.test.js` | Ajuste dos cadastros de teste para enviar o aceite agora obrigatório. |
| `CampusRating.Api/README.md` | Documentação dos endpoints, migração e escopo atual. |

## Execução e limites

Reiniciar a API aplica a migração automaticamente. Não é necessário apagar o banco. Recompilar e executar o aplicativo para carregar as novas telas.

A área administrativa que cria respostas e altera andamentos permanece uma próxima etapa. A lista de avisos fica vazia enquanto não houver respostas reais. O banco SQLite no dispositivo foi implementado conforme `SQLITE-NO-APP.md`. A migração geral para MVVM, Toast/Snackbar e a revisão dos tipos de navegação continuam nas etapas próprias.

## Verificação realizada

- Quatro testes novos da API aprovados, sem modificar o banco real.
- Build Windows aprovado com zero erros e avisos.
- Compilação C#/XAML Android aprovada com zero erros e avisos; não foi gerado um APK nesta verificação.
- Não foi realizada navegação visual no emulador nesta alteração.

## Etapa 4 — envio de relatos

Implementação concluída no código: abrir o formulário nos detalhes do local, informar texto, validar, gravar na API como Pendente e atualizar os comentários. O contador da listagem é consultado novamente ao voltar para Locais.

Ao retomar esta etapa, foram adicionados o contador de caracteres e o indicador de envio. A validação local limita o texto a 2.000 caracteres. O encerramento do modal aguarda a navegação terminar antes de informar o resultado; colocar o aplicativo em segundo plano não é tratado como cancelamento.

Arquivos ajustados nesta retomada: `CampusRating/ModaisDaPlataforma.cs` e `CampusRating/DetalhesLocalPage.xaml.cs`.

Verificação focada: teste de integração do fluxo de relatos aprovado em banco em memória e build Windows aprovado com zero erros e avisos. Não foi feita nova navegação no emulador.
