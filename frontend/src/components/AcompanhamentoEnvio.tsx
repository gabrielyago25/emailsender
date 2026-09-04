import { useEffect, useState } from "react";

import { obterEnvio } from "../services/envioService";
import type { EnvioJob } from "../types/envio";

interface AcompanhamentoEnvioProps {
  jobId: string;
  onNovoEnvio: () => void;
}

export function AcompanhamentoEnvio({
  jobId,
  onNovoEnvio,
}: AcompanhamentoEnvioProps) {
  const [job, setJob] =
    useState<EnvioJob | null>(null);

  const [erro, setErro] =
    useState<string | null>(null);

  useEffect(() => {
    let ativo = true;
    let timeoutId: number;

    async function atualizar() {
      try {
        const resultado =
          await obterEnvio(jobId);

        if (!ativo) {
          return;
        }

        setJob(resultado);
        setErro(null);

        const finalizado =
          resultado.status === "Concluido" ||
          resultado.status === "Falhou" ||
          resultado.status === "Cancelado";

        if (!finalizado) {
          timeoutId = window.setTimeout(
            atualizar,
            2000
          );
        }
      } catch (error) {
        if (!ativo) {
          return;
        }

        setErro(
          error instanceof Error
            ? error.message
            : "Erro ao consultar o envio."
        );

        timeoutId = window.setTimeout(
          atualizar,
          5000
        );
      }
    }

    atualizar();

    return () => {
      ativo = false;
      window.clearTimeout(timeoutId);
    };
  }, [jobId]);

  if (!job) {
    return (
      <main className="container">
        <p>
          {erro ?? "Carregando envio..."}
        </p>
      </main>
    );
  }

  const concluido =
    job.status === "Concluido";

  return (
    <main className="container">
      <header className="page-header">
        <h1>Acompanhamento do envio</h1>

        <p>
          Você pode acompanhar o progresso do envio nesta página.
        </p>
      </header>

      <div className="email-form">
        <section className="form-section">
          <div className="progress-header">
            <div>
              <span className="progress-status">
                {job.status}
              </span>

              <strong>
                {job.percentual}%
              </strong>
            </div>

            <span>
              {job.processados} de {job.total} processados
            </span>
          </div>

          <div className="progress-track">
            <div
              className="progress-bar"
              style={{
                width: `${job.percentual}%`,
              }}
            />
          </div>

          {job.destinatarioAtual && (
            <div className="current-recipient">
              <span>Destinatário atual</span>
              <strong>
                {job.destinatarioAtual}
              </strong>
            </div>
          )}

          {job.etapaAtual === "Aguardando" &&
            job.segundosRestantes !== null && (
              <p className="countdown">
                Próximo envio em{" "}
                <strong>
                  {job.segundosRestantes}s
                </strong>
              </p>
            )}
        </section>

        <section className="form-section">
          <h2>Resultado</h2>

          <div className="result-summary">
            <div>
              <strong>{job.total}</strong>
              <span>Total</span>
            </div>

            <div>
              <strong>{job.enviados}</strong>
              <span>Enviados</span>
            </div>

            <div>
              <strong>{job.falhas}</strong>
              <span>Falhas</span>
            </div>
          </div>

          {job.detalhesFalhas.length > 0 && (
            <div className="send-failures">
              <h3>Falhas no envio</h3>

              {job.detalhesFalhas.map(
                (falha) => (
                  <div
                    className="send-failure"
                    key={falha.email}
                  >
                    <div>
                      <strong>
                        {falha.nome ||
                          falha.email}
                      </strong>

                      <span>{falha.email}</span>
                    </div>

                    <span>{falha.erro}</span>
                  </div>
                )
              )}
            </div>
          )}
        </section>

        {erro && (
          <div className="error-message">
            {erro}
          </div>
        )}

        {concluido && (
          <footer className="form-actions">
            <button
              type="button"
              className="primary-button"
              onClick={onNovoEnvio}
            >
              Novo envio
            </button>
          </footer>
        )}
      </div>
    </main>
  );
}