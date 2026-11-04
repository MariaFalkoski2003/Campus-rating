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
  const esquema = fs.readFileSync(path.join(pastaAtual, 'esquema.sql'), 'utf8');
  banco.transaction(() => {
    banco.exec(esquema);
    const colunas = banco.pragma('table_info(usuarios)').map(coluna => coluna.name);
    for (const [nome, tipo] of [
      ['regras_aceitas_em', 'TEXT'], ['versao_regras', 'TEXT'],
      ['versao_token', 'INTEGER NOT NULL DEFAULT 0'],
    ]) {
      if (!colunas.includes(nome)) banco.exec(`ALTER TABLE usuarios ADD COLUMN ${nome} ${tipo}`);
    }
  })();
};

aplicarEsquema();
