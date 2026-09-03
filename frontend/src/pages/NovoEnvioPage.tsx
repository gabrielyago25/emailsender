import {useState} from "react";
import type {ChangeEvent, FormEvent} from "react";

import { validarPlanilha } from "../services/planilhaService";
import type { Destinatario } from "../types/destinatario";

export function NovoEnvioPage(){
    const [assunto, setAssunto] = useState("");
    const [corpo, setCorpo] = useState("");
    const [arquivo, setArquivo] = useState<File | null>(null);
    const [destinatarios, setDestinatarios] = useState<Destinatario[]>([]);
    const [carregandoPlanilha, setCarregandoPlanilha] = useState(false);
    const [error, setErro] = useState<string | null>(null);

    async function handlePlanilhaChange(event: ChangeEvent<HTMLInputElement>) {
        const arquivoSelecionado = event.target.files?.[0];

        if (!arquivoSelecionado){
            return;
        }

        setArquivo(arquivoSelecionado);
        setDestinatarios([]);
        setErro(null);
        setCarregandoPlanilha(true);

        try {
            const resultado = await validarPlanilha(arquivoSelecionado);
            setDestinatarios(resultado.destinatarios);
        } catch (error) {
            setArquivo(null);

            if (error instanceof Error) {
                setErro(error.message);
            } else {
                setErro("Ocorreu um erro inesperado.");
            }
        } finally {
            setCarregandoPlanilha(false);
        }
    }

    function handleSubmit(event: FormEvent<HTMLFormElement>){event.preventDefault();
        if (!assunto.trim()){
            setErro("Informe o assunto do e-mail.");
            return;
        }
        if (!corpo.trim()){
            setErro("Informe o corpo do e-mail.");
            return;
        }
        if (!arquivo){
            setErro("Selecione um arquivo.");
            return;
        }
        if (destinatarios.length === 0){
            setErro("O arquivo enviado não possui destinatários válidos.");
            return;
        }
        setErro(null);
        console.log({assunto, corpo, arquivo, destinatarios});
    }


    return (
        <main className="container">
            <header className="page-header">
                <div>
                    <h1>EmailSender</h1>
                </div>
            </header>
            <form className="email-form" onSubmit={handleSubmit}>
                <section className="form-section">
                    <h2>Novo Envio</h2>
                    <div className="form-group">
                        <label htmlFor="assunto">
                            Assunto
                        </label>
                        <input id="assunto" type="text" value={assunto} onChange={(event) => setAssunto(event.target.value)} placeholder="Digite o assunto do e-mail" maxLength={200}></input>
                        <span className="character-count">{assunto.length}/200</span>
                    </div>
                    <div className="form-group">
                        <label htmlFor="corpo">Corpo do e-mail</label>
                        <textarea id="corpo" value={corpo} onChange={(event) => setCorpo(event.target.value)} placeholder="Digite a mensagem que será enviada" rows={20}></textarea>
                    </div>                    
                </section>
                <section className="form-section">
                    <div className="section-title">
                        <div>
                            <h2>Destinatários</h2>
                            <p>Importe uma planilha no formato .XLSX</p>
                        </div>
                    </div>
                    <div className="upload-area">
                        <label htmlFor="planilha" className="upload-button">Selecionar arquivo</label>
                        <input id="planilha" className="file-input" type="file" accept=".xlsx" onChange={handlePlanilhaChange}></input>
                        {arquivo ? (<span className="file-name">{arquivo.name}</span>) : (<span className="file-hint">Nenhum arquivo selecioando</span>)}
                    </div>
                    {destinatarios.length > 0 && (
                        <div className="recipients-card">
                            <div className="recipients-summary">
                                <strong>{destinatarios.length}</strong>
                                <span>destinatário{destinatarios.length !== 1? "s": ""}{" "}válido{destinatarios.length !== 1? "s": ""}</span>
                            </div>
                            <div className="recipients-list">
                                {destinatarios.map((destinatario) => (
                                    <div className="recipient" key={destinatario.email}>
                                        <strong>{destinatario.nome || "Sem nome"}</strong>
                                        <span>{destinatario.email}</span>
                                    </div>
                                    )
                                )}
                            </div>
                        </div>
                    )}
                </section>
                {error && (
                    <div className="error-message">
                        {error}
                    </div>
                )}
                <footer className="form-actions">
                    <button type="submit" className="primary-button">Revisar envio</button>
                </footer>
            </form>
        </main>
    );
}