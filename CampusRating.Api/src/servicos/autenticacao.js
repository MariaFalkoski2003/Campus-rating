import bcrypt from 'bcryptjs';
import { configuracao } from '../configuracao.js';
import { repositorioDeUsuarios } from '../banco/repositorioDeUsuarios.js';
import { gerarApelidoDisponivel } from './apelidoAnonimo.js';
import { gerarToken } from './token.js';
import { erroDeAutenticacao, erroDeConflito, erroDeValidacao } from '../erros.js';

const formatoDeEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const normalizarEmail = (valor) => String(valor ?? '').trim().toLowerCase();

const validarEmail = (email) => {
  if (!formatoDeEmail.test(email)) {
    throw erroDeValidacao('email_invalido', 'Informe um e-mail valido.', 'email');
  }

  const dominio = email.split('@')[1];
  const listaDeDominios = configuracao.dominiosPermitidos;

  if (listaDeDominios.length > 0 && !listaDeDominios.includes(dominio)) {
    throw erroDeValidacao(
      'email_nao_institucional',
      `Use um e-mail institucional (${listaDeDominios.join(', ')}).`,
      'email'
    );
  }
};

const validarSenha = (senha) => {
  if (String(senha ?? '').length < configuracao.tamanhoMinimoDaSenha) {
    throw erroDeValidacao(
      'senha_curta',
      `A senha deve ter pelo menos ${configuracao.tamanhoMinimoDaSenha} caracteres.`,
      'senha'
    );
  }
};

const apresentarUsuario = (usuario) => ({
  id: usuario.id,
  email: usuario.email,
  apelido: usuario.apelido,
  criadoEm: usuario.criado_em,
});

export const registrar = ({ email, senha, confirmacaoSenha, apelido }) => {
  const emailNormalizado = normalizarEmail(email);

  validarEmail(emailNormalizado);
  validarSenha(senha);

  if (confirmacaoSenha !== undefined && senha !== confirmacaoSenha) {
    throw erroDeValidacao('senhas_diferentes', 'A confirmacao deve ser igual a senha.', 'confirmacaoSenha');
  }

  if (repositorioDeUsuarios.porEmail(emailNormalizado)) {
    throw erroDeConflito('email_ja_cadastrado', 'Ja existe uma conta com esse e-mail.', 'email');
  }

  const apelidoEscolhido = String(apelido ?? '').trim();
  const apelidoFinal =
    apelidoEscolhido.length > 0 && !repositorioDeUsuarios.apelidoEmUso(apelidoEscolhido)
      ? apelidoEscolhido
      : gerarApelidoDisponivel(repositorioDeUsuarios.apelidoEmUso);

  const usuario = repositorioDeUsuarios.criar({
    email: emailNormalizado,
    senhaHash: bcrypt.hashSync(String(senha), 10),
    apelido: apelidoFinal,
  });

  return { token: gerarToken(usuario), usuario: apresentarUsuario(usuario) };
};

export const entrar = ({ email, senha }) => {
  const emailNormalizado = normalizarEmail(email);
  const usuario = repositorioDeUsuarios.porEmail(emailNormalizado);
  const credenciaisInvalidas = erroDeAutenticacao('credenciais_invalidas', 'E-mail ou senha incorretos.');

  if (!usuario || !bcrypt.compareSync(String(senha ?? ''), usuario.senha_hash)) {
    throw credenciaisInvalidas;
  }

  return { token: gerarToken(usuario), usuario: apresentarUsuario(usuario) };
};

export const perfilDoUsuario = (id) => {
  const usuario = repositorioDeUsuarios.porId(id);

  if (!usuario) {
    throw erroDeAutenticacao('usuario_nao_encontrado', 'Conta nao encontrada.');
  }

  return apresentarUsuario(usuario);
};
