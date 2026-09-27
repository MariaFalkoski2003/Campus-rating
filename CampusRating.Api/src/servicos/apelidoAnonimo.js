const animais = ['Coruja', 'Raposa', 'Arara', 'Lontra', 'Capivara', 'Tucano', 'Lobo', 'Panda'];
const qualidades = ['Serena', 'Curiosa', 'Gentil', 'Atenta', 'Criativa', 'Valente', 'Sabia', 'Amiga'];

const sortear = (lista) => lista[Math.floor(Math.random() * lista.length)];

export const gerarApelido = () => {
  const numero = Math.floor(Math.random() * 90) + 10;
  return `${sortear(animais)} ${sortear(qualidades)} ${numero}`;
};

export const gerarApelidoDisponivel = (apelidoEstaEmUso, tentativas = 50) => {
  for (let tentativa = 0; tentativa < tentativas; tentativa += 1) {
    const apelido = gerarApelido();
    if (!apelidoEstaEmUso(apelido)) {
      return apelido;
    }
  }

  return `${gerarApelido()} ${Date.now().toString().slice(-4)}`;
};
