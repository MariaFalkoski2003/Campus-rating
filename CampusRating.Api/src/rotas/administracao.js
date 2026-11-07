import { Router } from 'express';
import { repositorioDeRelatos } from '../banco/repositorioDeRelatos.js';
import { exigirAutenticacao, exigirAdministrador } from '../middlewares/exigirAutenticacao.js';
import { ErroDaApi, erroDeConflito, erroDeValidacao } from '../erros.js';

export const rotasDeAdministracao = Router();
rotasDeAdministracao.use(exigirAutenticacao, exigirAdministrador);
const estados = ['pendente', 'em_analise', 'resolvido'];

const obterRelato = (valor) => {
  const id = Number(valor);
  if (!/^[1-9]\d*$/.test(valor) || !Number.isSafeInteger(id)) {
    throw erroDeValidacao('relato_invalido', 'Selecione um relato válido.', 'relatoId');
  }
  const relato = repositorioDeRelatos.porId(id);
  if (!relato) throw new ErroDaApi(404, 'relato_nao_encontrado', 'Este relato não foi encontrado.');
  return relato;
};

rotasDeAdministracao.get('/', (req, res, next) => {
  try { res.json({ relatos: repositorioDeRelatos.todos() }); }
  catch (erro) { next(erro); }
});

rotasDeAdministracao.get('/:id', (req, res, next) => {
  try { res.json({ relato: obterRelato(req.params.id) }); }
  catch (erro) { next(erro); }
});

rotasDeAdministracao.patch('/:id/status', (req, res, next) => {
  try {
    const relato = obterRelato(req.params.id);
    const { status, statusAnterior } = req.body ?? {};
    if (!estados.includes(status) || !estados.includes(statusAnterior)) {
      throw erroDeValidacao('status_invalido', 'Selecione um andamento válido.', 'status');
    }
    if (!repositorioDeRelatos.alterarStatus(relato.id, status, statusAnterior)) {
      throw erroDeConflito('relato_alterado', 'O andamento foi alterado por outra pessoa. Reabra o relato antes de salvar.', 'status');
    }
    res.json({ relato: repositorioDeRelatos.porId(relato.id) });
  } catch (erro) { next(erro); }
});

rotasDeAdministracao.post('/:id/respostas', (req, res, next) => {
  try {
    const relato = obterRelato(req.params.id);
    const { texto, chaveEnvio, status, statusAnterior } = req.body ?? {};
    if (typeof texto !== 'string' || !texto.trim() || texto.length > 2000) {
      throw erroDeValidacao('texto_invalido', 'Escreva uma resposta com até 2.000 caracteres.', 'texto');
    }
    if (typeof chaveEnvio !== 'string' || !/^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(chaveEnvio)) {
      throw erroDeValidacao('envio_invalido', 'Identificação do envio inválida. Reabra o relato.', 'chaveEnvio');
    }
    if ((status !== undefined || statusAnterior !== undefined) &&
        (!estados.includes(status) || !estados.includes(statusAnterior))) {
      throw erroDeValidacao('status_invalido', 'Selecione um andamento válido.', 'status');
    }
    // Repetir uma requisição após perda da conexão conserva uma única resposta e um único aviso.
    const chave = `${req.usuarioId}:${chaveEnvio.toLowerCase()}`;
    const existente = repositorioDeRelatos.respostaPorEnvio(chave);
    if (existente) {
      if (existente.relatoId !== relato.id || existente.texto !== texto.trim() ||
          (status !== undefined && relato.status !== status)) {
        throw erroDeConflito('envio_reutilizado', 'Este envio já foi utilizado. Reabra o relato antes de continuar.');
      }
      return res.json({ relato });
    }
    const atualizado = repositorioDeRelatos.responder(relato.id, texto.trim(), chave, status, statusAnterior);
    if (!atualizado) {
      throw erroDeConflito('relato_alterado', 'O andamento foi alterado por outra pessoa. Reabra o relato antes de salvar.', 'status');
    }
    res.status(201).json({ relato: atualizado });
  } catch (erro) { next(erro); }
});
