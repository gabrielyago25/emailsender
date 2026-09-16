import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { NovoEnvioPage } from "./NovoEnvioPage";
import { validarPlanilha } from "../services/planilhaService";
import { criarEnvio } from "../services/envioService";
import type { ResultadoValidacaoPlanilha } from "../types/planilha";

vi.mock("../services/planilhaService", () => ({
  validarPlanilha: vi.fn(),
  baixarModeloPlanilha: vi.fn(),
}));
vi.mock("../services/envioService", () => ({
  criarEnvio: vi.fn(),
  obterEnvio: vi.fn(),
}));
vi.mock("../components/EditorEmail", () => ({
  EditorEmail: ({ onChange }: { onChange: (html: string) => void }) => (
    <textarea aria-label="Mensagem" onChange={(event) => onChange(event.target.value)} />
  ),
}));

function deferred() {
  let resolve!: (value: ResultadoValidacaoPlanilha) => void;
  let reject!: (error: Error) => void;
  const promise = new Promise<ResultadoValidacaoPlanilha>((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
}

function result(total: number): ResultadoValidacaoPlanilha {
  return { totalEncontrados: total, totalValidos: total, totalInvalidos: 0, invalidos: [] };
}

function selectFile(name: string) {
  const file = new File(["test workbook"], name, {
    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  });
  fireEvent.change(screen.getByLabelText("Selecionar arquivo"), { target: { files: [file] } });
  return file;
}

function fillMessage() {
  fireEvent.change(screen.getByLabelText("Assunto"), { target: { value: "Comunicado" } });
  fireEvent.change(screen.getByLabelText("Mensagem"), { target: { value: "<p>Olá</p>" } });
}

afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});

describe("Validação da planilha selecionada", () => {
  it.each(["success", "failure"])("ignora %s anterior enquanto a nova validação está pendente", async (outcome) => {
    const first = deferred();
    const second = deferred();
    vi.mocked(validarPlanilha).mockReturnValueOnce(first.promise).mockReturnValueOnce(second.promise);
    render(<NovoEnvioPage />);
    fillMessage();
    selectFile("anterior.xlsx");
    const firstSignal = vi.mocked(validarPlanilha).mock.calls[0][1];
    selectFile("atual.xlsx");

    expect(firstSignal?.aborted).toBe(true);
    await act(async () => {
      if (outcome === "success") first.resolve(result(99));
      else first.reject(new Error("Erro da planilha anterior"));
    });

    expect(screen.getByText("atual.xlsx")).toBeTruthy();
    expect(screen.getByText("Validando planilha...")).toBeTruthy();
    expect((screen.getByRole("button", { name: "Revisar envio" }) as HTMLButtonElement).disabled).toBe(true);
    expect(screen.queryByText("99")).toBeNull();
    expect(screen.queryByText("Erro da planilha anterior")).toBeNull();
    // Uma submissão programática também não deve abrir a revisão.
    fireEvent.submit(screen.getByLabelText("Assunto").closest("form")!);
    expect(screen.queryByRole("heading", { name: "Revisar envio" })).toBeNull();

    await act(async () => second.resolve(result(7)));
    fireEvent.click(screen.getByRole("button", { name: "Revisar envio" }));
    expect(screen.getByText("atual.xlsx")).toBeTruthy();
    expect(screen.getByText("7")).toBeTruthy();
  });

  it.each(["success", "failure"])("preserva a revisão e o arquivo enviado após %s tardio da planilha anterior", async (outcome) => {
    const first = deferred();
    const second = deferred();
    vi.mocked(validarPlanilha).mockReturnValueOnce(first.promise).mockReturnValueOnce(second.promise);
    // Mantém a requisição de envio pendente para inspecionar a confirmação.
    vi.mocked(criarEnvio).mockReturnValue(new Promise(() => {}));
    render(<NovoEnvioPage />);
    fillMessage();
    selectFile("anterior.xlsx");
    const currentFile = selectFile("atual.xlsx");
    await act(async () => second.resolve(result(7)));
    fireEvent.click(screen.getByRole("button", { name: "Revisar envio" }));

    await act(async () => {
      if (outcome === "success") first.resolve(result(99));
      else first.reject(new Error("Erro antigo"));
    });

    expect(screen.getByRole("heading", { name: "Revisar envio" })).toBeTruthy();
    expect(screen.getByText("atual.xlsx")).toBeTruthy();
    expect(screen.getByText("7")).toBeTruthy();
    expect(screen.queryByText("99")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Enviar" }));
    expect(criarEnvio).toHaveBeenCalledExactlyOnceWith(currentFile, "Comunicado", "<p>Olá</p>");
  });

  it("mostra a falha atual e permite validar novamente o mesmo arquivo", async () => {
    vi.mocked(validarPlanilha).mockRejectedValueOnce(new Error("Planilha inválida"))
      .mockResolvedValueOnce(result(3));
    render(<NovoEnvioPage />);
    fillMessage();
    selectFile("atual.xlsx");
    expect(await screen.findByText("Planilha inválida")).toBeTruthy();
    expect(screen.getByText("Nenhum arquivo selecionado")).toBeTruthy();
    expect(screen.queryByText("Validando planilha...")).toBeNull();
    expect((screen.getByLabelText("Selecionar arquivo") as HTMLInputElement).value).toBe("");

    selectFile("atual.xlsx");
    await screen.findByText("Encontrados");
    fireEvent.click(screen.getByRole("button", { name: "Revisar envio" }));
    expect(screen.getByText("3")).toBeTruthy();
    expect(screen.queryByText("Planilha inválida")).toBeNull();
  });

  it("cancela a validação ao desmontar a página", async () => {
    const pending = deferred();
    vi.mocked(validarPlanilha).mockReturnValue(pending.promise);
    const { unmount } = render(<NovoEnvioPage />);
    selectFile("atual.xlsx");
    const signal = vi.mocked(validarPlanilha).mock.calls[0][1];
    expect(signal?.aborted).toBe(false);

    unmount();

    expect(signal?.aborted).toBe(true);
    await act(async () => pending.reject(new DOMException("Aborted", "AbortError")));
  });
});
