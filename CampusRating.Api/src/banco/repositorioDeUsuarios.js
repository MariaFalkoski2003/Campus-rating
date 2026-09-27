import { banco } from './conexao.js';

const inserir = banco.prepare(
  'INSERT INTO usuarios (email, senha_hash, apelido) VALUES (@email, @senhaHash, @apelido)'
);

const buscarPorEmail = banco.prepare('SELECT * FROM usuarios WHERE email = ?');
const buscarPorId = banco.prepare('SELECT * FROM usuarios WHERE id = ?');
const contarPorApelido = banco.prepare('SELECT COUNT(*) AS total FROM usuarios WHERE apelido = ?');

export const repositorioDeUsuarios = {
  criar({ email, senhaHash, apelido }) {
    const resultado = inserir.run({ email, senhaHash, apelido });
    return buscarPorId.get(resultado.lastInsertRowid);
  },

  porEmail(email) {
    return buscarPorEmail.get(email);
  },

  porId(id) {
    return buscarPorId.get(id);
  },

  apelidoEmUso(apelido) {
    return contarPorApelido.get(apelido).total > 0;
  },
};
