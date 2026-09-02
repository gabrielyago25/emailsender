import type { Destinatario } from "./destinatario";

export interface ResultadoValidacaoPlanilha{
    total: number;
    destinatarios: Destinatario[];
}