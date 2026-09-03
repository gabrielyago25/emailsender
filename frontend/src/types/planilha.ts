export interface DestinatarioInvalido{
    linha: number;
    nome: string;
    email: string;
    motivo: string;
}
export interface ResultadoValidacaoPlanilha{
    totalEncontrados: number;
    totalValidos: number;
    totalInvalidos: number;
    invalidos: DestinatarioInvalido[];
}