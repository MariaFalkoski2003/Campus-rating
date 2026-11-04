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
    throw erroDeValidacao(
      'email_invalido',
      `Utilize seu e-mail institucional (@${configuracao.dominioInstitucional}).`,
      'email'
    );
  }

  const dominio = email.split('@')[1];

  if (dominio !== configuracao.dominioInstitucional) {
    throw erroDeValidacao(
      'email_nao_institucional',
      `Utilize seu e-mail institucional (@${configuracao.dominioInstitucional}).`,
      'email'
    );
  }
};

const validarSenha = (senha) => {
  const valor = String(senha ?? '');

  if (valor.trim().length === 0 || valor.length < configuracao.tamanhoMinimoDaSenha) {
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

export const registrar = ({ email, senha, confirmacaoSenha, apelido, aceitouRegras }) => {
  const emailNormalizado = normalizarEmail(email);

  validarEmail(emailNormalizado);
  validarSenha(senha);

  if (confirmacaoSenha !== undefined && senha !== confirmacaoSenha) {
    throw erroDeValidacao('senhas_diferentes', 'As senhas informadas não coincidem.', 'confirmacaoSenha');
  }

  if (aceitouRegras !== true) {
    throw erroDeValidacao('regras_nao_aceitas', 'Leia e aceite as regras da plataforma para criar sua conta.', 'aceitouRegras');
  }

  if (repositorioDeUsuarios.porEmail(emailNormalizado)) {
    throw erroDeConflito('email_ja_cadastrado', 'Já existe uma conta com esse e-mail.', 'email');
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
    regrasAceitasEm: new Date().toISOString(),
    versaoRegras: '1',
  });

  return { token: gerarToken(usuario), usuario: apresentarUsuario(usuario) };
};

export const entrar = ({ email, senha }) => {
  const emailNormalizado = normalizarEmail(email);

  validarEmail(emailNormalizado);

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

const conferirSenhaAtual = (id, senhaAtual) => {
  const usuario = repositorioDeUsuarios.porId(id);
  if (!usuario) throw erroDeAutenticacao('usuario_nao_encontrado', 'Conta não encontrada.');
  if (typeof senhaAtual !== 'string' || !bcrypt.compareSync(senhaAtual, usuario.senha_hash)) {
    throw erroDeValidacao('senha_atual_incorreta', 'A senha atual está incorreta.', 'senhaAtual');
  }
  return usuario;
};

export const alterarEmail = (id, { email, senhaAtual }) => {
  conferirSenhaAtual(id, senhaAtual);
  const normalizado = normalizarEmail(email);
  validarEmail(normalizado);
  const existente = repositorioDeUsuarios.porEmail(normalizado);
  if (existente && existente.id !== id) {
    throw erroDeConflito('email_ja_cadastrado', 'Já existe uma conta com esse e-mail.', 'email');
  }
  const usuario = repositorioDeUsuarios.alterarEmail(id, normalizado);
  return { token: gerarToken(usuario), usuario: apresentarUsuario(usuario) };
};

export const alterarSenha = (id, { senhaAtual, senha, confirmacaoSenha }) => {
  conferirSenhaAtual(id, senhaAtual);
  if (typeof senha !== 'string') throw erroDeValidacao('senha_invalida', 'Digite a nova senha.', 'senha');
  validarSenha(senha);
  if (senha !== confirmacaoSenha) {
    throw erroDeValidacao('senhas_diferentes', 'As senhas informadas não coincidem.', 'confirmacaoSenha');
  }
  const usuario = repositorioDeUsuarios.alterarSenha(id, bcrypt.hashSync(senha, 10));
  return { token: gerarToken(usuario), usuario: apresentarUsuario(usuario) };
};

export const excluirConta = (id, { senhaAtual }) => {
  conferirSenhaAtual(id, senhaAtual);
  repositorioDeUsuarios.excluir(id);
  return { mensagem: 'Sua conta e seus relatos foram excluídos.' };
};
