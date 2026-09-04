import { API_URL } from "./api";

import type { CriarEnvioResponse, EnvioJob } from "../types/envio";

export async function criarEnvio(arquivo: File, assunto: string, corpo: string): Promise<CriarEnvioResponse> {
    const formData = new FormData();

    formData.append("arquivo", arquivo);
    formData.append("assunto", assunto);
    formData.append("corpo", corpo);

    const response = await fetch(`${API_URL}/api/envios`,{
        method:"POST",
        body: formData,
    });

    if (!response.ok) {
        let mensagem = "Não foi possível iniciar o envio.";

        try {
            const erro = await response.json();
            mensagem = erro.mensagem ?? erro.message ?? mensagem;
        } catch {
            //Mantém mensagem padrão
        } 
        throw new Error(mensagem)
    }

    return response.json();
}

export async function obterEnvio(id:string): Promise<EnvioJob> {
    const response = await fetch(`${API_URL}/api/envios/${id}`);

    if (!response.ok) {
        throw new Error("Não foi possível consultar o envio");
    }

    return response.json();
}