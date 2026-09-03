import {useState} from "react";
import type {ChangeEvent, SubmitEvent} from "react";

import { validarPlanilha } from "../services/planilhaService";
import type {ResultadoValidacaoPlanilha} from "../types/planilha";

export function NovoEnvioPage(){
    const [assunto, setAssunto] = useState("");
    const [corpo, setCorpo] = useState("");
    const [arquivo, setArquivo] = useState<File | null>(null);
    const [validacao, setValidacao] = useState<ResultadoValidacaoPlanilha | null>(null);
    const [carregandoPlanilha, setCarregandoPlanilha] = useState(false);
    const [error, setErro] = useState<string | null>(null);

    async function handlePlanilhaChange(event: ChangeEvent<HTMLInputElement>) {
        const arquivoSelecionado = event.target.files?.[0];

        if (!arquivoSelecionado){
            return;
        }

        setArquivo(arquivoSelecionado);
        setValidacao(null);
        setErro(null);
        setCarregandoPlanilha(true);

        try {
            const resultado = await validarPlanilha(arquivoSelecionado);
            setValidacao(resultado);
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

    function handleSubmit(event: SubmitEvent<HTMLFormElement>){event.preventDefault();
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
        if (!validacao || validacao.totalValidos === 0){
            setErro("O arquivo enviado não possui destinatários válidos.");
            return;
        }
        setErro(null);
        console.log({assunto, corpo, arquivo, validacao});
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
                    {validacao && (
                        <div className="validation-result">
                            <div className="validation-summary">
                            <div className="summary-item">
                                <strong>{validacao.totalEncontrados}</strong><span>Encontrados</span>
                            </div>
                            <div className="summary-item">
                                <strong>{validacao.totalValidos}</strong>
                                <span>Válidos</span>
                            </div>

                            <div className="summary-item">
                                <strong>{validacao.totalInvalidos}</strong>
                                <span>Inválidos</span>
                            </div>
                            </div>
                            
                            <div className="invalid-list">
                                {validacao.invalidos.map((destinatario) => (
                                    <div
                                    className="invalid-recipient"
                                    key={`${destinatario.linha}-${destinatario.email}`}
                                    >
                                    <span className="invalid-line">
                                        Linha {destinatario.linha}
                                    </span>

                                    <div className="invalid-data">
                                        <strong>
                                        {destinatario.nome || "Sem nome"}
                                        </strong>

                                        <span>
                                        {destinatario.email || "E-mail não informado"}
                                        </span>
                                    </div>

                                    <span className="invalid-reason">
                                        {destinatario.motivo}
                                    </span>
                                    </div>
                                ))}
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