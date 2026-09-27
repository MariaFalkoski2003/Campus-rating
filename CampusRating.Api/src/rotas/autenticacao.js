import { Router } from 'express';
import { entrar, perfilDoUsuario, registrar } from '../servicos/autenticacao.js';
import { exigirAutenticacao } from '../middlewares/exigirAutenticacao.js';

export const rotasDeAutenticacao = Router();

rotasDeAutenticacao.post('/registro', (requisicao, resposta, proximo) => {
  try {
    resposta.status(201).json(registrar(requisicao.body ?? {}));
  } catch (erro) {
    proximo(erro);
  }
});

rotasDeAutenticacao.post('/login', (requisicao, resposta, proximo) => {
  try {
    resposta.json(entrar(requisicao.body ?? {}));
  } catch (erro) {
    proximo(erro);
  }
});

rotasDeAutenticacao.get('/eu', exigirAutenticacao, (requisicao, resposta, proximo) => {
  try {
    resposta.json({ usuario: perfilDoUsuario(requisicao.usuarioId) });
  } catch (erro) {
    proximo(erro);
  }
});
