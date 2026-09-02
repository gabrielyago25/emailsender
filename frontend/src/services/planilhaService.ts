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

        } throw new Error(mensagem);
    }
    return response.json();
}