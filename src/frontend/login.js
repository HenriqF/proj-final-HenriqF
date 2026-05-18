const userLogin = document.getElementById("username");
const senhaLogin = document.getElementById("password");
const mensagem = document.getElementById("message");

function mostrar_senha(){
    senhaLogin.type = "text";
};

function esconder_senha(){
    senhaLogin.type = "password";
};

function trocar(){
    document.getElementById("cadastro").click();
};

async function fazerLogin(){
    if(!validarLogin()){
        return;
    }
    
    try {

        const response = await fetch('http://localhost:5269/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ "nome": userLogin.value, "senha": senhaLogin.value })
        });

        const data = await response.json();

        mensagem.innerText = data;

    } catch (error) {
        mensagem.innerText = "Erro ao conectar com o servidor.";
    }
    
};

function validarLogin(){
    let regex = /^[a-zA-Z0-9]*$/;

    if(userLogin.value == "" || senhaLogin.value == ""){
        mensagem.innerText = "Campo em Branco!";
        return false;
    }

    if(!regex.test(userLogin.value)||!regex.test(senhaLogin.value)){
        mensagem.innerText = "Somente letras e números são permitidos!";
        return false;
    }
    return true;
}