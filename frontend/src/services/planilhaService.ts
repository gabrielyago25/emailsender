import { API_URL } from "./api";
import type { ResultadoValidacaoPlanilha } from "../types/planilha";

export async function validarPlanilha (arquivo: File): Promise<ResultadoValidacaoPlanilha>{
    const formData = new FormData();
    formData.append("arquivo", arquivo);

    const response = await fetch(`${API_URL}/api/planilhas/validar`,{
        method: "POST",
        body: formData,
    });

    if (!response.ok) {
        let mensagem = "Não foi possível validar a planilha.";

        try {
            const erro = await response.json();

            mensagem = erro.mensagem ?? erro.message ?? mensagem;
        } catch {
            mensagem = "Não foi possível validar a planilha.";

        } throw new Error(mensagem);
    }
    return response.json();
}

export async function baixarModeloPlanilha(): Promise<void> {
    const response = await fetch(`${API_URL}/api/planilhas/modelo`);

    if (!response.ok) {
        throw new Error("Não foi possível baixar o modelo.");
    }

    const arquivo = await response.blob();
    const url = URL.createObjectURL(arquivo);
    const link = document.createElement("a");

    link.href = url;
    link.download = "modelo_destinatarios.xlsx";

    document.body.appendChild(link);

    link.click();
    link.remove();

    URL.revokeObjectURL(url);
}