import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { AcompanhamentoEnvio } from "./AcompanhamentoEnvio";
import { obterEnvio } from "../services/envioService";
import type { EnvioJob, StatusOperacaoEnvio } from "../types/envio";

vi.mock("../services/envioService", () => ({ obterEnvio: vi.fn() }));

function job(status: StatusOperacaoEnvio): EnvioJob {
  return {
    id: "job-1", status, total: 2, processados: 1, enviados: 1, falhas: 0, percentual: 50,
    etapaAtual: "Aguardando", segundosRestantes: 30, destinatarioAtual: "recipient@example.test",
    detalhesFalhas: [], erro: null, criadoEm: "2026-09-16T00:00:00Z",
    iniciadoEm: "2026-09-16T00:00:01Z", finalizadoEm: null,
  };
}

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.resetAllMocks();
});

it.each(["Concluido", "Falhou", "Cancelado"] as const)("permite novo envio e encerra polling em %s", async (status) => {
  vi.useFakeTimers();
  vi.mocked(obterEnvio).mockResolvedValue(job(status));
  const onNovoEnvio = vi.fn();

  await act(async () => { render(<AcompanhamentoEnvio jobId="job-1" onNovoEnvio={onNovoEnvio} />); });

  expect(screen.queryByText(/Próximo envio em/)).toBeNull();
  expect(screen.queryByText("Destinatário atual")).toBeNull();
  fireEvent.click(screen.getByRole("button", { name: "Novo envio" }));
  expect(onNovoEnvio).toHaveBeenCalledOnce();
  await act(async () => { await vi.advanceTimersByTimeAsync(10000); });
  expect(obterEnvio).toHaveBeenCalledTimes(1);
  if (status === "Cancelado") {
    expect(screen.getByRole("status").textContent).toContain("não são desfeitas");
  }
});

it("exibe o erro geral do job como texto", async () => {
  const failed = { ...job("Falhou"), erro: "Falha interna. <img src=x onerror=alert(1)>" };
  vi.mocked(obterEnvio).mockResolvedValue(failed);

  render(<AcompanhamentoEnvio jobId="job-1" onNovoEnvio={vi.fn()} />);

  const alert = await screen.findByRole("alert");
  expect(alert.textContent).toBe(failed.erro);
  expect(alert.querySelector("img")).toBeNull();
});

it("mantém o acompanhamento ativo e depois apresenta o resultado final", async () => {
  vi.useFakeTimers();
  vi.mocked(obterEnvio).mockResolvedValueOnce(job("EmAndamento"))
    .mockResolvedValueOnce({ ...job("Falhou"), erro: "Falha interna segura" });

  await act(async () => { render(<AcompanhamentoEnvio jobId="job-1" onNovoEnvio={vi.fn()} />); });

  expect(screen.getByText(/Próximo envio em/)).toBeTruthy();
  expect(screen.queryByRole("button", { name: "Novo envio" })).toBeNull();
  await act(async () => { await vi.advanceTimersByTimeAsync(2000); });
  expect(screen.getByRole("alert").textContent).toBe("Falha interna segura");
  expect(screen.getByRole("button", { name: "Novo envio" })).toBeTruthy();
  expect(screen.queryByText(/Próximo envio em/)).toBeNull();
  await act(async () => { await vi.advanceTimersByTimeAsync(10000); });
  expect(obterEnvio).toHaveBeenCalledTimes(2);
});

it("uma falha de consulta não finaliza o job e permite retomar o polling", async () => {
  vi.useFakeTimers();
  vi.mocked(obterEnvio).mockRejectedValueOnce(new Error("Não foi possível consultar o envio"))
    .mockResolvedValueOnce(job("Concluido"));

  await act(async () => { render(<AcompanhamentoEnvio jobId="job-1" onNovoEnvio={vi.fn()} />); });

  expect(screen.getByText("Não foi possível consultar o envio")).toBeTruthy();
  expect(screen.queryByRole("button", { name: "Novo envio" })).toBeNull();
  await act(async () => { await vi.advanceTimersByTimeAsync(5000); });
  expect(screen.getByRole("button", { name: "Novo envio" })).toBeTruthy();
  expect(screen.queryByText("Não foi possível consultar o envio")).toBeNull();
});
