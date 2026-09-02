import {useState} from "react";
import type {ChangeEvent} from "react";

import { validarPlanilha } from "../services/planilhaService";
import type { Destinatario } from "../types/destinatario";

export function NovoEnvioPage(){
    const [arquivo, setArquivo] = useState<File | null>(null);

    const [destinatarios, setDestinatarios] = useState<Destinatario[]>([]);

    const [carregando, setCarregando] = useState(false);

    const [error, setErro] = useState<string | null>(null);

    async function handlePlanilhaChange(event: ChangeEvent<HTMLInputElement>) {
        const arquivoSelecionado = event.target.files?.[0];

        if (!arquivoSelecionado){
            return;
        }

        setArquivo(arquivoSelecionado);
        setDestinatarios([]);
        setErro(null);
        setCarregando(true);

        try {
            const resultado = await validarPlanilha(arquivoSelecionado);

            setDestinatarios(resultado.destinatarios);
        } catch (error) {
            if (error instanceof Error) {
                setErro(error.message);
            } else {
                setErro("Ocorreu um erro inesperado.");
            }
        } finally {
            setCarregando(false);
        }
    }

    return (
        <main>
            <h1>EmailSender</h1>
            <h2>Novo Envio</h2>

            <section>
                <label htmlFor="planilha">
                    Planilha de Destinatários
                </label>
                <br/>
                <input id="planilha" type="file" accept=".xlsx" onChange={handlePlanilhaChange}></input>
            </section>

            {arquivo && (<p>Arquivo selecionado: {arquivo.name}</p>)}
            {carregando && (<p>Validando planilha...</p>)}
            {error && (<p>{error}</p>)}
            {destinatarios.length > 0 && (
                <section>
                    <h3>
                        {destinatarios.length} destinatário(s) encontrados
                    </h3>

                    <ul>
                        {destinatarios.map((destinatario) => (
                            <li key={destinatario.email}>
                                {destinatario.nome} —{" "}
                                {destinatario.email}
                            </li>
                            )
                        )}
                    </ul>
                </section>
            )}
        </main>
    );
}