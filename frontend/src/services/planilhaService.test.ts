import { afterEach, expect, it, vi } from "vitest";
import { validarPlanilha } from "./planilhaService";

vi.mock("./api", () => ({ API_URL: "https://api.example.test" }));

afterEach(() => vi.unstubAllGlobals());

it("encaminha o cancelamento da validação ao fetch", async () => {
  const controller = new AbortController();
  const arquivo = new File(["test"], "destinatarios.xlsx");
  const fetchMock = vi.fn().mockImplementation((_url, options: RequestInit) =>
    new Promise((_resolve, reject) => {
      options.signal?.addEventListener("abort", () => reject(new DOMException("Aborted", "AbortError")));
    }));
  vi.stubGlobal("fetch", fetchMock);

  const pending = validarPlanilha(arquivo, controller.signal);
  const rejected = expect(pending).rejects.toMatchObject({ name: "AbortError" });
  controller.abort();

  await rejected;
  expect(fetchMock).toHaveBeenCalledOnce();
  const [url, options] = fetchMock.mock.calls[0];
  expect(url).toBe("https://api.example.test/api/planilhas/validar");
  expect(options.method).toBe("POST");
  expect(options.signal).toBe(controller.signal);
  expect(options.body.get("arquivo").name).toBe("destinatarios.xlsx");
});
