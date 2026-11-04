import { banco } from './conexao.js';

const inserir = banco.prepare(
  `INSERT INTO usuarios (email, senha_hash, apelido, regras_aceitas_em, versao_regras)
   VALUES (@email, @senhaHash, @apelido, @regrasAceitasEm, @versaoRegras)`
);

const buscarPorEmail = banco.prepare('SELECT * FROM usuarios WHERE email = ?');
const buscarPorId = banco.prepare('SELECT * FROM usuarios WHERE id = ?');
const contarPorApelido = banco.prepare('SELECT COUNT(*) AS total FROM usuarios WHERE apelido = ?');

export const repositorioDeUsuarios = {
  criar({ email, senhaHash, apelido, regrasAceitasEm = null, versaoRegras = null }) {
    const resultado = inserir.run({ email, senhaHash, apelido, regrasAceitasEm, versaoRegras });
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

  alterarEmail(id, email) {
    banco.prepare('UPDATE usuarios SET email = ?, versao_token = versao_token + 1 WHERE id = ?').run(email, id);
    return buscarPorId.get(id);
  },

  alterarSenha(id, senhaHash) {
    banco.prepare('UPDATE usuarios SET senha_hash = ?, versao_token = versao_token + 1 WHERE id = ?').run(senhaHash, id);
    return buscarPorId.get(id);
  },

  excluir(id) {
    banco.transaction(() => {
      banco.prepare('DELETE FROM relatos WHERE usuario_id = ?').run(id);
      banco.prepare('DELETE FROM usuarios WHERE id = ?').run(id);
    })();
  },
};
