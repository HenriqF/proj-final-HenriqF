let sock;

sock = new WebSocket(`ws://127.0.0.1:5015/ws`); 

sock.onopen = () => {
    input.value = "conectado"
}; 

sock.onmessage = (event) => {
    let message = event.data.toString();

    if(message=="JATEM"){
        mensagem2.style.color = "red";
        mensagem2.innerText = "Usuário ou Email já existentes!";
    }

    if(message=="SUCESSO"){
        mensagem2.style.color = "green";
        mensagem2.innerText = "CRIADO COM SUCESSO!";
    }

    if(message=="CREDINV"){
        mensagem.style.color = "red";
        mensagem.innerText = "Credenciais Inválidas!";
    }

    if(message.startsWith("SUCESSO")){
        let dados = message.split(",");

        localStorage.setItem("user", dados[1]);
        localStorage.setItem("email", dados[2]);
        localStorage.setItem("jwt_token", dados[3]);

        mensagem.style.color = "green";
        mensagem.innerText = `user: ${localStorage.getItem("user")}
        email: ${localStorage.getItem("email")}
        jwt: ${localStorage.getItem("jwt_token")}`;

        mensagem2.style.color = "green";
        mensagem2.innerText = `user: ${localStorage.getItem("user")}
        email: ${localStorage.getItem("email")}
        jwt: ${localStorage.getItem("jwt_token")}`;
    }

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



//LOGIN & SIGN UP
function trocar(n){
    let log = document.getElementById("entras");
    let cada = document.getElementById("cadas");

    if(n==1){
        log.style.display = "none";
        cada.style.display = "block";
        mensagem.innerText = "";
    };

    if(n==2){
        log.style.display = "block";
        cada.style.display = "none";
        mensagem2.innerText = "";
    };
};

const userLogin = document.getElementById("username");
const senhaLogin = document.getElementById("password");
const userCadastro = document.getElementById("username2");
const emailCadastro = document.getElementById("email");
const senhaCadastro = document.getElementById("password2");
const senhaConfirm = document.getElementById("password3");
const mensagem = document.getElementById("message");
const mensagem2 = document.getElementById("message2");

async function fazerLogin(){
    /* document.getElementById("sudoku").click(); */
    if(userLogin.value == "" || senhaLogin.value == ""){
        mensagem.style.color = "red";
        mensagem.innerText = "Campo em Branco!";
    }else if(!validarLogin()){
        mensagem.style.color = "red";
        mensagem.innerText = "Caracteres especiais não são permitidos!";
    }else{
        sock.send(`login,${userLogin.value},${senhaLogin.value}`);
    }
};

async function fazerSignup(){
    if(userCadastro.value == "" || emailCadastro.value == "" || senhaCadastro.value == "" || senhaConfirm.value == ""){
        mensagem2.style.color = "red";
        mensagem2.innerText = "Campo em Branco!";
    }else if(!validarCadastro()){
        mensagem2.style.color = "red";
        mensagem2.innerText = "Caracteres especiais não são permitidos!";
    }else if(senhaCadastro.value != senhaConfirm.value){
        mensagem2.style.color = "red";
        mensagem2.innerText = "Senhas Diferentes!";
    }else{
        let email = emailCadastro.value.toLowerCase();

        sock.send(`cadastro,${userCadastro.value},${email},${senhaCadastro.value}`);
    };
};

function validarCadastro(){
    let regex = /^[a-zA-Z0-9]*$/;
    let regex2 = /^[a-zA-Z0-9@.]*$/;

    if(!regex.test(userCadastro.value)||!regex2.test(emailCadastro.value)||!regex.test(senhaCadastro.value)){
        return false;
    }
    return true;
}

function validarLogin(){
    let regex = /^[a-zA-Z0-9]*$/;

    if(!regex.test(userLogin.value)||!regex.test(senhaLogin.value)){
        return false;
    }
    return true;
}

//FRONTEND

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
