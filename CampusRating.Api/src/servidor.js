import { criarAplicacao } from './aplicacao.js';
import { configuracao } from './configuracao.js';
import { prepararAdministradorInicial } from './servicos/autenticacao.js';

prepararAdministradorInicial();

criarAplicacao().listen(configuracao.porta, '0.0.0.0', () => {
  console.log(`Campus Rating API ouvindo em http://localhost:${configuracao.porta}`);
});
