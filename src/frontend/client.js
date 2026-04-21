let sock;
function ligar(){
    sock = new WebSocket("ws://localhost:5015/ws"); 

    sock.onopen = () => {
        input.value = "conectado"
    }; 

    sock.onmessage = (event) => {
        resposta.textContent = event.data.toString();
    };

    sock.onerror = (erro) => {
        console.error("erro");
    };

    sock.onclose = () => {
        setTimeout(ligar, 200);
    };
}
ligar();



const botao = document.getElementById("butao");
const input = document.getElementById("input");
const resposta = document.getElementById("resposta");

botao.addEventListener("click", () => {
    sock.send(input.value);
});