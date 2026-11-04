import { lerToken } from '../servicos/token.js';
import { erroDeAutenticacao } from '../erros.js';
import { repositorioDeUsuarios } from '../banco/repositorioDeUsuarios.js';

export const exigirAutenticacao = (requisicao, resposta, proximo) => {
  const cabecalho = requisicao.headers.authorization ?? '';
  const [esquema, token] = cabecalho.split(' ');

  if (esquema !== 'Bearer' || !token) {
    return proximo(erroDeAutenticacao('token_ausente', 'Envie o token de acesso.'));
  }

  try {
    const conteudo = lerToken(token);
    requisicao.usuarioId = Number(conteudo.sub);
    const usuario = repositorioDeUsuarios.porId(requisicao.usuarioId);
    if (!usuario) throw erroDeAutenticacao('usuario_nao_encontrado', 'Conta não encontrada. Entre novamente.');
    if ((conteudo.versao ?? 0) !== usuario.versao_token) {
      throw erroDeAutenticacao('token_invalido', 'Sua sessão expirou. Entre novamente.');
    }
    return proximo();
  } catch (erro) {
    return proximo(erro);
  }
};
