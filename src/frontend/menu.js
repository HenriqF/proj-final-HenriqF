const infoNome = document.getElementById("nome");
const infoEmail = document.getElementById("email");
const infoFoto = document.getElementById("foto");
const infoPos = document.getElementById("pos");
const infoElo = document.getElementById("elo");
const infoWins = document.getElementById("wins");
const infoDefeats = document.getElementById("defeats");

async function leader() {
    try {
        const table = document.getElementById("table");
        const newRow = table.insertRow();

        const response = await fetch('https://localhost:7185/leaderboard');

        const data = await response.json();

        for(let i=0, j=1;i<data.length;i++){
            if(i!=0 && data[i][1]!=data[i-1][1]){
                j += 1;

                if(i+1!=j) j = i+1;
            }

            table.innerHTML += `
                <tr>
                <th scope="row">#${j}</th>
                <td>${data[i][0]}</td>
                <td>${data[i][1]}</td>
                </tr>
                `
        }
        Carregardados()

    } catch (error) {
        return
        console.log("ERROOO")
        Carregardados()
    }
}

const nome = "pedro";

async function Carregardados(){
    try {

        const response = await fetch(`https://localhost:7185/stats/${nome}`);

        const data = await response.json();

        infoNome.textContent = nome;
        infoEmail.textContent = data.email;
        infoFoto.src = data.foto_link;
        infoPos.textContent = `Rank: ${data.pos_global}`;
        infoElo.textContent = `Elo: ${data.elo}`;
        infoWins.textContent = `Vitórias: ${data.vitorias}`;
        infoDefeats.textContent = `Derrotas: ${data.partidas - data.vitorias}`;


    } catch (error) {
        infoNome.textContent = "dois";
        return;
    }
    
};


leader()