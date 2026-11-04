import assert from 'node:assert/strict';
import { once } from 'node:events';
import { after, before, test } from 'node:test';
import bcrypt from 'bcryptjs';

// O banco e as configurações abaixo existem apenas no processo de testes.
process.env.CAMINHO_DO_BANCO = ':memory:';
process.env.SEGREDO_DO_TOKEN = 'campus-rating-testes';
process.env.VALIDADE_DO_TOKEN = '7d';
// Uma configuração antiga não pode liberar outro domínio.
process.env.DOMINIOS_PERMITIDOS = 'gmail.com';

const { criarAplicacao } = await import('../src/aplicacao.js');
const { banco } = await import('../src/banco/conexao.js');
const { repositorioDeUsuarios } = await import('../src/banco/repositorioDeUsuarios.js');

let servidor;
let endereco;

before(async () => {
  servidor = criarAplicacao().listen(0, '127.0.0.1');
  await once(servidor, 'listening');
  endereco = `http://127.0.0.1:${servidor.address().port}`;
});

after(async () => {
  await new Promise((resolve, reject) => {
    servidor.close((erro) => (erro ? reject(erro) : resolve()));
  });
  banco.close();
});

const enviar = async (rota, email, campos = {}) => {
  const resposta = await fetch(`${endereco}/auth/${rota}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      email,
      senha: 'senha123',
      confirmacaoSenha: 'senha123',
      apelido: 'Coruja de Teste 47',
      aceitouRegras: true,
      ...campos,
    }),
  });

  return { status: resposta.status, corpo: await resposta.json() };
};

test('cadastro e login aceitam o domínio exato e normalizam maiúsculas e espaços nas pontas', async () => {
  const cadastro = await enviar('registro', '  Maria@ALUNO.URICER.EDU.BR  ');
  assert.equal(cadastro.status, 201);
  assert.equal(cadastro.corpo.usuario.email, 'maria@aluno.uricer.edu.br');
  assert.ok(cadastro.corpo.token);

  const login = await enviar('login', '  MARIA@aluno.uricer.edu.br  ');
  assert.equal(login.status, 200);
  assert.equal(login.corpo.usuario.id, cadastro.corpo.usuario.id);
  assert.ok(login.corpo.token);
});

test('cadastro e login rejeitam outros domínios e tentativas de incluir o domínio no texto', async (t) => {
  const emails = [
    'maria@gmail.com',
    'maria@uricer.edu.br',
    'maria@professor.uricer.edu.br',
    'maria@sub.aluno.uricer.edu.br',
    'maria@outroaluno.uricer.edu.br',
    'maria@aluno.uricer.edu.br.outro.com',
    'aluno.uricer.edu.br@gmail.com',
  ];

  const totalAntes = banco.prepare('SELECT COUNT(*) AS total FROM usuarios').get().total;

  for (const email of emails) {
    await t.test(email, async () => {
      for (const rota of ['registro', 'login']) {
        const resposta = await enviar(rota, email);
        assert.equal(resposta.status, 400, rota);
        assert.equal(resposta.corpo.erro.codigo, 'email_nao_institucional', rota);
        assert.equal(resposta.corpo.erro.campo, 'email', rota);
        assert.equal(resposta.corpo.erro.mensagem, 'Utilize seu e-mail institucional (@aluno.uricer.edu.br).', rota);
      }
    });
  }

  assert.equal(banco.prepare('SELECT COUNT(*) AS total FROM usuarios').get().total, totalAntes);
});

test('cadastro e login rejeitam endereços malformados', async (t) => {
  const emails = [
    '',
    '@aluno.uricer.edu.br',
    'maria@@aluno.uricer.edu.br',
    'maria silva@aluno.uricer.edu.br',
    'maria@aluno.uricer.edu.br\noutra@aluno.uricer.edu.br',
  ];

  for (const email of emails) {
    await t.test(JSON.stringify(email), async () => {
      for (const rota of ['registro', 'login']) {
        const resposta = await enviar(rota, email);
        assert.equal(resposta.status, 400, rota);
        assert.equal(resposta.corpo.erro.codigo, 'email_invalido', rota);
        assert.equal(resposta.corpo.erro.mensagem, 'Utilize seu e-mail institucional (@aluno.uricer.edu.br).', rota);
      }
    });
  }
});

test('uma conta antiga de outro domínio não consegue entrar, mesmo com a senha correta', async () => {
  const usuario = repositorioDeUsuarios.criar({
    email: 'conta.antiga@gmail.com',
    senhaHash: bcrypt.hashSync('senha123', 4),
    apelido: 'Conta Antiga',
  });

  const resposta = await enviar('login', usuario.email);
  assert.equal(resposta.status, 400);
  assert.equal(resposta.corpo.erro.codigo, 'email_nao_institucional');
  assert.ok(repositorioDeUsuarios.porId(usuario.id));
});

test('senhas curtas ou compostas apenas de espaços não criam uma conta', async (t) => {
  const totalAntes = banco.prepare('SELECT COUNT(*) AS total FROM usuarios').get().total;

  for (const senha of ['', '12345', '      ']) {
    await t.test(JSON.stringify(senha), async () => {
      const resposta = await enviar('registro', 'senha.invalida@aluno.uricer.edu.br', {
        senha,
        confirmacaoSenha: senha,
      });
      assert.equal(resposta.status, 400);
      assert.equal(resposta.corpo.erro.codigo, 'senha_curta');
      assert.equal(resposta.corpo.erro.mensagem, 'A senha deve ter pelo menos 6 caracteres.');
      assert.equal(resposta.corpo.erro.campo, 'senha');
    });
  }

  assert.equal(banco.prepare('SELECT COUNT(*) AS total FROM usuarios').get().total, totalAntes);
});

test('confirmação diferente é informada sem salvar o cadastro e permite corrigir e reenviar', async () => {
  const email = 'confirmacao@aluno.uricer.edu.br';
  const resposta = await enviar('registro', email, { confirmacaoSenha: 'outra-senha' });

  assert.equal(resposta.status, 400);
  assert.equal(resposta.corpo.erro.codigo, 'senhas_diferentes');
  assert.equal(resposta.corpo.erro.mensagem, 'As senhas informadas não coincidem.');
  assert.equal(resposta.corpo.erro.campo, 'confirmacaoSenha');
  assert.equal(repositorioDeUsuarios.porEmail(email), undefined);
  assert.equal((await enviar('registro', email)).status, 201);
});

test('senha de seis caracteres permite cadastro e login sem expor ou gravar a senha original', async () => {
  const email = 'senha.minima@aluno.uricer.edu.br';
  const senha = 'abc123';
  const cadastro = await enviar('registro', email, { senha, confirmacaoSenha: senha });

  assert.equal(cadastro.status, 201);
  assert.ok(cadastro.corpo.token);
  assert.equal(cadastro.corpo.usuario.senha, undefined);
  assert.equal(cadastro.corpo.usuario.senha_hash, undefined);

  const usuario = repositorioDeUsuarios.porEmail(email);
  assert.notEqual(usuario.senha_hash, senha);
  assert.ok(bcrypt.compareSync(senha, usuario.senha_hash));

  const login = await enviar('login', email, { senha });
  assert.equal(login.status, 200);
  assert.equal(login.corpo.usuario.id, cadastro.corpo.usuario.id);
});

test('e-mail duplicado é informado sem substituir a conta existente', async () => {
  const email = 'duplicado@aluno.uricer.edu.br';
  const cadastro = await enviar('registro', email);
  assert.equal(cadastro.status, 201);

  const resposta = await enviar('registro', email.toUpperCase(), {
    senha: 'outra-senha',
    confirmacaoSenha: 'outra-senha',
    apelido: 'Apelido que não deve ser salvo',
  });

  assert.equal(resposta.status, 409);
  assert.equal(resposta.corpo.erro.codigo, 'email_ja_cadastrado');
  assert.equal(resposta.corpo.erro.mensagem, 'Já existe uma conta com esse e-mail.');
  assert.equal(resposta.corpo.erro.campo, 'email');
  assert.equal(repositorioDeUsuarios.porEmail(email).apelido, cadastro.corpo.usuario.apelido);
  assert.equal((await enviar('login', email)).status, 200);
});

test('senha errada ou conta inexistente recebe o aviso de credenciais incorretas', async () => {
  const email = 'credenciais@aluno.uricer.edu.br';
  assert.equal((await enviar('registro', email)).status, 201);

  for (const [emailDeLogin, senha] of [
    [email, 'senha-incorreta'],
    ['inexistente@aluno.uricer.edu.br', 'senha123'],
  ]) {
    const resposta = await enviar('login', emailDeLogin, { senha });
    assert.equal(resposta.status, 401);
    assert.equal(resposta.corpo.erro.codigo, 'credenciais_invalidas');
    assert.equal(resposta.corpo.erro.mensagem, 'E-mail ou senha incorretos.');
    assert.equal(resposta.corpo.token, undefined);
  }
});

test('cadastros com o mesmo apelido recebem identificações distintas e persistidas', async () => {
  const apelido = 'Raposa de Teste 28';
  const primeiro = await enviar('registro', 'apelido.um@aluno.uricer.edu.br', { apelido });
  const segundo = await enviar('registro', 'apelido.dois@aluno.uricer.edu.br', { apelido });

  assert.equal(primeiro.status, 201);
  assert.equal(segundo.status, 201);
  assert.equal(primeiro.corpo.usuario.apelido, apelido);
  assert.notEqual(segundo.corpo.usuario.apelido, apelido);
  assert.equal(repositorioDeUsuarios.porId(segundo.corpo.usuario.id).apelido, segundo.corpo.usuario.apelido);
});
