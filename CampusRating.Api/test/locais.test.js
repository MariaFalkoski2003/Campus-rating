import assert from 'node:assert/strict';
import { once } from 'node:events';
import { after, afterEach, before, test } from 'node:test';
import jwt from 'jsonwebtoken';

process.env.CAMINHO_DO_BANCO = ':memory:';
process.env.SEGREDO_DO_TOKEN = 'campus-rating-testes-locais';

const { criarAplicacao } = await import('../src/aplicacao.js');
const { banco, aplicarEsquema } = await import('../src/banco/conexao.js');

let servidor;
let endereco;
let token;
let usuario;

before(async () => {
  servidor = criarAplicacao().listen(0, '127.0.0.1');
  await once(servidor, 'listening');
  endereco = `http://127.0.0.1:${servidor.address().port}`;

  const resposta = await fetch(`${endereco}/auth/registro`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      email: 'locais@aluno.uricer.edu.br',
      senha: 'senha123',
      confirmacaoSenha: 'senha123',
      apelido: 'Coruja dos Locais 47',
      aceitouRegras: true,
    }),
  });
  assert.equal(resposta.status, 201);
  ({ token, usuario } = await resposta.json());
});

afterEach(() => banco.exec('DELETE FROM relatos'));

after(async () => {
  await new Promise((resolve, reject) => {
    servidor.close((erro) => (erro ? reject(erro) : resolve()));
  });
  banco.close();
});

const consultar = async (credencial = token) => {
  const resposta = await fetch(`${endereco}/locais`, {
    headers: credencial ? { Authorization: `Bearer ${credencial}` } : {},
  });
  return { status: resposta.status, corpo: await resposta.json() };
};

const criarRelato = (localId, texto, status = 'pendente') => banco.prepare(`
  INSERT INTO relatos (local_id, usuario_id, texto, status)
  VALUES (?, ?, ?, ?)
`).run(localId, usuario.id, texto, status);

test('a lista de locais exige autenticação e rejeita tokens inválidos ou expirados', async () => {
  const expirado = jwt.sign({}, process.env.SEGREDO_DO_TOKEN, {
    subject: String(usuario.id),
    expiresIn: -1,
  });

  for (const credencial of [null, 'token-invalido', expirado]) {
    const resposta = await consultar(credencial);
    assert.equal(resposta.status, 401);
    assert.ok(['token_ausente', 'token_invalido'].includes(resposta.corpo.erro.codigo));
    assert.equal(resposta.corpo.locais, undefined);
  }
});

test('lista os quatro locais predefinidos na ordem da interface, sem notas fictícias', async () => {
  const resposta = await consultar();
  assert.equal(resposta.status, 200);
  assert.deepEqual(resposta.corpo.locais.map(local => local.nome), [
    'Biblioteca Central', 'Bloco B - Banheiros', 'Cantina', 'Laboratório 3',
  ]);
  assert.deepEqual(resposta.corpo.locais.map(local => local.categoria), [
    'Biblioteca', 'Banheiros', 'Cantina', 'Salas',
  ]);

  for (const local of resposta.corpo.locais) {
    assert.ok(Number.isInteger(local.id) && local.id > 0);
    assert.ok(local.localizacao);
    assert.ok(local.descricao);
    assert.match(local.imagem, /^campus_.*\.jpg$/);
    assert.equal(local.quantidadeComentarios, 0);
    assert.equal(local.nota, undefined);
    assert.equal(local.rating, undefined);
    assert.equal(local.avaliacoes, undefined);
  }
});

test('os dados exibidos são consultados no banco, não em uma lista fixa da rota', async () => {
  const local = banco.prepare('SELECT * FROM locais WHERE codigo = ?').get('biblioteca-central');

  try {
    banco.prepare('UPDATE locais SET descricao = ? WHERE id = ?').run('Descrição alterada somente neste teste.', local.id);
    const resposta = await consultar();
    assert.equal(resposta.corpo.locais.find(item => item.id === local.id).descricao, 'Descrição alterada somente neste teste.');
  } finally {
    banco.prepare('UPDATE locais SET descricao = ? WHERE id = ?').run(local.descricao, local.id);
  }
});

test('o contador considera todos os relatos originais do local, independentemente do andamento', async () => {
  const locais = (await consultar()).corpo.locais;
  const biblioteca = locais.find(local => local.categoria === 'Biblioteca');
  const cantina = locais.find(local => local.categoria === 'Cantina');

  criarRelato(biblioteca.id, 'Primeiro relato.', 'pendente');
  criarRelato(biblioteca.id, 'Segundo relato.', 'em_analise');
  criarRelato(biblioteca.id, 'Terceiro relato.', 'resolvido');
  criarRelato(cantina.id, 'Relato da cantina.');

  const resposta = await consultar();
  assert.equal(resposta.corpo.locais.find(local => local.id === biblioteca.id).quantidadeComentarios, 3);
  assert.equal(resposta.corpo.locais.find(local => local.id === cantina.id).quantidadeComentarios, 1);
  assert.equal(resposta.corpo.locais.find(local => local.categoria === 'Salas').quantidadeComentarios, 0);
});

test('respostas futuras em outra tabela não são somadas como novos comentários do local', async () => {
  const localId = (await consultar()).corpo.locais[0].id;
  const relatoId = criarRelato(localId, 'Relato original.').lastInsertRowid;

  // Simula a tabela de respostas sem implementar o envio, que pertence a outra etapa.
  banco.exec('CREATE TEMP TABLE respostas (relato_id INTEGER, texto TEXT)');
  try {
    const inserir = banco.prepare('INSERT INTO respostas (relato_id, texto) VALUES (?, ?)');
    inserir.run(relatoId, 'Resposta da instituição.');
    inserir.run(relatoId, 'Retorno do aluno.');
    assert.equal((await consultar()).corpo.locais[0].quantidadeComentarios, 1);
  } finally {
    banco.exec('DROP TABLE respostas');
  }
});

test('reaplicar o esquema preserva contas, relatos e os identificadores dos locais, sem duplicação', async () => {
  const antes = (await consultar()).corpo.locais;
  const relatoId = criarRelato(antes[0].id, 'Relato preservado.').lastInsertRowid;

  aplicarEsquema();
  aplicarEsquema();

  const depois = (await consultar()).corpo.locais;
  assert.deepEqual(depois.map(local => local.id), antes.map(local => local.id));
  assert.equal(depois.length, 4);
  assert.equal(depois[0].quantidadeComentarios, 1);
  assert.equal(banco.prepare('SELECT COUNT(*) AS total FROM usuarios').get().total, 1);
  assert.ok(banco.prepare('SELECT id FROM relatos WHERE id = ?').get(relatoId));
});

test('o banco rejeita relatos sem local válido, sem autor válido ou sem texto', () => {
  const localId = banco.prepare('SELECT id FROM locais LIMIT 1').get().id;
  const inserir = banco.prepare('INSERT INTO relatos (local_id, usuario_id, texto) VALUES (?, ?, ?)');
  assert.throws(() => inserir.run(-1, usuario.id, 'Texto.'), /FOREIGN KEY/);
  assert.throws(() => inserir.run(localId, -1, 'Texto.'), /FOREIGN KEY/);
  assert.throws(() => inserir.run(localId, usuario.id, '   '), /CHECK/);
});

test('não há endpoints de cadastro, edição ou exclusão de locais', async () => {
  const id = (await consultar()).corpo.locais[0].id;
  for (const [metodo, caminho] of [['POST', '/locais'], ['PUT', `/locais/${id}`], ['DELETE', `/locais/${id}`]]) {
    const resposta = await fetch(endereco + caminho, {
      method: metodo,
      headers: { Authorization: `Bearer ${token}` },
    });
    assert.equal(resposta.status, 404);
  }
  assert.equal((await consultar()).corpo.locais.length, 4);
});

test('detalhes de um local retornam comentários e validam autenticação e identificador', async () => {
  const local = (await consultar()).corpo.locais[0];
  criarRelato(local.id, 'Comentário de teste dos detalhes.');

  const resposta = await fetch(`${endereco}/locais/${local.id}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  assert.equal(resposta.status, 200);
  const detalhes = await resposta.json();
  assert.equal(detalhes.local.id, local.id);
  assert.equal(detalhes.local.quantidadeComentarios, 1);
  assert.equal(detalhes.comentarios.length, 1);
  assert.equal(detalhes.comentarios[0].texto, 'Comentário de teste dos detalhes.');
  assert.equal(detalhes.comentarios[0].apelido, usuario.apelido);
  assert.equal(detalhes.comentarios[0].status, 'pendente');
  assert.ok(detalhes.comentarios[0].criadoEm);
  assert.equal(detalhes.comentarios[0].email, undefined);

  assert.equal((await fetch(`${endereco}/locais/${local.id}`)).status, 401);
  for (const [id, status] of [['invalido', 400], ['999999', 404]]) {
    const erro = await fetch(`${endereco}/locais/${id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    assert.equal(erro.status, status);
  }
});
