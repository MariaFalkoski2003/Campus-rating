import assert from 'node:assert/strict';
import { once } from 'node:events';
import { after, before, test } from 'node:test';

process.env.CAMINHO_DO_BANCO = ':memory:';
process.env.SEGREDO_DO_TOKEN = 'teste-telas-existentes';
const { criarAplicacao } = await import('../src/aplicacao.js');
const { banco, aplicarEsquema } = await import('../src/banco/conexao.js');
let servidor, endereco;
let numero = 0;
before(async () => {
  servidor = criarAplicacao().listen(0, '127.0.0.1');
  await once(servidor, 'listening');
  endereco = `http://127.0.0.1:${servidor.address().port}`;
});
after(async () => {
  await new Promise(resolve => servidor.close(resolve));
  banco.close();
});
const pedir = async (metodo, caminho, corpo, token) => {
  const resposta = await fetch(endereco + caminho, {
    method: metodo,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    ...(corpo === undefined ? {} : { body: JSON.stringify(corpo) }),
  });
  return { status: resposta.status, corpo: await resposta.json() };
};
const cadastrar = async (campos = {}) => pedir('POST', '/auth/registro', {
  email: `telas${++numero}@aluno.uricer.edu.br`, senha: 'senha123', confirmacaoSenha: 'senha123',
  apelido: `Coruja Teste ${numero}`, aceitouRegras: true, ...campos,
});

test('aceite é obrigatório, registrado no banco e preservado ao reaplicar o esquema', async () => {
  for (const valor of [undefined, false, 'true']) {
    const resposta = await cadastrar({ aceitouRegras: valor });
    assert.equal(resposta.status, 400);
    assert.equal(resposta.corpo.erro.codigo, 'regras_nao_aceitas');
  }
  const { corpo } = await cadastrar();
  aplicarEsquema();
  const usuario = banco.prepare('SELECT * FROM usuarios WHERE id = ?').get(corpo.usuario.id);
  assert.ok(usuario.regras_aceitas_em);
  assert.equal(usuario.versao_regras, '1');
});

test('perfil exige senha atual, mantém apelido e invalida tokens após alterações', async () => {
  let { corpo: sessao } = await cadastrar();
  const original = sessao;
  const outro = (await cadastrar()).corpo;
  const rotaEmail = '/auth/eu/email';
  assert.equal((await pedir('PATCH', rotaEmail, { email: outro.usuario.email, senhaAtual: 'errada' }, sessao.token)).status, 400);
  assert.equal((await pedir('PATCH', rotaEmail, { email: outro.usuario.email, senhaAtual: 'senha123' }, sessao.token)).status, 409);
  assert.equal((await pedir('PATCH', rotaEmail, { email: 'a@gmail.com', senhaAtual: 'senha123' }, sessao.token)).status, 400);
  const email = 'email.alterado@aluno.uricer.edu.br';
  const alteracao = await pedir('PATCH', rotaEmail, { email, senhaAtual: 'senha123' }, sessao.token);
  assert.equal(alteracao.status, 200);
  sessao = alteracao.corpo;
  assert.equal(sessao.usuario.apelido, original.usuario.apelido);
  assert.equal(sessao.usuario.email, email);
  assert.equal((await pedir('GET', '/auth/eu', undefined, original.token)).status, 401);
  for (const campos of [
    { senha: '123', confirmacaoSenha: '123' },
    { senha: 'novasenha', confirmacaoSenha: 'outra' },
  ]) assert.equal((await pedir('PATCH', '/auth/eu/senha', { senhaAtual: 'senha123', ...campos }, sessao.token)).status, 400);
  const senha = await pedir('PATCH', '/auth/eu/senha', { senhaAtual: 'senha123', senha: 'novasenha', confirmacaoSenha: 'novasenha' }, sessao.token);
  assert.equal(senha.status, 200);
  assert.equal((await pedir('GET', '/locais', undefined, sessao.token)).status, 401);
  assert.equal((await pedir('POST', '/auth/login', { email, senha: 'senha123' })).status, 401);
  assert.equal((await pedir('POST', '/auth/login', { email, senha: 'novasenha' })).status, 200);
});

test('relatos são persistidos, contados e privados na consulta pessoal; avisos vêm de respostas reais', async () => {
  const aluno = (await cadastrar()).corpo;
  const outro = (await cadastrar()).corpo;
  const locais = await pedir('GET', '/locais', undefined, aluno.token);
  const localId = locais.corpo.locais[0].id;
  const contador = locais.corpo.locais[0].quantidadeComentarios;
  const caminho = `/locais/${localId}/relatos`;
  assert.equal((await pedir('POST', caminho, { texto: 'Teste' })).status, 401);
  for (const texto of ['', '   ', 123, 'x'.repeat(2001)])
    assert.equal((await pedir('POST', caminho, { texto }, aluno.token)).status, 400);
  assert.equal((await pedir('POST', '/locais/999999/relatos', { texto: 'Teste' }, aluno.token)).status, 404);
  const criado = await pedir('POST', caminho, { texto: '  Precisa de manutenção.  ', usuarioId: outro.usuario.id, status: 'resolvido' }, aluno.token);
  assert.equal(criado.status, 201);
  const relato = criado.corpo.relato;
  assert.equal(relato.status, 'pendente');
  assert.equal(relato.apelido, aluno.usuario.apelido);
  assert.equal(relato.texto, 'Precisa de manutenção.');
  const detalhes = await pedir('GET', `/locais/${localId}`, undefined, aluno.token);
  assert.equal(detalhes.corpo.local.quantidadeComentarios, contador + 1);
  assert.equal(detalhes.corpo.comentarios.find(c => c.id === relato.id).email, undefined);
  assert.equal((await pedir('GET', '/relatos/meus', undefined, aluno.token)).corpo.relatos.length, 1);
  assert.equal((await pedir('GET', '/relatos/meus', undefined, outro.token)).corpo.relatos.length, 0);
  assert.equal((await pedir('GET', `/relatos/${relato.id}`, undefined, outro.token)).status, 404);
  assert.deepEqual((await pedir('GET', '/avisos', undefined, aluno.token)).corpo.avisos, []);
  const respostaId = Number(banco.prepare('INSERT INTO respostas (relato_id, texto) VALUES (?, ?)').run(relato.id, 'Vamos verificar.').lastInsertRowid);
  const avisos = (await pedir('GET', '/avisos', undefined, aluno.token)).corpo.avisos;
  assert.equal(avisos.length, 1);
  assert.equal(avisos[0].lida, false);
  assert.equal((await pedir('PATCH', `/avisos/${respostaId}/lido`, {}, outro.token)).status, 404);
  assert.equal((await pedir('PATCH', `/avisos/${respostaId}/lido`, {}, aluno.token)).status, 200);
  assert.equal((await pedir('GET', '/avisos', undefined, aluno.token)).corpo.avisos[0].lida, true);
  assert.equal((await pedir('GET', `/relatos/${relato.id}`, undefined, aluno.token)).corpo.relato.respostas[0].texto, 'Vamos verificar.');
});

test('exclusão confirma senha, remove somente a conta e seus relatos e revoga acesso', async () => {
  const aluno = (await cadastrar()).corpo;
  const outro = (await cadastrar()).corpo;
  const localId = (await pedir('GET', '/locais', undefined, aluno.token)).corpo.locais[0].id;
  const relato = (await pedir('POST', `/locais/${localId}/relatos`, { texto: 'Relato para excluir.' }, aluno.token)).corpo.relato;
  const preservado = (await pedir('POST', `/locais/${localId}/relatos`, { texto: 'Relato preservado.' }, outro.token)).corpo.relato;
  const respostaId = Number(banco.prepare('INSERT INTO respostas (relato_id, texto) VALUES (?, ?)').run(relato.id, 'Resposta.').lastInsertRowid);
  await pedir('PATCH', `/avisos/${respostaId}/lido`, {}, aluno.token);
  assert.equal((await pedir('DELETE', '/auth/eu', { senhaAtual: 'errada' }, aluno.token)).status, 400);
  assert.ok(banco.prepare('SELECT id FROM usuarios WHERE id = ?').get(aluno.usuario.id));
  assert.equal((await pedir('DELETE', '/auth/eu', { senhaAtual: 'senha123' }, aluno.token)).status, 200);
  assert.equal(banco.prepare('SELECT id FROM usuarios WHERE id = ?').get(aluno.usuario.id), undefined);
  assert.equal(banco.prepare('SELECT id FROM relatos WHERE id = ?').get(relato.id), undefined);
  assert.equal(banco.prepare('SELECT id FROM respostas WHERE id = ?').get(respostaId), undefined);
  assert.ok(banco.prepare('SELECT id FROM relatos WHERE id = ?').get(preservado.id));
  assert.equal((await pedir('GET', '/locais', undefined, aluno.token)).status, 401);
  assert.equal((await pedir('GET', '/locais', undefined, outro.token)).status, 200);
  assert.equal(banco.pragma('foreign_key_check').length, 0);
});
