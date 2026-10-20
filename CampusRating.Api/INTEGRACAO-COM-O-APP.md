# Integração do app .NET MAUI com a API

O que mudou no projeto `CampusRating` e por quê.

---

## Arquivos novos em `Servicos/`

| Arquivo | Papel |
|---|---|
| `ConfiguracaoDaApi.cs` | Guarda o endereço da API. |
| `ModelosDaApi.cs` | Os `record` que representam o JSON que vai e volta. |
| `ExcecaoDaApi.cs` | O erro que o app entende e sabe exibir. |
| `ApiCampusRating.cs` | Faz as chamadas HTTP de registro e login. |
| `SessaoDoUsuario.cs` | Guarda o token e o usuário logado enquanto o app está aberto. |

### `ConfiguracaoDaApi.cs` — o detalhe do `10.0.2.2`

No emulador Android, `localhost` é o **próprio emulador**, não o computador. O endereço `10.0.2.2` é o apelido que o emulador dá à máquina que o hospeda. Por isso a classe escolhe o endereço conforme a plataforma. Rodando como aplicativo Windows, `localhost` funciona normalmente.

Se forem testar em um **celular físico** na mesma rede Wi-Fi, troquem por `http://192.168.x.x:3000` — o IP do computador, que sai com `ipconfig` no Windows.

### `ModelosDaApi.cs` — por que `JsonPropertyName`

A API responde em minúsculas (`token`, `usuario`, `apelido`) e o C# usa PascalCase (`Token`, `Usuario`, `Apelido`). O atributo `[JsonPropertyName]` faz essa ponte de forma explícita, sem depender de configuração global do serializador. São `record` porque esses objetos só carregam dados e não mudam depois de criados.

### `ApiCampusRating.cs` — um `HttpClient` só

A classe expõe `ApiCampusRating.Instancia`, uma única instância criada sob demanda (`Lazy<T>` cuida disso com segurança entre threads). O motivo é conhecido: criar um `HttpClient` por chamada esgota as conexões do sistema. Um por aplicativo é o recomendado.

O método `EnviarAsync` faz sempre a mesma sequência:

1. Envia o objeto como JSON (`PostAsJsonAsync`).
2. Se a conexão falhar ou estourar os 15 segundos, converte em um erro claro de "servidor fora do ar" — sem isso o app mostraria uma mensagem técnica incompreensível.
3. Se o status não for de sucesso, lê o JSON de erro da API e transforma em `ExcecaoDaApi`, preservando `codigo`, `mensagem` e `campo`.
4. Se der certo, devolve a resposta desserializada.

Assim a tela não precisa saber nada de HTTP: ela chama um método e trata um único tipo de exceção.

### `SessaoDoUsuario.cs`

Guarda o token em memória por enquanto. É deliberadamente simples: quando o app fecha, a sessão se perde. O próximo passo natural é trocar por `SecureStorage.SetAsync("token", ...)`, que grava de forma criptografada no dispositivo.

---

## Mudanças em arquivos existentes

### `SignPage.xaml`

Só uma linha: o botão "Criar conta" ganhou `x:Name="CreateAccountButton"`, para que o code-behind consiga desabilitá-lo durante o envio.

### `SignPage.xaml.cs`

O método que antes se chamava `ValidateRegistrationAsync` virou `RegistrarAsync`. A validação local continua ali — ela dá resposta instantânea, sem ida ao servidor — mas deixou de ser o fim da história:

- `_enviando` impede dois cadastros simultâneos se a pessoa tocar duas vezes no botão.
- `DefinirEstadoDeEnvio` desabilita o botão e troca o texto para "Criando conta...", dando retorno visual durante a espera.
- Em caso de sucesso, a sessão é iniciada e **o apelido exibido passa a ser o que veio do servidor** — que pode ser diferente do sorteado, caso aquele já estivesse em uso.
- Em caso de erro, a mensagem exibida é a que a API mandou, e o campo indicado por `erro.Campo` recebe o foco.

Note que a validação local e a do servidor são propositalmente redundantes. A do app existe pela experiência de uso; a do servidor existe porque nenhuma validação feita no cliente é confiável — qualquer requisição pode chegar direto na API, sem passar pelo app.

### `Platforms/Android/AndroidManifest.xml` e `Resources/xml/network_security_config.xml`

Desde o Android 9, aplicativos não podem fazer requisições em HTTP puro. Como a API roda local, sem certificado, é preciso uma exceção declarada para `10.0.2.2` e `localhost`. É só isso que o arquivo de configuração faz, e vale **apenas para esses dois endereços** — o resto do app continua exigindo HTTPS. Em produção, a API teria certificado e este arquivo poderia sair.

---

## Como testar a integração

1. Terminal na pasta `CampusRating.Api`: `npm run dev`.
2. Rodar o app pelo Visual Studio (emulador Android ou Windows).
3. Preencher e-mail e senha e tocar em "Criar conta".
4. No terminal da API nada aparece em caso de sucesso — para conferir, cadastre o mesmo e-mail duas vezes: na segunda o app deve mostrar "Já existe uma conta com esse e-mail."
5. Com a API desligada, o app deve mostrar a mensagem de servidor inacessível, sem travar.

Para inspecionar o banco, abra `CampusRating.Api/dados/campusrating.sqlite` no [DB Browser for SQLite](https://sqlitebrowser.org/) e veja a tabela `usuarios` — a coluna `senha_hash` mostra bem por que a senha nunca é guardada em texto puro.
