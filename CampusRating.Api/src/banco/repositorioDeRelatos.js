import { banco } from './conexao.js';

const selecionar = `SELECT relatos.id, relatos.local_id AS localId, locais.nome AS localNome,
  usuarios.apelido, relatos.texto, relatos.status, relatos.criado_em AS criadoEm
  FROM relatos JOIN locais ON locais.id = relatos.local_id
  JOIN usuarios ON usuarios.id = relatos.usuario_id`;
const respostas = banco.prepare(`SELECT id, texto, criado_em AS criadoEm FROM respostas
  WHERE relato_id = ? ORDER BY criado_em, id`);

export const repositorioDeRelatos = {
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
