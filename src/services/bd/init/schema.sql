CREATE TABLE IF NOT EXISTS usuarios (

    id INT AUTO_INCREMENT PRIMARY KEY ,
    email VARCHAR(255) NOT NULL UNIQUE,
    senha_hash VARBINARY(128) NOT NULL,
    senha_salt VARBINARY(128) NOT NULL,
    nome VARCHAR(255) NOT NULL UNIQUE

) ENGINE=InnoDB;



CREATE TABLE IF NOT EXISTS sudoku_stats (

    user_id INT PRIMARY KEY ,
    user_elo INT DEFAULT 1000,
    qtd_jogos_jogados INT DEFAULT 0,
    qtd_jogos_ganhos INT DEFAULT 0,
    melhor_tempo INT DEFAULT 0,

    FOREIGN KEY (user_id) REFERENCES usuarios(id) ON DELETE CASCADE

) ENGINE=InnoDB;



CREATE TABLE IF NOT EXISTS partidas (

    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,

    tabuleiros VARCHAR(255),
    resultado VARCHAR(255),
    elo_diff INT,

    FOREIGN KEY (user_id) REFERENCES usuarios(id) ON DELETE CASCADE

) ENGINE = InnoDB
