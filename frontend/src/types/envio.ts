export type StatusOperacaoEnvio = 
| "Pendente"
| "EmAndamento"
| "Concluido"
| "Falhou"
| "Cancelado";

export type StatusEnvio = 
| "Enviando"
| "Enviado"
| "Falha"
| "Aguardando";

export interface FalhaEnvio {
    nome: string;
    email: string;
    erro: string;
}

export interface CriarEnvioResponse {
    id: string;
    status: StatusOperacaoEnvio;
    total: number;
}

export interface EnvioJob {
    id: string;
    status: StatusOperacaoEnvio;
    etapaAtual: StatusEnvio | null

    total: number;
    processados: number;
    enviados: number;
    falhas: number;
    percentual: number;

    segundosRestantes: number | null;
    destinatarioAtual: string | null;

    detalhesFalhas: FalhaEnvio[];
    errro: string | null;

    criadoEm: string;
    iniciadoEm: string | null;
    finalizadoEm: string | null;
}