const conectar_botao = document.getElementById("conectar");
const nome = 


conectar_botao.addEventListener("click", e => {

    const sock = new WebSocket(`ws://127.0.0.1:5015/ws/${document.getElementById("input-nome").value}`); 

    sock.onopen = () => {
        input.value = "jogar"
    }; 

    sock.onmessage = (event) => {
        let message = event.data.toString();

        if(message.startsWith("sudoku:")){
            jogando(message);
        }

        if(message.startsWith("ganhou:")||message.startsWith("perdeu:")){
            document.getElementById("sus").style.display="none";
        }

        resposta.textContent = message;
    };

    sock.onerror = (erro) => {
        console.error("erro");
    };

    sock.onclose = () => {
        setTimeout(ligar, 200);
    };



    const botao = document.getElementById("butao");
    const input = document.getElementById("input");
    const resposta = document.getElementById("resposta");

    botao.addEventListener("click", () => {
        sock.send(input.value);
    });

    function jogando(sudoku){
        document.getElementById("sus").style.display = "block";

        for(let i=1,  j=7; j<sudoku.length;i++, j++){
            if(sudoku[j]!='_'){
                document.getElementById(`${i}`).value = sudoku[j]
                document.getElementById(`${i}`).disabled = true
            }
            else{
                document.getElementById(`${i}`).value = ""
                document.getElementById(`${i}`).disabled = false
            }
        }
    }

    function finalizar(){
        let out = ""
        for(let i=1; i<37;i++){
            out += `${document.getElementById(`${i}`).value}`
        }
        sock.send(out)
    }
});

