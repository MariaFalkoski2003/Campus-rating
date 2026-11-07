import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { once } from 'node:events';
import { after, before, test } from 'node:test';

process.env.CAMINHO_DO_BANCO = ':memory:';
process.env.SEGREDO_DO_TOKEN = 'teste-telas-existentes';
const { criarAplicacao } = await import('../src/aplicacao.js');
const { banco, aplicarEsquema } = await import('../src/banco/conexao.js');
const { repositorioDeUsuarios } = await import('../src/banco/repositorioDeUsuarios.js');
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

const cadastrarAdministrador = async () => {
  const { corpo: aluno } = await cadastrar();
  repositorioDeUsuarios.definirPapel(aluno.usuario.id, 'administrador');
  return (await pedir('POST', '/auth/login', { email: aluno.usuario.email, senha: 'senha123' })).corpo;
};

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

test('administração exige permissão no servidor e o cadastro não concede privilégios', async () => {
  const aluno = (await cadastrar({ papel: 'administrador' })).corpo;
  assert.equal(aluno.usuario.papel, 'aluno');
  const operacoes = [
    ['GET', '/admin/relatos'], ['GET', '/admin/relatos/1'],
    ['PATCH', '/admin/relatos/1/status', { status: 'resolvido', statusAnterior: 'pendente' }],
    ['POST', '/admin/relatos/1/respostas', { texto: 'Resposta.', chaveEnvio: randomUUID(), status: 'resolvido', statusAnterior: 'pendente' }],
  ];
  for (const [metodo, caminho, corpo] of operacoes) {
    assert.equal((await pedir(metodo, caminho, corpo)).status, 401);
    const negada = await pedir(metodo, caminho, corpo, aluno.token);
    assert.equal(negada.status, 403);
    assert.equal(negada.corpo.erro.codigo, 'acesso_negado');
  }
  repositorioDeUsuarios.definirPapel(aluno.usuario.id, 'administrador');
  assert.equal((await pedir('GET', '/admin/relatos', undefined, aluno.token)).status, 401);
  const admin = (await pedir('POST', '/auth/login', { email: aluno.usuario.email, senha: 'senha123' })).corpo;
  assert.equal(admin.usuario.papel, 'administrador');
  assert.equal((await pedir('GET', '/admin/relatos', undefined, admin.token)).status, 200);
  repositorioDeUsuarios.definirPapel(admin.usuario.id, 'aluno');
  assert.equal((await pedir('GET', '/admin/relatos', undefined, admin.token)).status, 401);
  const removido = (await pedir('POST', '/auth/login', { email: aluno.usuario.email, senha: 'senha123' })).corpo;
  assert.equal((await pedir('GET', '/admin/relatos', undefined, removido.token)).status, 403);
});

test('administração lista relatos de alunos diferentes sem e-mail e valida dados antes de gravar', async () => {
  const admin = await cadastrarAdministrador();
  const alunos = [(await cadastrar()).corpo, (await cadastrar()).corpo];
  const ids = [];
  for (const aluno of alunos) {
    ids.push((await pedir('POST', '/locais/1/relatos', { texto: 'Relato administrativo.' }, aluno.token)).corpo.relato.id);
  }
  const lista = await pedir('GET', '/admin/relatos', undefined, admin.token);
  assert.equal(lista.status, 200);
  for (const id of ids) {
    assert.ok(lista.corpo.relatos.some(relato => relato.id === id));
    const detalhe = await pedir('GET', `/admin/relatos/${id}`, undefined, admin.token);
    assert.equal(detalhe.status, 200);
    assert.equal(detalhe.corpo.relato.email, undefined);
    assert.deepEqual(detalhe.corpo.relato.respostas, []);
  }
  for (const id of ['0', '-1', 'abc', '1.5', '9007199254740992'])
    assert.equal((await pedir('GET', `/admin/relatos/${id}`, undefined, admin.token)).status, 400);
  assert.equal((await pedir('GET', '/admin/relatos/999999', undefined, admin.token)).status, 404);
  const caminho = `/admin/relatos/${ids[0]}`;
  for (const texto of [undefined, '', '   ', 123, 'x'.repeat(2001)])
    assert.equal((await pedir('POST', caminho + '/respostas', { texto, chaveEnvio: randomUUID() }, admin.token)).status, 400);
  assert.equal((await pedir('POST', caminho + '/respostas', { texto: 'Resposta.', chaveEnvio: 'invalida' }, admin.token)).status, 400);
  for (const corpo of [{ status: 'outro', statusAnterior: 'pendente' }, { status: 'resolvido' }])
    assert.equal((await pedir('PATCH', caminho + '/status', corpo, admin.token)).status, 400);
  assert.equal(banco.prepare('SELECT COUNT(*) AS total FROM respostas WHERE relato_id = ?').get(ids[0]).total, 0);
  assert.equal(banco.prepare('SELECT status FROM relatos WHERE id = ?').get(ids[0]).status, 'pendente');
});

test('andamento detecta conflito e respostas repetidas geram um único aviso ao autor', async () => {
  const admin = await cadastrarAdministrador();
  const aluno = (await cadastrar()).corpo;
  const outro = (await cadastrar()).corpo;
  const relato = (await pedir('POST', '/locais/1/relatos', { texto: 'Precisa de reparo.' }, aluno.token)).corpo.relato;
  const caminho = `/admin/relatos/${relato.id}`;
  const analise = await pedir('PATCH', caminho + '/status', { status: 'em_analise', statusAnterior: 'pendente' }, admin.token);
  assert.equal(analise.status, 200);
  assert.equal(analise.corpo.relato.status, 'em_analise');
  const conflito = await pedir('PATCH', caminho + '/status', { status: 'resolvido', statusAnterior: 'pendente' }, admin.token);
  assert.equal(conflito.status, 409);
  assert.equal(conflito.corpo.erro.codigo, 'relato_alterado');
  assert.equal((await pedir('PATCH', caminho + '/status', { status: 'resolvido', statusAnterior: 'em_analise' }, admin.token)).status, 200);
  const corpo = { texto: '  Reparo concluído.  ', chaveEnvio: randomUUID() };
  const envios = await Promise.all([
    pedir('POST', caminho + '/respostas', corpo, admin.token),
    pedir('POST', caminho + '/respostas', corpo, admin.token),
  ]);
  assert.deepEqual(envios.map(envio => envio.status).sort(), [200, 201]);
  assert.equal(envios[0].corpo.relato.respostas.length, 1);
  assert.equal((await pedir('POST', caminho + '/respostas', { ...corpo, texto: 'Outro texto.' }, admin.token)).status, 409);
  const avisos = (await pedir('GET', '/avisos', undefined, aluno.token)).corpo.avisos;
  assert.equal(avisos.length, 1);
  assert.equal(avisos[0].texto, 'Reparo concluído.');
  assert.equal(avisos[0].lida, false);
  assert.deepEqual((await pedir('GET', '/avisos', undefined, outro.token)).corpo.avisos, []);
  const pessoal = (await pedir('GET', `/relatos/${relato.id}`, undefined, aluno.token)).corpo.relato;
  assert.equal(pessoal.status, 'resolvido');
  assert.equal(pessoal.respostas.length, 1);
  aplicarEsquema();
  assert.equal(banco.prepare('SELECT papel FROM usuarios WHERE id = ?').get(admin.usuario.id).papel, 'administrador');
  assert.equal(banco.prepare('SELECT COUNT(*) AS total FROM respostas WHERE relato_id = ?').get(relato.id).total, 1);
  assert.equal(banco.pragma('foreign_key_check').length, 0);
});

test('comentário e andamento são salvos juntos e repetir o envio não duplica a resposta', async () => {
  const admin = await cadastrarAdministrador();
  const aluno = (await cadastrar()).corpo;
  const relato = (await pedir('POST', '/locais/1/relatos', { texto: 'Reparo necessário.' }, aluno.token)).corpo.relato;
  const caminho = `/admin/relatos/${relato.id}`;
  const corpo = { texto: '  Reparo concluído.  ', chaveEnvio: randomUUID(), status: 'resolvido', statusAnterior: 'pendente' };
  const envios = await Promise.all([
    pedir('POST', caminho + '/respostas', corpo, admin.token),
    pedir('POST', caminho + '/respostas', corpo, admin.token),
  ]);
  assert.deepEqual(envios.map(envio => envio.status).sort(), [200, 201]);
  for (const envio of envios) {
    assert.equal(envio.corpo.relato.status, 'resolvido');
    assert.equal(envio.corpo.relato.respostas.length, 1);
    assert.equal(envio.corpo.relato.respostas[0].texto, 'Reparo concluído.');
  }
  assert.equal((await pedir('GET', '/avisos', undefined, aluno.token)).corpo.avisos.length, 1);
  const pessoal = (await pedir('GET', `/relatos/${relato.id}`, undefined, aluno.token)).corpo.relato;
  assert.equal(pessoal.status, 'resolvido');
  assert.equal(pessoal.respostas.length, 1);

  const comentario = await pedir('POST', caminho + '/respostas', {
    ...corpo, texto: 'Acompanhamento do reparo.', chaveEnvio: randomUUID(), statusAnterior: 'resolvido',
  }, admin.token);
  assert.equal(comentario.status, 201);
  assert.equal(comentario.corpo.relato.status, 'resolvido');
  assert.equal(comentario.corpo.relato.respostas.length, 2);

  // Uma repetição atrasada conserva o andamento definido depois por outro administrador.
  assert.equal((await pedir('PATCH', caminho + '/status', { status: 'em_analise', statusAnterior: 'resolvido' }, admin.token)).status, 200);
  assert.equal((await pedir('POST', caminho + '/respostas', corpo, admin.token)).status, 409);
  assert.equal(banco.prepare('SELECT status FROM relatos WHERE id = ?').get(relato.id).status, 'em_analise');
  assert.equal(banco.prepare('SELECT COUNT(*) AS total FROM respostas WHERE relato_id = ?').get(relato.id).total, 2);
});

test('envio conjunto rejeita dados inválidos e conflitos e desfaz o andamento se a resposta falhar', async () => {
  const admin = await cadastrarAdministrador();
  const aluno = (await cadastrar()).corpo;
  const relato = (await pedir('POST', '/locais/1/relatos', { texto: 'Reparo necessário.' }, aluno.token)).corpo.relato;
  const caminho = `/admin/relatos/${relato.id}/respostas`;
  const corpo = { texto: 'Resposta válida.', chaveEnvio: randomUUID(), status: 'resolvido', statusAnterior: 'pendente' };
  for (const campos of [
    { status: 'outro' }, { statusAnterior: 'outro' }, { statusAnterior: undefined },
    { texto: '   ' }, { texto: 'x'.repeat(2001) }, { chaveEnvio: 'invalida' },
  ]) assert.equal((await pedir('POST', caminho, { ...corpo, ...campos }, admin.token)).status, 400);
  const conflito = await pedir('POST', caminho, { ...corpo, statusAnterior: 'em_analise' }, admin.token);
  assert.equal(conflito.status, 409);
  assert.equal(conflito.corpo.erro.codigo, 'relato_alterado');
  assert.equal(banco.prepare('SELECT status FROM relatos WHERE id = ?').get(relato.id).status, 'pendente');
  assert.equal(banco.prepare('SELECT COUNT(*) AS total FROM respostas WHERE relato_id = ?').get(relato.id).total, 0);

  banco.exec(`CREATE TEMP TRIGGER falha_resposta BEFORE INSERT ON respostas
    WHEN NEW.texto = 'Falha simulada' BEGIN SELECT RAISE(ABORT, 'Falha simulada'); END`);
  try {
    assert.equal((await pedir('POST', caminho, { ...corpo, texto: 'Falha simulada' }, admin.token)).status, 500);
    assert.equal(banco.prepare('SELECT status FROM relatos WHERE id = ?').get(relato.id).status, 'pendente');
    assert.equal(banco.prepare('SELECT COUNT(*) AS total FROM respostas WHERE relato_id = ?').get(relato.id).total, 0);
  } finally {
    banco.exec('DROP TRIGGER falha_resposta');
  }
  // Após a falha, a mesma identificação ainda permite concluir o envio.
  assert.equal((await pedir('POST', caminho, corpo, admin.token)).status, 201);
});
