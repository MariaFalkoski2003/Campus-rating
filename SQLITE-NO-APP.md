# SQLite no aplicativo

O aplicativo agora mantém uma cópia local das consultas de locais, detalhes/comentários, Meus relatos e avisos. A API continua sendo a fonte principal e responsável por autenticação, alterações da conta e envio de relatos.

## Arquivo e tabelas

O banco `campusrating-local.sqlite` é criado automaticamente na primeira execução, na pasta privada retornada por `FileSystem.AppDataDirectory`. Não é o mesmo arquivo SQLite usado pelo servidor. Não é necessário copiar um banco manualmente para o projeto.

| Tabela | Conteúdo |
|---|---|
| `locais` | Dados dos locais e contagem de comentários. |
| `relatos` | Relatos do aluno e respostas já consultadas. |
| `avisos` | Respostas notificadas e estado de leitura confirmado pela API. |
| `consultas_salvas` | Datas de atualização, proprietário da cópia, indicação de lista completa/parcial e detalhes dos locais em JSON. |

O pacote `sqlite-net-pcl` cria as tabelas ao abrir o banco. As gravações relacionadas usam transações. Cada consulta carrega a identificação da conta e da sessão local; respostas que chegam depois de uma saída/troca de conta são rejeitadas.

## Comportamento das telas

- Com conexão: consulta a API e atualiza a cópia local. Se a cópia não puder ser gravada, a tela mostra os dados do servidor com um aviso.
- Sem conexão com o servidor: consulta a cópia já salva e mostra sua data. Listas parciais são identificadas. Uma tela nunca consultada apresenta uma orientação para abri-la com conexão primeiro.
- Erros de autenticação, validação, registro inexistente ou erro do servidor não são substituídos silenciosamente pela cópia local.
- Detalhes de um local e detalhes/respostas de um relato precisam ter sido consultados para estarem disponíveis sem conexão. A lista de relatos, sozinha, não garante que suas respostas estejam salvas.
- Envio de relato: depende da API. Após confirmação, salva o relato e atualiza as contagens locais. Uma falha ao salvar essa cópia não transforma um envio confirmado em erro nem solicita outro envio.
- Leitura de aviso: é confirmada pela API. Sem conexão, pode-se consultar um relato já salvo, mas o aviso permanece não lido até ser aberto novamente com conexão.
- Atualizações offline não são enfileiradas. Edição de perfil, exclusão de conta e cadastro continuam online.

## Sessão e dados pessoais

Token e identificação do usuário ficam no `SecureStorage` do dispositivo; nenhuma senha é gravada no SQLite. Uma sessão salva e ainda dentro do prazo permite reabrir o app sem repetir o login. O primeiro login precisa de conexão. Se o armazenamento seguro estiver indisponível, o login atual pode continuar, mas a sessão não será restaurada na próxima abertura.

Ao sair, excluir a conta ou receber uma rejeição de sessão da API, o app remove a sessão salva e as cópias pessoais de relatos, avisos e detalhes/comentários. A lista pública dos locais permanece. Trocar a conta também limpa as cópias pessoais anteriores. Tokens expirados não permitem consulta offline. Revogações feitas no servidor são detectadas quando há conexão novamente.

## Arquivos alterados e motivos

| Arquivo | Responsabilidade |
|---|---|
| `CampusRating/Servicos/BancoLocal.cs` | DAO SQLite: tabelas, transações, leitura/gravação, limpeza e isolamento por conta/sessão. |
| `CampusRating/Servicos/ModelosDoBancoLocal.cs` | Estrutura das tabelas e metadados das cópias. |
| `CampusRating/Servicos/DadosDoCampus.cs` | Consulta à API, atualização do SQLite e recuperação da cópia somente em falhas de conexão. |
| `CampusRating/Servicos/ServicosLocais.cs` | Caminho privado e instâncias compartilhadas do banco e do serviço. |
| `CampusRating/Servicos/SessaoDoUsuario.cs` | Persistência segura, restauração, prazo da sessão e limpeza do banco. |
| `CampusRating/Servicos/ApiCampusRating.cs` | Rejeita sessões expiradas e respostas de requisições de uma sessão anterior. |
| `CampusRating/HomePage.xaml` e `.xaml.cs` | Consultas locais de Locais, Meus relatos e Avisos; mensagens de cópia offline; encerramento de sessão aguardado. |
| `CampusRating/DetalhesLocalPage.xaml` e `.xaml.cs` | Cópia dos detalhes/comentários e gravação local após envio confirmado. |
| `CampusRating/LoginPage.xaml.cs` | Restaura a sessão salva e aguarda a preparação antes de habilitar o login. |
| `CampusRating/SignPage.xaml.cs` | Aguarda a gravação da sessão após cadastro. |
| `CampusRating/ModaisDaPlataforma.cs` | Indicação da cópia offline nos relatos e explicação do armazenamento nas regras. |
| `CampusRating/CampusRating.csproj` | Dependência `sqlite-net-pcl` 1.11.285. |

## Verificação

Verificação focada em console, usando os arquivos reais dos serviços e um banco temporário: persistência após fechar/reabrir, lista vazia versus ausência de cópia, recuperação offline, propagação dos erros da API, atualização de envio sem duplicar contagem, limpeza e isolamento entre contas, rejeição de resposta antiga, sessão expirada e falha de envio sem criar um relato fictício. Os testes de sessão substituem as APIs do dispositivo por armazenamento em memória; não validam a implementação nativa do SecureStorage.

Build Windows e compilação C#/XAML Android validados. Não foi feita navegação visual no emulador nem gerado um APK nesta alteração.

Para conferir no dispositivo: entre com conexão, abra os locais, detalhes, Meus relatos e Avisos; feche o app; interrompa a conexão com a API e reabra. As consultas já salvas devem aparecer com a indicação da cópia. Sair da conta remove os dados pessoais e exige novo login online.
