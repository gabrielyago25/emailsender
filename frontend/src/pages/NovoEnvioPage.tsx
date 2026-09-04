import { useState } from "react";
import type { ChangeEvent, SubmitEvent } from "react";

import { validarPlanilha } from "../services/planilhaService";
import { criarEnvio } from "../services/envioService";

import type { ResultadoValidacaoPlanilha } from "../types/planilha";

import { RevisaoEnvio } from "../components/RevisaoEnvio";
import { AcompanhamentoEnvio } from "../components/AcompanhamentoEnvio";

export function NovoEnvioPage() {
  // Dados do formulário
  const [assunto, setAssunto] = useState("");
  const [corpo, setCorpo] = useState("");
  const [arquivo, setArquivo] = useState<File | null>(null);

  // Validação da planilha
  const [validacao, setValidacao] =
    useState<ResultadoValidacaoPlanilha | null>(null);

  const [carregandoPlanilha, setCarregandoPlanilha] =
    useState(false);

  // Fluxo da aplicação
  const [revisando, setRevisando] = useState(false);
  const [iniciandoEnvio, setIniciandoEnvio] = useState(false);
  const [jobId, setJobId] = useState<string | null>(null);

  // Mensagens de erro
  const [erro, setErro] = useState<string | null>(null);

  async function handlePlanilhaChange(
    event: ChangeEvent<HTMLInputElement>
  ) {
    const arquivoSelecionado = event.target.files?.[0];

    if (!arquivoSelecionado) {
      return;
    }

    setArquivo(arquivoSelecionado);
    setValidacao(null);
    setErro(null);
    setCarregandoPlanilha(true);

    try {
      const resultado =
        await validarPlanilha(arquivoSelecionado);

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

  function handleSubmit(
    event: SubmitEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    if (!assunto.trim()) {
      setErro("Informe o assunto do e-mail.");
      return;
    }

    if (!corpo.trim()) {
      setErro("Informe o corpo do e-mail.");
      return;
    }

    if (!arquivo) {
      setErro("Selecione um arquivo.");
      return;
    }

    if (!validacao || validacao.totalValidos === 0) {
      setErro(
        "O arquivo enviado não possui destinatários válidos."
      );
      return;
    }

    setErro(null);
    setRevisando(true);
  }

  async function handleConfirmarEnvio() {
    if (!arquivo) {
      return;
    }

    setErro(null);
    setIniciandoEnvio(true);

    try {
      const resultado = await criarEnvio(
        arquivo,
        assunto.trim(),
        corpo.trim()
      );

      setJobId(resultado.id);
      setRevisando(false);
    } catch (error) {
      if (error instanceof Error) {
        setErro(error.message);
      } else {
        setErro("Não foi possível iniciar o envio.");
      }

      // Volta ao formulário para exibir o erro.
      setRevisando(false);
    } finally {
      setIniciandoEnvio(false);
    }
  }

  function handleNovoEnvio() {
    setJobId(null);
    setRevisando(false);

    setAssunto("");
    setCorpo("");
    setArquivo(null);
    setValidacao(null);

    setErro(null);
  }

  // Acompanhamento do envio
  if (jobId) {
    return (
      <AcompanhamentoEnvio
        jobId={jobId}
        onNovoEnvio={handleNovoEnvio}
      />
    );
  }

  // Revisão antes do envio
  if (revisando && arquivo && validacao) {
    return (
      <RevisaoEnvio
        assunto={assunto}
        corpo={corpo}
        nomeArquivo={arquivo.name}
        totalValidos={validacao.totalValidos}
        totalInvalidos={validacao.totalInvalidos}
        onVoltar={() => setRevisando(false)}
        onConfirmar={handleConfirmarEnvio}
        confirmando={iniciandoEnvio}
      />
    );
  }

  // Formulário de novo envio
  return (
    <main className="container">
      <header className="page-header">
        <h1>EmailSender</h1>
      </header>

      <form
        className="email-form"
        onSubmit={handleSubmit}
      >
        {/* Mensagem */}
        <section className="form-section">
          <h2>Novo Envio</h2>

          <div className="form-group">
            <label htmlFor="assunto">
              Assunto
            </label>

            <input
              id="assunto"
              type="text"
              value={assunto}
              onChange={(event) =>
                setAssunto(event.target.value)
              }
              placeholder="Digite o assunto do e-mail"
              maxLength={200}
            />

            <span className="character-count">
              {assunto.length}/200
            </span>
          </div>

          <div className="form-group">
            <label htmlFor="corpo">
              Corpo do e-mail
            </label>

            <textarea
              id="corpo"
              value={corpo}
              onChange={(event) =>
                setCorpo(event.target.value)
              }
              placeholder="Digite a mensagem que será enviada"
              rows={20}
            />
          </div>
        </section>

        {/* Destinatários */}
        <section className="form-section">
          <div className="section-title">
            <h2>Destinatários</h2>

            <p>
              Importe uma planilha no formato .XLSX
            </p>
          </div>

          {/* Upload */}
          <div className="upload-area">
            <label
              htmlFor="planilha"
              className="upload-button"
            >
              Selecionar arquivo
            </label>

            <input
              id="planilha"
              className="file-input"
              type="file"
              accept=".xlsx"
              onChange={handlePlanilhaChange}
            />

            {arquivo ? (
              <span className="file-name">
                {arquivo.name}
              </span>
            ) : (
              <span className="file-hint">
                Nenhum arquivo selecionado
              </span>
            )}
          </div>

          {/* Carregamento */}
          {carregandoPlanilha && (
            <div className="status-message">
              Validando planilha...
            </div>
          )}

          {/* Resultado da validação */}
          {validacao && (
            <div className="validation-result">
              <div className="validation-summary">
                <div className="summary-item">
                  <strong>
                    {validacao.totalEncontrados}
                  </strong>

                  <span>Encontrados</span>
                </div>

                <div className="summary-item summary-valid">
                  <strong>
                    {validacao.totalValidos}
                  </strong>

                  <span>Válidos</span>
                </div>

                <div className="summary-item summary-invalid">
                  <strong>
                    {validacao.totalInvalidos}
                  </strong>

                  <span>Inválidos</span>
                </div>
              </div>

              {/* Exibe somente os inválidos */}
              {validacao.invalidos.length > 0 && (
                <div className="invalid-recipients">
                  <div className="invalid-header">
                    <h3>
                      Destinatários inválidos
                    </h3>

                    <p>
                      Estes registros serão ignorados
                      durante o envio.
                    </p>
                  </div>

                  <div className="invalid-list">
                    {validacao.invalidos.map(
                      (destinatario) => (
                        <div
                          className="invalid-recipient"
                          key={`${destinatario.linha}-${destinatario.email}`}
                        >
                          <span className="invalid-line">
                            Linha {destinatario.linha}
                          </span>

                          <div className="invalid-data">
                            <strong>
                              {destinatario.nome ||
                                "Sem nome"}
                            </strong>

                            <span>
                              {destinatario.email ||
                                "E-mail não informado"}
                            </span>
                          </div>

                          <span className="invalid-reason">
                            {destinatario.motivo}
                          </span>
                        </div>
                      )
                    )}
                  </div>
                </div>
              )}
            </div>
          )}
        </section>

        {/* Erro */}
        {erro && (
          <div className="error-message">
            {erro}
          </div>
        )}

        {/* Ações */}
        <footer className="form-actions">
          <button
            type="submit"
            className="primary-button"
            disabled={carregandoPlanilha}
          >
            Revisar envio
          </button>
        </footer>
      </form>
    </main>
  );
}