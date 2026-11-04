import { Router } from 'express';
import { repositorioDeRelatos } from '../banco/repositorioDeRelatos.js';
import { exigirAutenticacao } from '../middlewares/exigirAutenticacao.js';
import { ErroDaApi } from '../erros.js';

export const rotasDeRelatos = Router();
rotasDeRelatos.use(exigirAutenticacao);
rotasDeRelatos.get('/meus', (req, res, next) => {
  try { res.json({ relatos: repositorioDeRelatos.meus(req.usuarioId) }); }
  catch (erro) { next(erro); }
});
rotasDeRelatos.get('/:id', (req, res, next) => {
  try {
    const relato = repositorioDeRelatos.meuPorId(Number(req.params.id), req.usuarioId);
    if (!relato) throw new ErroDaApi(404, 'relato_nao_encontrado', 'Este relato não foi encontrado.');
    res.json({ relato });
  } catch (erro) { next(erro); }
});

export const rotasDeAvisos = Router();
rotasDeAvisos.use(exigirAutenticacao);
rotasDeAvisos.get('/', (req, res, next) => {
  try { res.json({ avisos: repositorioDeRelatos.avisos(req.usuarioId) }); }
  catch (erro) { next(erro); }
});
rotasDeAvisos.patch('/:id/lido', (req, res, next) => {
  try {
    if (!repositorioDeRelatos.marcarLido(Number(req.params.id), req.usuarioId)) {
      throw new ErroDaApi(404, 'aviso_nao_encontrado', 'Este aviso não foi encontrado.');
    }
    res.json({ mensagem: 'Aviso marcado como lido.' });
  } catch (erro) { next(erro); }
});
