import { banco } from './conexao.js';

const selecionar = `SELECT relatos.id, relatos.local_id AS localId, locais.nome AS localNome,
  usuarios.apelido, relatos.texto, relatos.status, relatos.criado_em AS criadoEm
  FROM relatos JOIN locais ON locais.id = relatos.local_id
  JOIN usuarios ON usuarios.id = relatos.usuario_id`;
const respostas = banco.prepare(`SELECT id, texto, criado_em AS criadoEm FROM respostas
  WHERE relato_id = ? ORDER BY criado_em, id`);

export const repositorioDeRelatos = {
  todos() {
    return banco.prepare(`${selecionar} ORDER BY relatos.id DESC`).all();
  },
  porId(id) {
    const relato = banco.prepare(`${selecionar} WHERE relatos.id = ?`).get(id);
    return relato ? { ...relato, respostas: respostas.all(id) } : undefined;
  },
  alterarStatus(id, status, statusAnterior) {
    return banco.prepare('UPDATE relatos SET status = ? WHERE id = ? AND status = ?')
      .run(status, id, statusAnterior).changes > 0;
  },
  respostaPorEnvio(chaveEnvio) {
    return banco.prepare('SELECT relato_id AS relatoId, texto FROM respostas WHERE chave_envio = ?').get(chaveEnvio);
  },
  responder(id, texto, chaveEnvio, status, statusAnterior) {
    return banco.transaction(() => {
      if (status !== undefined && !this.alterarStatus(id, status, statusAnterior)) return undefined;
      banco.prepare('INSERT INTO respostas (relato_id, texto, chave_envio) VALUES (?, ?, ?)').run(id, texto, chaveEnvio);
      return this.porId(id);
    })();
  },
  criar(localId, usuarioId, texto) {
    const { lastInsertRowid } = banco.prepare(
      'INSERT INTO relatos (local_id, usuario_id, texto) VALUES (?, ?, ?)'
    ).run(localId, usuarioId, texto);
    return banco.prepare(`${selecionar} WHERE relatos.id = ?`).get(lastInsertRowid);
  },
  meus(usuarioId) {
    return banco.prepare(`${selecionar} WHERE relatos.usuario_id = ? ORDER BY relatos.id DESC`).all(usuarioId);
  },
  meuPorId(id, usuarioId) {
    const relato = banco.prepare(`${selecionar} WHERE relatos.id = ? AND relatos.usuario_id = ?`).get(id, usuarioId);
    return relato ? { ...relato, respostas: respostas.all(id) } : undefined;
  },
  respostas(id) { return respostas.all(id); },
  avisos(usuarioId) {
    return banco.prepare(`SELECT respostas.id, relatos.id AS relatoId, locais.nome AS localNome,
      respostas.texto, respostas.criado_em AS criadoEm,
      CASE WHEN avisos_lidos.resposta_id IS NULL THEN 0 ELSE 1 END AS lida
      FROM respostas JOIN relatos ON relatos.id = respostas.relato_id
      JOIN locais ON locais.id = relatos.local_id
      LEFT JOIN avisos_lidos ON avisos_lidos.resposta_id = respostas.id AND avisos_lidos.usuario_id = ?
      WHERE relatos.usuario_id = ? ORDER BY respostas.id DESC`).all(usuarioId, usuarioId)
      .map(aviso => ({ ...aviso, lida: Boolean(aviso.lida) }));
  },
  marcarLido(id, usuarioId) {
    const aviso = banco.prepare(`SELECT respostas.id FROM respostas
      JOIN relatos ON relatos.id = respostas.relato_id WHERE respostas.id = ? AND relatos.usuario_id = ?`).get(id, usuarioId);
    if (!aviso) return false;
    banco.prepare('INSERT OR IGNORE INTO avisos_lidos (usuario_id, resposta_id) VALUES (?, ?)').run(usuarioId, id);
    return true;
  },
};
