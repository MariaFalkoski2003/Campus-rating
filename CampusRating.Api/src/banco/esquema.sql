CREATE TABLE IF NOT EXISTS usuarios (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  email TEXT NOT NULL UNIQUE COLLATE NOCASE,
  senha_hash TEXT NOT NULL,
  apelido TEXT NOT NULL UNIQUE COLLATE NOCASE,
  criado_em TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS indice_usuarios_email ON usuarios (email);

CREATE TABLE IF NOT EXISTS locais (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  codigo TEXT NOT NULL UNIQUE,
  nome TEXT NOT NULL,
  categoria TEXT NOT NULL,
  localizacao TEXT NOT NULL,
  descricao TEXT NOT NULL,
  imagem TEXT NOT NULL,
  ordem INTEGER NOT NULL DEFAULT 0
);

-- Os locais são predefinidos. Aplicar o esquema novamente não os duplica
-- nem substitui registros que já estejam no banco.
INSERT INTO locais (codigo, nome, categoria, localizacao, descricao, imagem, ordem)
VALUES
  ('biblioteca-central', 'Biblioteca Central', 'Biblioteca', 'Prédio Principal, Térreo',
   'Acervo completo, salas de estudo e computadores disponíveis.', 'campus_library.jpg', 1),
  ('bloco-b-banheiros', 'Bloco B - Banheiros', 'Banheiros', 'Bloco B, Térreo',
   'Banheiros masculinos, femininos e acessíveis.', 'campus_bathrooms.jpg', 2),
  ('cantina', 'Cantina', 'Cantina', 'Prédio de Convivência',
   'Refeições, lanches e bebidas durante o expediente.', 'campus_canteen.jpg', 3),
  ('laboratorio-3', 'Laboratório 3', 'Salas', 'Bloco A, 2º Andar',
   'Laboratório de informática e pesquisas.', 'campus_laboratory.jpg', 4)
ON CONFLICT (codigo) DO NOTHING;

-- Base para contar os comentários originais de cada local.
-- O envio usa o autor autenticado e inicia sempre como pendente.
CREATE TABLE IF NOT EXISTS relatos (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  local_id INTEGER NOT NULL REFERENCES locais (id),
  usuario_id INTEGER NOT NULL REFERENCES usuarios (id),
  texto TEXT NOT NULL CHECK (length(trim(texto)) > 0),
  status TEXT NOT NULL DEFAULT 'pendente'
    CHECK (status IN ('pendente', 'em_analise', 'resolvido')),
  criado_em TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS indice_relatos_local ON relatos (local_id);

CREATE TABLE IF NOT EXISTS respostas (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  relato_id INTEGER NOT NULL REFERENCES relatos (id) ON DELETE CASCADE,
  texto TEXT NOT NULL CHECK (length(trim(texto)) > 0),
  criado_em TEXT NOT NULL DEFAULT (datetime('now'))
);
CREATE INDEX IF NOT EXISTS indice_respostas_relato ON respostas (relato_id);

CREATE TABLE IF NOT EXISTS avisos_lidos (
  usuario_id INTEGER NOT NULL REFERENCES usuarios (id) ON DELETE CASCADE,
  resposta_id INTEGER NOT NULL REFERENCES respostas (id) ON DELETE CASCADE,
  PRIMARY KEY (usuario_id, resposta_id)
);
