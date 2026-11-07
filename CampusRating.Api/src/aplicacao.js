import express from 'express';
import { rotasDeAutenticacao } from './rotas/autenticacao.js';
import { rotasDeLocais } from './rotas/locais.js';
import { rotasDeRelatos, rotasDeAvisos } from './rotas/relatos.js';
import { rotasDeAdministracao } from './rotas/administracao.js';
import { rotaNaoEncontrada, tratarErros } from './middlewares/tratarErros.js';

export const criarAplicacao = () => {
  const aplicacao = express();

  aplicacao.use(express.json());

  aplicacao.get('/saude', (requisicao, resposta) => {
    resposta.json({ status: 'ok', servico: 'campus-rating-api' });
  });

  aplicacao.use('/auth', rotasDeAutenticacao);
  aplicacao.use('/locais', rotasDeLocais);
  aplicacao.use('/relatos', rotasDeRelatos);
  aplicacao.use('/avisos', rotasDeAvisos);
  aplicacao.use('/admin/relatos', rotasDeAdministracao);

  aplicacao.use(rotaNaoEncontrada);
  aplicacao.use(tratarErros);

  return aplicacao;
};
