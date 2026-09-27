# Campus Rating API

API REST em Node.js que atende o aplicativo .NET MAUI. Esta primeira fatia cobre **cadastro e login** com apelido anônimo, conforme o item 3.1 do documento do trabalho (Back-end: Node.js e API REST).

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
| `DOMINIOS_PERMITIDOS` | vazio (aceita qualquer domínio) | Lista separada por vírgula, ex.: `uri.edu.br`. Serve para exigir e-mail institucional. |

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
    │   └── repositorioDeUsuarios.js  todo o SQL de usuários fica aqui
    ├── servicos/
    │   ├── autenticacao.js           as regras de negócio de cadastro e login
    │   ├── apelidoAnonimo.js         sorteia um apelido que ainda não existe
    │   └── token.js                  gera e valida o JWT
    ├── middlewares/
    │   ├── exigirAutenticacao.js     bloqueia rotas privadas sem token válido
    │   └── tratarErros.js            converte qualquer erro em uma resposta JSON
    └── rotas/
        └── autenticacao.js           os três endereços de /auth
```

### A ideia por trás dessa divisão

É a separação em camadas que a documentação menciona. Cada arquivo tem um assunto só:

- **Rota** recebe a requisição HTTP e devolve a resposta HTTP. Não sabe SQL nem regra de negócio.
- **Serviço** decide o que é válido, o que é conflito, o que vira token. Não sabe que existe HTTP.
- **Repositório** é o único lugar do projeto que escreve SQL. Se amanhã o banco mudar, só esse arquivo muda.
- **Middleware** é código que roda entre a chegada da requisição e a rota (autenticação) ou depois dela (tratamento de erro).

Na prática isso significa que, para adicionar avaliações depois, vocês criam `rotas/avaliacoes.js`, `servicos/avaliacoes.js` e `banco/repositorioDeAvaliacoes.js` — sem mexer em nada do que já funciona.

### Detalhes que valem entender

**`configuracao.js`** existe para que nenhum valor fique espalhado pelo código. Porta, caminho do banco e segredo do token vêm de variáveis de ambiente, com um valor padrão para desenvolvimento. É o que permite rodar a mesma API na máquina de vocês e, um dia, em um servidor, sem editar código.

**`conexao.js`** abre o banco uma única vez e exporta esse objeto. Como o Node guarda em cache cada módulo importado, todos os arquivos que fizerem `import { banco }` recebem exatamente a mesma conexão. Ele liga dois `pragma`: `journal_mode = WAL`, que permite ler enquanto se escreve, e `foreign_keys = ON`, porque o SQLite não valida chaves estrangeiras por padrão — isso será necessário quando entrarem avaliações e locais.

**`repositorioDeUsuarios.js`** prepara as consultas SQL no momento em que o arquivo é carregado (`banco.prepare(...)`) e depois só as executa. Preparar uma vez e executar muitas é mais rápido e é o que garante o uso de parâmetros em vez de concatenação. Repare que `esquema.sql` usa `COLLATE NOCASE` no e-mail e no apelido: assim `Maria@uri.edu.br` e `maria@uri.edu.br` são a mesma conta, e não existem dois apelidos iguais com maiúsculas diferentes.

**`autenticacao.js`** concentra as regras: formato do e-mail, domínio institucional quando configurado, tamanho mínimo da senha, confirmação igual à senha, e-mail ainda não cadastrado, hash da senha e escolha do apelido. Ele nunca devolve `senha_hash` para fora — a função `apresentarUsuario` monta um objeto só com o que o app pode ver.

**`apelidoAnonimo.js`** usa as mesmas listas de animais e qualidades que já existiam na tela de cadastro. A diferença é que aqui o apelido é conferido contra o banco: se o que o app sugeriu já estiver em uso, o servidor sorteia outro e devolve o valor final na resposta. Por isso o app deve sempre exibir o apelido que **veio da resposta**, não o que ele mesmo sorteou.

**`tratarErros.js`** é registrado por último, e é o que garante que o app nunca receba um HTML de erro do Express. Erros previstos (os do tipo `ErroDaApi`) viram uma resposta com código e mensagem prontos para exibir na tela; qualquer outro vira um `500` genérico, e o detalhe real fica só no console do servidor — não se devolve stack trace para o cliente.

---

## 4. Os endpoints

### `POST /auth/registro`

```json
{
  "email": "maria@uri.edu.br",
  "senha": "123456",
  "confirmacaoSenha": "123456",
  "apelido": "Coruja Serena 47"
}
```

`confirmacaoSenha` e `apelido` são opcionais. Se o apelido vier vazio ou já estiver em uso, o servidor gera outro.

**201 Created**

```json
{
  "token": "eyJhbGciOi...",
  "usuario": {
    "id": 1,
    "email": "maria@uri.edu.br",
    "apelido": "Coruja Serena 47",
    "criadoEm": "2026-09-20 23:30:01"
  }
}
```

### `POST /auth/login`

```json
{ "email": "maria@uri.edu.br", "senha": "123456" }
```

**200 OK** com o mesmo formato do registro.

### `GET /auth/eu`

Exige o cabeçalho `Authorization: Bearer <token>`. Devolve `{ "usuario": { ... } }`. Serve para o app confirmar, ao abrir, se o token guardado ainda vale.

### `GET /saude`

Sem autenticação. Usado para checar se a API está no ar.

### Formato dos erros

Todo erro sai no mesmo formato, o que permite ao app ter um único tratamento:

```json
{ "erro": { "codigo": "email_ja_cadastrado", "mensagem": "Ja existe uma conta com esse e-mail.", "campo": "email" } }
```

| Código | HTTP | Quando acontece |
|---|---|---|
| `email_invalido` | 400 | Formato de e-mail incorreto. |
| `email_nao_institucional` | 400 | Domínio fora da lista de `DOMINIOS_PERMITIDOS`. |
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

Uma tabela, por enquanto:

```sql
CREATE TABLE usuarios (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  email TEXT NOT NULL UNIQUE COLLATE NOCASE,
  senha_hash TEXT NOT NULL,
  apelido TEXT NOT NULL UNIQUE COLLATE NOCASE,
  criado_em TEXT NOT NULL DEFAULT (datetime('now'))
);
```

Uma observação importante para a documentação do trabalho: o documento descreve o SQLite como armazenamento **local, de cache e rascunhos, no dispositivo**. Aqui ele aparece em um segundo papel, como o banco **do servidor** — que o item 4.4 chama de "base de dados central". São dois bancos diferentes, com finalidades diferentes, e vale deixar isso explícito no texto do TCC: o SQLite do servidor guarda a verdade (contas, avaliações); o SQLite do dispositivo guardará cópia para leitura offline e rascunhos.

---

## 6. Próximos passos sugeridos

1. Guardar o token no dispositivo com `SecureStorage` para a pessoa não precisar entrar toda vez.
2. Criar as tabelas `locais` e `avaliacoes` e os endpoints `GET /locais`, `GET /locais/:id`, `POST /locais/:id/avaliacoes` — a `HomePage` deixa de usar o array fixo.
3. Criar `ocorrencias` com categoria, local e status, atendendo ao objetivo específico de acompanhar o andamento de cada registro.
4. Incluir o SQLite no app (`sqlite-net-pcl`) como cache das listas, fechando o uso descrito no item 4.5.
5. Montar a coleção do Postman com essas rotas para usar como evidência de teste no trabalho.
