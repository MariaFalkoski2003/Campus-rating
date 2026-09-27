import { lerToken } from '../servicos/token.js';
import { erroDeAutenticacao } from '../erros.js';

export const exigirAutenticacao = (requisicao, resposta, proximo) => {
  const cabecalho = requisicao.headers.authorization ?? '';
  const [esquema, token] = cabecalho.split(' ');

  if (esquema !== 'Bearer' || !token) {
    return proximo(erroDeAutenticacao('token_ausente', 'Envie o token de acesso.'));
  }

  try {
    const conteudo = lerToken(token);
    requisicao.usuarioId = Number(conteudo.sub);
    return proximo();
  } catch (erro) {
    return proximo(erro);
  }
};
