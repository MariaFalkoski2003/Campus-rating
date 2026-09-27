import path from 'node:path';
import { fileURLToPath } from 'node:url';

const raizDoProjeto = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

export const configuracao = {
  porta: Number(process.env.PORTA ?? 3000),
  caminhoDoBanco: process.env.CAMINHO_DO_BANCO ?? path.join(raizDoProjeto, 'dados', 'campusrating.sqlite'),
  segredoDoToken: process.env.SEGREDO_DO_TOKEN ?? 'campus-rating-desenvolvimento',
  validadeDoToken: process.env.VALIDADE_DO_TOKEN ?? '7d',
  dominiosPermitidos: (process.env.DOMINIOS_PERMITIDOS ?? '')
    .split(',')
    .map((dominio) => dominio.trim().toLowerCase())
    .filter((dominio) => dominio.length > 0),
  tamanhoMinimoDaSenha: 6,
};
