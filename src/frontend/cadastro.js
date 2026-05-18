const userCadastro = document.getElementById("username");
const emailCadastro = document.getElementById("email");
const senhaCadastro = document.getElementById("password");
const senhaConfirm = document.getElementById("password2");
const mensagem = document.getElementById("message");


function mostrar_senha(){
    senhaCadastro.type = "text";
    senhaConfirm.type = "text";
};

function esconder_senha(){
    senhaCadastro.type = "password";
    senhaConfirm.type = "password";
};


function trocar(){
    document.getElementById("login").click();
};


async function fazerSignup(){
    if(!validarCadastro()){
        return;
    }

    try {
        var email = emailCadastro.value.toLowerCase();

        const response = await fetch('http://localhost:5269/cadastro', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ "nome": userCadastro.value, "email": email, "senha": senhaCadastro.value })
        });

        if (!response.ok){
            mensagem.innerText = (await response.text()).slice(1, -1);
            return;
        }

        const data  = await response.json();
        mensagem.innerText = `seja bem vindo, ${data["nome"]}`;

    } catch (error) {
        mensagem.style.color = "red";
        mensagem.innerText = "Erro";
    }
}

function validarCadastro(){
    let regex = /^[a-zA-Z0-9]*$/;
    let regex2 = /^[a-zA-Z0-9@.]*$/;

    if(userCadastro.value == "" || emailCadastro.value == "" || senhaCadastro.value == "" || senhaConfirm.value == ""){
        mensagem.innerText = "Campo em Branco!";
        return false;
    }

    if(!regex.test(userCadastro.value)||!regex2.test(emailCadastro.value)||!regex.test(senhaCadastro.value)){
        mensagem.innerText = "Caracteres especiais não são permitidos!";
        return false;
    }

    if(senhaCadastro.value != senhaConfirm.value){
        mensagem.innerText = "Senhas Diferentes!";
        return false;
    }
    return true;
}