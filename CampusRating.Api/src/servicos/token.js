import jwt from 'jsonwebtoken';
import { configuracao } from '../configuracao.js';
import { erroDeAutenticacao } from '../erros.js';

export const gerarToken = (usuario) =>
  jwt.sign({ apelido: usuario.apelido }, configuracao.segredoDoToken, {
    subject: String(usuario.id),
    expiresIn: configuracao.validadeDoToken,
  });

export const lerToken = (token) => {
  try {
    return jwt.verify(token, configuracao.segredoDoToken);
  } catch {
    throw erroDeAutenticacao('token_invalido', 'Sessao expirada ou invalida. Entre novamente.');
  }
};
