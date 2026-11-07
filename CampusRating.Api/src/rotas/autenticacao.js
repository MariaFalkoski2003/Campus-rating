import { Router } from 'express';
import { entrar, entrarAdministrador, perfilDoUsuario, registrar, alterarEmail, alterarSenha, excluirConta } from '../servicos/autenticacao.js';
import { exigirAutenticacao } from '../middlewares/exigirAutenticacao.js';

export const rotasDeAutenticacao = Router();

for (const [metodo, caminho, acao] of [
  ['patch', '/eu/email', alterarEmail], ['patch', '/eu/senha', alterarSenha], ['delete', '/eu', excluirConta],
]) {
  rotasDeAutenticacao[metodo](caminho, exigirAutenticacao, (requisicao, resposta, proximo) => {
    try { resposta.json(acao(requisicao.usuarioId, requisicao.body ?? {})); }
    catch (erro) { proximo(erro); }
  });
}

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

rotasDeAutenticacao.post('/admin/login', (requisicao, resposta, proximo) => {
  try { resposta.json(entrarAdministrador(requisicao.body ?? {})); }
  catch (erro) { proximo(erro); }
});

rotasDeAutenticacao.get('/eu', exigirAutenticacao, (requisicao, resposta, proximo) => {
  try {
    resposta.json({ usuario: perfilDoUsuario(requisicao.usuarioId) });
  } catch (erro) {
    proximo(erro);
  }
});
