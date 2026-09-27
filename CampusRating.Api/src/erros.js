export class ErroDaApi extends Error {
  constructor(status, codigo, mensagem, campo = null) {
    super(mensagem);
    this.status = status;
    this.codigo = codigo;
    this.campo = campo;
  }
}

export const erroDeValidacao = (codigo, mensagem, campo) => new ErroDaApi(400, codigo, mensagem, campo);
export const erroDeConflito = (codigo, mensagem, campo) => new ErroDaApi(409, codigo, mensagem, campo);
export const erroDeAutenticacao = (codigo, mensagem) => new ErroDaApi(401, codigo, mensagem);
