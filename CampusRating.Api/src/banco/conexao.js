import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import Database from 'better-sqlite3';
import { configuracao } from '../configuracao.js';

const pastaAtual = path.dirname(fileURLToPath(import.meta.url));

fs.mkdirSync(path.dirname(configuracao.caminhoDoBanco), { recursive: true });

export const banco = new Database(configuracao.caminhoDoBanco);

banco.pragma('journal_mode = WAL');
banco.pragma('foreign_keys = ON');

export const aplicarEsquema = () => {
  banco.exec(fs.readFileSync(path.join(pastaAtual, 'esquema.sql'), 'utf8'));
};

aplicarEsquema();
