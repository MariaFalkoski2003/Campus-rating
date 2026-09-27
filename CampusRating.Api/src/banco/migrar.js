import { aplicarEsquema } from './conexao.js';
import { configuracao } from '../configuracao.js';

aplicarEsquema();

console.log(`Banco pronto em ${configuracao.caminhoDoBanco}`);
