import DOMPurify from "dompurify"

interface RevisaoEnvioProps {
    assunto: string,
    corpo: string,
    nomeArquivo: string,
    totalValidos: number,
    totalInvalidos: number,
    confirmando: boolean,
    onVoltar: () => void;
    onConfirmar: () => void;

}

export function RevisaoEnvio({
    assunto,
    corpo,
    nomeArquivo,
    totalValidos,
    totalInvalidos,
    confirmando,
    onVoltar,
    onConfirmar,
}: RevisaoEnvioProps) {
    return (
        <main className="container">
            <header className="page-header">
                <h1>
                    Revisar envio
                </h1>
                <p>Confirma as informações antes de iniciar o envio.</p>
            </header>
            <div className="email-form">
                <section className="form-section review-section">
                    <div className="review-item">
                        <span>Assunto</span>
                        <strong>{assunto}</strong>
                    </div>

                    <div className="review-item">
                        <span>Mensagem</span>
                        <div className="review-body" dangerouslySetInnerHTML={{__html: DOMPurify.sanitize(corpo),}}>
                        </div>
                    </div>
                </section>
                <section className="form-section">
                    <h2>Destinatários</h2>
                    <div className="review-summary">
                        <div className="review-item">
                            <span>Planilha</span>
                            {nomeArquivo}
                        </div>

                        <div className="review-item">
                            <span>Destinatários que receberão o e-mail</span>
                            <strong>{totalValidos}</strong>
                        </div>

                        {totalInvalidos > 0 &&(
                            <div className="review-item">
                                <span>Registros ignorados</span>
                                <strong>{totalInvalidos}</strong>
                            </div>
                        )}
                    </div>
                </section>
                <div className="review-warning">
                    Após confirmar, o envio será iniciado e os e-mails serão processados individualmente.
                </div>
                <footer className="review-actions">
                    <button type="button" className="secondary-button" onClick={onVoltar} disabled={confirmando}>Voltar</button>
                    <button type="button" className="primary-button" onClick={onConfirmar} disabled={confirmando}>Enviar</button>
                </footer>
            </div>
        </main>
    );
}