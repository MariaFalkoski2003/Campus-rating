import { ErroDaApi } from '../erros.js';

export const rotaNaoEncontrada = (requisicao, resposta) => {
  resposta.status(404).json({
    erro: { codigo: 'rota_nao_encontrada', mensagem: `Rota ${requisicao.method} ${requisicao.path} nao existe.` },
  });
};

export const tratarErros = (erro, requisicao, resposta, proximo) => {
  if (resposta.headersSent) {
    return proximo(erro);
  }

  if (erro instanceof ErroDaApi) {
    return resposta.status(erro.status).json({
      erro: { codigo: erro.codigo, mensagem: erro.message, campo: erro.campo },
    });
  }

  console.error(erro);

  return resposta.status(500).json({
    erro: { codigo: 'erro_interno', mensagem: 'Nao foi possivel concluir a operacao.' },
  });
};
