import { Router } from 'express';
import { repositorioDeLocais } from '../banco/repositorioDeLocais.js';
import { exigirAutenticacao } from '../middlewares/exigirAutenticacao.js';
import { ErroDaApi, erroDeValidacao } from '../erros.js';
import { repositorioDeRelatos } from '../banco/repositorioDeRelatos.js';

export const rotasDeLocais = Router();

rotasDeLocais.post('/:id/relatos', exigirAutenticacao, (requisicao, resposta, proximo) => {
  try {
    const localId = Number(requisicao.params.id);
    if (!/^[1-9]\d*$/.test(requisicao.params.id) || !Number.isSafeInteger(localId)) {
      throw erroDeValidacao('local_invalido', 'Selecione um local válido.', 'localId');
    }
    if (!repositorioDeLocais.porId(localId)) {
      throw new ErroDaApi(404, 'local_nao_encontrado', 'Este local não foi encontrado.');
    }
    const texto = requisicao.body?.texto;
    if (typeof texto !== 'string' || !texto.trim() || texto.length > 2000) {
      throw erroDeValidacao('texto_invalido', 'Escreva um relato com até 2.000 caracteres.', 'texto');
    }
    const relato = repositorioDeRelatos.criar(localId, requisicao.usuarioId, texto.trim());
    resposta.status(201).json({ relato });
  } catch (erro) { proximo(erro); }
});

rotasDeLocais.get('/', exigirAutenticacao, (requisicao, resposta, proximo) => {
  try {
    resposta.json({ locais: repositorioDeLocais.listar() });
  } catch (erro) {
    proximo(erro);
  }
});

rotasDeLocais.get('/:id', exigirAutenticacao, (requisicao, resposta, proximo) => {
  try {
    const id = Number(requisicao.params.id);
    if (!/^[1-9]\d*$/.test(requisicao.params.id) || !Number.isSafeInteger(id)) {
      throw erroDeValidacao('local_invalido', 'Selecione um local válido.', 'localId');
    }

    const local = repositorioDeLocais.porId(id);
    if (!local) {
      throw new ErroDaApi(404, 'local_nao_encontrado', 'Este local não foi encontrado. Volte à lista de locais.');
    }

    resposta.json({ local, comentarios: repositorioDeLocais.comentariosDoLocal(id) });
  } catch (erro) {
    proximo(erro);
  }
});
