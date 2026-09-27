import express from 'express';
import { rotasDeAutenticacao } from './rotas/autenticacao.js';
import { rotaNaoEncontrada, tratarErros } from './middlewares/tratarErros.js';

export const criarAplicacao = () => {
  const aplicacao = express();

  aplicacao.use(express.json());

  aplicacao.get('/saude', (requisicao, resposta) => {
    resposta.json({ status: 'ok', servico: 'campus-rating-api' });
  });

  aplicacao.use('/auth', rotasDeAutenticacao);

  aplicacao.use(rotaNaoEncontrada);
  aplicacao.use(tratarErros);

  return aplicacao;
};
