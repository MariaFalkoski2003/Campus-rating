import { banco } from './conexao.js';
import { repositorioDeUsuarios } from './repositorioDeUsuarios.js';

const [email, opcao, ...extras] = process.argv.slice(2);
try {
  if (!email || (opcao !== undefined && opcao !== '--remover') || extras.length > 0) {
    throw new Error('Uso: npm run administrador -- email@aluno.uricer.edu.br [--remover]');
  }
  const usuario = repositorioDeUsuarios.porEmail(email.trim().toLowerCase());
  if (!usuario) throw new Error('Conta não encontrada. Cadastre a conta no aplicativo primeiro.');
  const papel = opcao === '--remover' ? 'aluno' : 'administrador';
  repositorioDeUsuarios.definirPapel(usuario.id, papel);
  console.log(`Perfil definido como ${papel}. Entre novamente no aplicativo.`);
} catch (erro) {
  console.error(erro.message);
  process.exitCode = 1;
} finally {
  banco.close();
}
