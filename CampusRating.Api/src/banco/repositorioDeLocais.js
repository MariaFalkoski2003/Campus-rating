import { banco } from './conexao.js';
import { repositorioDeRelatos } from './repositorioDeRelatos.js';

const selecionarLocal = `
  SELECT
    locais.id,
    locais.nome,
    locais.categoria,
    locais.localizacao,
    locais.descricao,
    locais.imagem,
    COUNT(relatos.id) AS quantidadeComentarios
  FROM locais
  LEFT JOIN relatos ON relatos.local_id = locais.id
`;

const listar = banco.prepare(`${selecionarLocal} GROUP BY locais.id ORDER BY locais.ordem, locais.id`);
const buscarPorId = banco.prepare(`${selecionarLocal} WHERE locais.id = ? GROUP BY locais.id`);
const buscarComentarios = banco.prepare(`
  SELECT relatos.id, usuarios.apelido, relatos.texto, relatos.status,
         relatos.criado_em AS criadoEm
  FROM relatos
  INNER JOIN usuarios ON usuarios.id = relatos.usuario_id
  WHERE relatos.local_id = ?
  ORDER BY relatos.criado_em DESC, relatos.id DESC
`);

export const repositorioDeLocais = {
  listar() {
    return listar.all();
  },

  porId(id) {
    return buscarPorId.get(id);
  },

  comentariosDoLocal(id) {
    return buscarComentarios.all(id).map(relato => ({ ...relato, respostas: repositorioDeRelatos.respostas(relato.id) }));
  },
};
