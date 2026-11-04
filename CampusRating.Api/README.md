# Campus Rating API

API REST em Node.js que atende o aplicativo .NET MAUI. Cobre cadastro com aceite das regras, login, edição de e-mail/senha, exclusão da conta, consulta dos locais, envio e acompanhamento dos próprios relatos e leitura de avisos de respostas.

---

## 1. Como rodar

```bash
cd Projeto/CampusRating.Api
npm install
npm run dev
```

O servidor sobe em `http://localhost:3000` e imprime uma linha confirmando. Para parar, `Ctrl + C`.

Teste rápido no navegador ou no Postman: `GET http://localhost:3000/saude` deve devolver `{"status":"ok","servico":"campus-rating-api"}`.

O arquivo do banco (`dados/campusrating.sqlite`) é criado sozinho na primeira execução. Ele está no `.gitignore` — cada pessoa da dupla terá o seu, e nenhum dado de teste vai para o GitHub.

### Se o `npm install` falhar

**`gyp ERR! find Python` / `Could not find any Python installation to use`** — o `better-sqlite3` tem uma parte escrita em C++. Normalmente o npm baixa essa parte já compilada (*prebuild*) para a sua versão do Node; quando não existe uma compilada, ele tenta compilar na hora, e aí precisa de Python e do compilador do Visual Studio. A solução não é instalar Python: é usar uma versão da biblioteca que tenha o binário pronto para o seu Node. A versão fixada no `package.json` (`^12.11.1`) cobre Node 20, 22, 24, 25 e 26. Se der esse erro, apague o que ficou pela metade e instale de novo:

```powershell
Remove-Item -Recurse -Force node_modules, package-lock.json -ErrorAction SilentlyContinue
npm install
```

**`EPERM: operation not permitted`** — algum programa está segurando arquivos dentro de `node_modules` (antivírus, sincronização de nuvem ou um `npm` anterior ainda rodando). Feche o terminal, abra outro e repita o comando acima.

**Versão do Node** — confira com `node -v`. O projeto pede Node 20 ou mais novo.

### Variáveis de ambiente (todas opcionais)

| Variável | Padrão | Para que serve |
|---|---|---|
| `PORTA` | `3000` | Porta onde a API escuta. |
| `CAMINHO_DO_BANCO` | `dados/campusrating.sqlite` | Onde fica o arquivo SQLite. |
| `SEGREDO_DO_TOKEN` | valor de desenvolvimento | Chave que assina os tokens. Em produção precisa ser um valor secreto e diferente. |
| `VALIDADE_DO_TOKEN` | `7d` | Quanto tempo o token continua válido. |

Cadastro e login aceitam somente e-mails do domínio completo `aluno.uricer.edu.br`. Essa regra é fixa em `src/configuracao.js`: a antiga variável `DOMINIOS_PERMITIDOS` não é mais utilizada. Maiúsculas/minúsculas e espaços nas pontas são normalizados; subdomínios e sufixos adicionais são rejeitados. Contas antigas com outro domínio permanecem no banco, mas não podem fazer login.

Para verificar a autenticação, os locais e seus contadores, execute `npm test`. Os testes usam um SQLite em memória e não alteram o banco da aplicação.

---

## 2. As quatro dependências e por que cada uma existe

**`express`** — o framework web. Sozinho, o Node consegue abrir uma porta e receber requisições HTTP, mas você teria que ler a URL, o método e o corpo da requisição na mão. O Express dá três coisas: **roteamento** (dizer "quando chegar um POST em `/auth/login`, execute esta função"), **middlewares** (funções que rodam antes ou depois da rota, como a que lê JSON ou a que trata erros) e uma forma pronta de responder (`resposta.status(201).json(...)`). É o que o documento chama de camada de serviços, separada da camada de apresentação (o app).

**`better-sqlite3`** — o driver que abre o arquivo `.sqlite` e executa SQL. Foi escolhido em vez do pacote `sqlite3` porque ele é **síncrono**: `const usuario = buscarPorEmail.get(email)` devolve a linha na hora, sem `await` e sem callback. Isso deixa o código muito mais próximo do SQL que vocês vão mostrar na documentação, e para o volume de um TCC não há perda de desempenho. Ele também suporta *prepared statements*, que é o mecanismo que impede SQL Injection: os valores nunca são concatenados dentro do texto SQL, são enviados separados.

**`bcryptjs`** — transforma a senha em um *hash*. O banco nunca guarda a senha digitada; guarda um resultado irreversível dela. Se o arquivo do banco vazar, as senhas continuam protegidas. O bcrypt também é propositalmente lento e embute um "sal" aleatório em cada hash, o que inviabiliza ataques de tabela pronta. Isso atende ao princípio de segurança citado no item 4.6 do documento.

**`jsonwebtoken`** — cria e verifica o token JWT. Depois que a pessoa se cadastra ou entra, o servidor devolve um token assinado. O app guarda esse token e o envia no cabeçalho `Authorization` das próximas chamadas. O servidor confere a assinatura e sabe quem está falando sem precisar guardar sessão nenhuma — é o "sem estado" (*stateless*) do REST descrito por Fielding e citado no item 4.4.

---

## 3. O que cada arquivo faz

```
CampusRating.Api/
├── package.json                      nome do projeto, scripts e dependências
├── .gitignore                        impede node_modules e o banco de irem ao GitHub
├── dados/campusrating.sqlite         criado em tempo de execução
└── src/
    ├── servidor.js                   ponto de entrada: sobe o servidor na porta
    ├── aplicacao.js                  monta o Express e pendura as rotas
    ├── configuracao.js               lê as variáveis de ambiente em um lugar só
    ├── erros.js                      define o tipo de erro que a API sabe responder
    ├── banco/
    │   ├── conexao.js                abre o arquivo SQLite e aplica o esquema
    │   ├── esquema.sql               o CREATE TABLE, em SQL puro
    │   ├── migrar.js                 comando avulso para preparar o banco
    │   ├── repositorioDeUsuarios.js  consultas e cadastro de usuários
    │   └── repositorioDeLocais.js    lista os locais e conta os relatos vinculados
    ├── servicos/
    │   ├── autenticacao.js           as regras de negócio de cadastro e login
    │   ├── apelidoAnonimo.js         sorteia um apelido que ainda não existe
    │   └── token.js                  gera e valida o JWT
    ├── middlewares/
    │   ├── exigirAutenticacao.js     bloqueia rotas privadas sem token válido
    │   └── tratarErros.js            converte qualquer erro em uma resposta JSON
    └── rotas/
        ├── autenticacao.js           os três endereços de /auth
        └── locais.js                 GET /locais, protegido por token
```

### A ideia por trás dessa divisão

É a separação em camadas que a documentação menciona. Cada arquivo tem um assunto só:

- **Rota** recebe a requisição HTTP e devolve a resposta HTTP. Não sabe SQL nem regra de negócio.
- **Serviço** decide o que é válido, o que é conflito, o que vira token. Não sabe que existe HTTP.
- **Repositório** é o único lugar do projeto que escreve SQL. Se amanhã o banco mudar, só esse arquivo muda.
- **Middleware** é código que roda entre a chegada da requisição e a rota (autenticação) ou depois dela (tratamento de erro).

Na prática isso significa que o envio de relatos e as respostas podem ser adicionados em suas próprias rotas, serviços e repositórios. A consulta de locais já usa esse padrão, com a rota HTTP separada do SQL.

### Detalhes que valem entender

**`configuracao.js`** existe para que nenhum valor fique espalhado pelo código. Porta, caminho do banco e segredo do token vêm de variáveis de ambiente, com um valor padrão para desenvolvimento. É o que permite rodar a mesma API na máquina de vocês e, um dia, em um servidor, sem editar código.

**`conexao.js`** abre o banco uma única vez e exporta esse objeto. Como o Node guarda em cache cada módulo importado, todos os arquivos que fizerem `import { banco }` recebem exatamente a mesma conexão. Ele liga dois `pragma`: `journal_mode = WAL`, que permite ler enquanto se escreve, e `foreign_keys = ON`, porque o SQLite não valida chaves estrangeiras por padrão — isso será necessário quando entrarem avaliações e locais.

**`repositorioDeUsuarios.js`** prepara as consultas SQL no momento em que o arquivo é carregado (`banco.prepare(...)`) e depois só as executa. Preparar uma vez e executar muitas é mais rápido e é o que garante o uso de parâmetros em vez de concatenação. Repare que `esquema.sql` usa `COLLATE NOCASE` no e-mail e no apelido: assim `Maria@aluno.uricer.edu.br` e `maria@aluno.uricer.edu.br` são a mesma conta, e não existem dois apelidos iguais com maiúsculas diferentes.

**`autenticacao.js`** concentra as regras: formato do e-mail e domínio obrigatório `aluno.uricer.edu.br` no cadastro e no login, tamanho mínimo da senha, confirmação igual à senha, e-mail ainda não cadastrado, hash da senha e escolha do apelido. Ele nunca devolve `senha_hash` para fora — a função `apresentarUsuario` monta um objeto só com o que o app pode ver.

**`apelidoAnonimo.js`** usa as mesmas listas de animais e qualidades que já existiam na tela de cadastro. A diferença é que aqui o apelido é conferido contra o banco: se o que o app sugeriu já estiver em uso, o servidor sorteia outro e devolve o valor final na resposta. Por isso o app deve sempre exibir o apelido que **veio da resposta**, não o que ele mesmo sorteou.

**`tratarErros.js`** é registrado por último, e é o que garante que o app nunca receba um HTML de erro do Express. Erros previstos (os do tipo `ErroDaApi`) viram uma resposta com código e mensagem prontos para exibir na tela; qualquer outro vira um `500` genérico, e o detalhe real fica só no console do servidor — não se devolve stack trace para o cliente.

---

## 4. Os endpoints

### `POST /auth/registro`

```json
{
  "email": "maria@aluno.uricer.edu.br",
  "senha": "123456",
  "confirmacaoSenha": "123456",
  "apelido": "Coruja Serena 47",
  "aceitouRegras": true
}
```

`confirmacaoSenha` e `apelido` são opcionais. Se o apelido vier vazio ou já estiver em uso, o servidor gera outro.

`aceitouRegras` deve ser o booleano `true`. Data e versão do aceite são gravadas no servidor.

**201 Created**

```json
{
  "token": "eyJhbGciOi...",
  "usuario": {
    "id": 1,
    "email": "maria@aluno.uricer.edu.br",
    "apelido": "Coruja Serena 47",
    "criadoEm": "2026-09-20 23:30:01"
  }
}
```

### `POST /auth/login`

```json
{ "email": "maria@aluno.uricer.edu.br", "senha": "123456" }
```

**200 OK** com o mesmo formato do registro.

### `GET /auth/eu`

Exige o cabeçalho `Authorization: Bearer <token>`. Devolve `{ "usuario": { ... } }`. Serve para o app confirmar, ao abrir, se o token guardado ainda vale.

### `GET /saude`

Sem autenticação. Usado para checar se a API está no ar.

### `GET /locais`

Exige `Authorization: Bearer <token>`. Devolve os locais predefinidos, na ordem da interface:

```json
{
  "locais": [
    {
      "id": 1,
      "nome": "Biblioteca Central",
      "categoria": "Biblioteca",
      "localizacao": "Prédio Principal, Térreo",
      "descricao": "Acervo completo, salas de estudo e computadores disponíveis.",
      "imagem": "campus_library.jpg",
      "quantidadeComentarios": 0
    }
  ]
}
```

O exemplo mostra um item; a base inicial inclui biblioteca, banheiros, cantina e laboratório. Os nomes das imagens correspondem aos arquivos já incluídos no aplicativo.

`quantidadeComentarios` é calculada por `COUNT` dos registros da tabela `relatos` associados ao local, independentemente do andamento. Cada relato original conta uma vez; respostas não entram nesse total. Não são retornadas notas, médias ou estrelas.

A pesquisa por nome/localização e os filtros por categoria continuam no aplicativo, sobre a lista recebida. Ao entrar na página ou voltar à aba Locais, a lista é consultada novamente. Há estados de carregamento, falha com tentativa novamente e busca sem resultado.

Não há endpoints para cadastrar, editar ou excluir locais.

### `GET /locais/:id`

Exige `Authorization: Bearer <token>`. Retorna `{ "local": { ... }, "comentarios": [ ... ] }`.
O objeto `local` contém os mesmos campos da listagem. Os comentários são os relatos originais
do espaço, ordenados do mais recente para o mais antigo, com `id`, `apelido`, `texto`, `status`
e `criadoEm`. O e-mail do autor não é exposto. Sem relatos, a lista é vazia e o contador é zero.

Identificadores inválidos retornam `400`; locais inexistentes retornam `404`.
No aplicativo, tocar em um local abre `DetalhesLocalPage`, com imagem, categoria, localização,
descrição e comentários. Há carregamento, tentativa novamente, estado vazio e retorno à lista.
O botão "Enviar relato" abre um formulário modal, envia o texto à API e atualiza os comentários após o sucesso.

### Operações do perfil, relatos e avisos

Todas as rotas abaixo exigem `Authorization: Bearer <token>`.

| Método e caminho | Corpo / resultado |
|---|---|
| `PATCH /auth/eu/email` | `{ "email": "novo@aluno.uricer.edu.br", "senhaAtual": "..." }`. Mantém o apelido e retorna token/usuário atualizados. |
| `PATCH /auth/eu/senha` | `{ "senhaAtual": "...", "senha": "...", "confirmacaoSenha": "..." }`. Exige pelo menos 6 caracteres e retorna token/usuário atualizados. |
| `DELETE /auth/eu` | `{ "senhaAtual": "..." }`. Exclui a conta, seus relatos e respostas vinculadas em transação. |
| `POST /locais/:id/relatos` | `{ "texto": "..." }`, até 2.000 caracteres. Retorna `201` com `{ "relato": ... }`, autor da sessão e status `pendente`. |
| `GET /relatos/meus` | `{ "relatos": [...] }`, somente do usuário autenticado. |
| `GET /relatos/:id` | `{ "relato": ... }` com respostas, somente quando pertence ao usuário. |
| `GET /avisos` | `{ "avisos": [...] }`, derivados das respostas reais aos relatos do aluno, com `lida` booleano. |
| `PATCH /avisos/:id/lido` | Marca um aviso do próprio aluno como lido. |

Alterar e-mail ou senha invalida os tokens anteriores. O aplicativo usa o novo token retornado. Excluir uma conta também impede o uso de seu token antigo. Não há edição nem exclusão de relatos individuais.

O formulário de exclusão informa que os relatos e as respostas vinculadas serão removidos, pede confirmação e exige a senha atual. Os dados dos demais alunos e os locais predefinidos são preservados.

A consulta de respostas e avisos está pronta. A área administrativa para produzir respostas e alterar andamentos ainda será implementada. Nenhum aviso fictício é gerado.

### Formato dos erros

Todo erro sai no mesmo formato, o que permite ao app ter um único tratamento:

```json
{ "erro": { "codigo": "email_ja_cadastrado", "mensagem": "Ja existe uma conta com esse e-mail.", "campo": "email" } }
```

| Código | HTTP | Quando acontece |
|---|---|---|
| `email_invalido` | 400 | Formato de e-mail incorreto. |
| `email_nao_institucional` | 400 | Domínio diferente de `aluno.uricer.edu.br` no cadastro ou no login. |
| `senha_curta` | 400 | Menos de 6 caracteres. |
| `senhas_diferentes` | 400 | Confirmação não confere. |
| `email_ja_cadastrado` | 409 | Já existe conta com esse e-mail. |
| `credenciais_invalidas` | 401 | E-mail ou senha errados no login. |
| `token_ausente` / `token_invalido` | 401 | Rota privada sem token ou com token expirado. |
| `rota_nao_encontrada` | 404 | Endereço que não existe. |
| `erro_interno` | 500 | Falha inesperada no servidor. |

O campo `campo` indica qual entrada da tela destacar — o app usa isso para dar foco no `Entry` certo.

---

## 5. O banco

O esquema atual possui cinco tabelas no servidor:

- `usuarios`: contas e apelidos, preservados ao atualizar o esquema.
- `locais`: os quatro espaços predefinidos, com código único, nome, categoria, localização, descrição, imagem e ordem.
- `relatos`: comentários vinculados a um local e ao autor, com texto, andamento e data.
- `respostas`: respostas da administração aos relatos, consultadas nos detalhes e em avisos.
- `avisos_lidos`: registro de quais respostas o aluno já abriu.

A tabela de usuários mantém suas contas existentes. A migração adiciona `regras_aceitas_em`, `versao_regras` e `versao_token` quando necessário. Contas antigas permanecem com o aceite vazio; um aceite não é inventado retroativamente. Estrutura original:

```sql
CREATE TABLE usuarios (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  email TEXT NOT NULL UNIQUE COLLATE NOCASE,
  senha_hash TEXT NOT NULL,
  apelido TEXT NOT NULL UNIQUE COLLATE NOCASE,
  criado_em TEXT NOT NULL DEFAULT (datetime('now'))
);
```

O esquema é aplicado em uma transação. Os locais são inseridos pelo código único com `ON CONFLICT DO NOTHING`: executar novamente não duplica os locais, não muda seus identificadores e não apaga contas ou relatos. Para atualizar uma base existente, execute `npm run migrar`; iniciar a API também aplica o esquema automaticamente.

Uma observação importante para a documentação do trabalho: o documento descreve o SQLite como armazenamento **local, de cache e rascunhos, no dispositivo**. Aqui ele aparece em um segundo papel, como o banco **do servidor** — que o item 4.4 chama de "base de dados central". São dois bancos diferentes, com finalidades diferentes, e vale deixar isso explícito no texto do TCC: o SQLite do servidor guarda a verdade (contas, avaliações); o SQLite do dispositivo guardará cópia para leitura offline e rascunhos.

---

## 6. Próximos passos sugeridos

1. Guardar o token no dispositivo com `SecureStorage` para a pessoa não precisar entrar toda vez.
2. Implementar a área administrativa, as respostas e a alteração dos estados `pendente`, `em_analise` e `resolvido`.
3. Organizar gradualmente a aplicação em MVVM e revisar a navegação exigida na disciplina.
4. Incluir o SQLite no app (`sqlite-net-pcl`) como cache das listas, fechando o uso descrito no item 4.5.
5. Montar a coleção do Postman com essas rotas para usar como evidência de teste no trabalho.
