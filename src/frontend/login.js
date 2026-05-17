function trocar(n){
    let log = document.getElementById("entras");
    let cada = document.getElementById("cadas");

    if(n==1){
        log.style.display = "none";
        cada.style.display = "block";
    }

    if(n==2){
        log.style.display = "block";
        cada.style.display = "none";
    }
}

async function fazerLogin() {
        const user = document.getElementById('username').value;
        const pass = document.getElementById('password').value;
        const messageDiv = document.getElementById('message');

        try {
            const response = await fetch('https://localhost:7000/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ Username: user, Password: pass })
            });

            const data = await response.json();

            if (response.ok) {
                messageDiv.style.color = "green";
                messageDiv.innerText = "Sucesso! Redirecionando...";
                localStorage.setItem('token', data.token); // Salva o token para uso futuro
            } else {
                messageDiv.style.color = "red";
                messageDiv.innerText = data.message;
            }
        } catch (error) {
            messageDiv.style.color = "red";
            messageDiv.innerText = "Erro ao conectar com o servidor.";
        }
    }