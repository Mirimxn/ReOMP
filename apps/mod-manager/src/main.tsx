import { StrictMode, useCallback, useEffect, useState } from "react";
import { createRoot } from "react-dom/client";

// Tauri's global API (app.withGlobalTauri). The backend owns all file work.
type Unlisten = () => void;
declare global {
  interface Window {
    __TAURI__: {
      core: { invoke<T>(command: string, args?: Record<string, unknown>): Promise<T> };
      event: { listen<T>(event: string, handler: (event: { payload: T }) => void): Promise<Unlisten> };
    };
  }
}
const invoke = <T,>(command: string, args?: Record<string, unknown>) => window.__TAURI__.core.invoke<T>(command, args);
const listen = <T,>(event: string, handler: (payload: T) => void) =>
  window.__TAURI__.event.listen<T>(event, (e) => handler(e.payload));

type VersionView = { version: string; files: string[]; conflict: boolean };
type ModView = {
  id: string;
  name: string;
  description: string | null;
  versions: VersionView[];
  enabled: boolean;
  pinned: string | null;
  selected: string | null;
  problems: string[];
};
type TargetView = {
  kind: "steam" | "epic";
  label: string;
  path: string | null;
  enabled: boolean;
  installed: boolean;
  installedMods: string[];
};
type DeployStatus =
  | { state: "idle" }
  | { state: "working" }
  | { state: "waiting"; games: string[] }
  | { state: "blocked"; reasons: string[] }
  | { state: "failed"; title: string; details: string[] };
type LibraryView = {
  targets: TargetView[];
  deploy: DeployStatus;
  ompVersion: string | null;
  libraryDir: string | null;
  libraryError: string | null;
  watcherError: string | null;
  mods: ModView[];
  invalid: { file: string; reason: string }[];
};
type Outcome = { ok: boolean; title: string; details: string[] };

function ModRow({ mod, onOutcome }: { mod: ModView; onOutcome: (o: Outcome) => void }) {
  const newest = mod.versions[0]?.version;
  return (
    <li className={`mod${mod.problems.length ? " has-problems" : ""}`}>
      <label className="toggle">
        <input
          type="checkbox"
          checked={mod.enabled}
          onChange={(e) => invoke<Outcome>("set_enabled", { id: mod.id, enabled: e.target.checked }).then(onOutcome)}
        />
        <span className="mod-name">{mod.name}</span>
      </label>
      <span className="mod-id">{mod.id}</span>
      {mod.versions.length > 0 && (
        <select
          aria-label={`Version of ${mod.name}`}
          value={mod.pinned ?? ""}
          onChange={(e) =>
            invoke<Outcome>("set_pinned_version", { id: mod.id, version: e.target.value || null }).then(onOutcome)
          }
        >
          <option value="">Newest ({newest})</option>
          {mod.pinned && !mod.versions.some((v) => v.version === mod.pinned) && (
            <option value={mod.pinned}>{mod.pinned} (missing)</option>
          )}
          {mod.versions.map((v) => (
            <option key={v.version} value={v.version}>
              {v.version}
              {v.conflict ? " (conflict)" : ""}
            </option>
          ))}
        </select>
      )}
      {mod.description && <p className="mod-description">{mod.description}</p>}
      {mod.problems.map((p) => (
        <p key={p} className="problem">
          {p}
        </p>
      ))}
    </li>
  );
}

function DeployBanner({ status, onRetry }: { status: DeployStatus; onRetry: () => void }) {
  switch (status.state) {
    case "idle":
      return null;
    case "working":
      return <div className="banner info">Updating the game files…</div>;
    case "waiting":
      return (
        <div className="banner info">
          <strong>Waiting for the game to close.</strong> Changes are applied when {status.games.join(" and ")} closes.
        </div>
      );
    case "blocked":
      return (
        <div className="banner error">
          <strong>The game can't be updated yet:</strong>
          <ul>
            {status.reasons.map((r) => (
              <li key={r}>{r}</li>
            ))}
          </ul>
        </div>
      );
    case "failed":
      return (
        <div className="banner error">
          <strong>{status.title}</strong>
          <ul>
            {status.details.map((d) => (
              <li key={d}>{d}</li>
            ))}
          </ul>
          <button onClick={onRetry}>Retry</button>
        </div>
      );
  }
}

function TargetRow({ target, onOutcome }: { target: TargetView; onOutcome: (o: Outcome) => void }) {
  return (
    <li className="target">
      <label className="toggle">
        <input
          type="checkbox"
          checked={target.enabled}
          onChange={(e) =>
            invoke<Outcome>("set_target_enabled", { kind: target.kind, enabled: e.target.checked }).then(onOutcome)
          }
        />
        <span className="mod-name">{target.label}</span>
      </label>
      <span className="target-state">
        {target.installed ? `Installed · ${target.installedMods.length} mod${target.installedMods.length === 1 ? "" : "s"}` : "Not installed"}
      </span>
      <button onClick={() => invoke<Outcome>("pick_game_folder", { kind: target.kind }).then(onOutcome)}>
        {target.path ? "Change folder…" : "Choose folder…"}
      </button>
      <p className="mod-description">
        {target.path ? <code>{target.path}</code> : "Game folder not found. Choose it to install OMP and mods."}
      </p>
    </li>
  );
}

function App() {
  const [library, setLibrary] = useState<LibraryView | null>(null);
  const [outcome, setOutcome] = useState<Outcome | null>(null);
  const [dragging, setDragging] = useState(false);
  const [busy, setBusy] = useState(false);

  const refresh = useCallback(() => {
    invoke<LibraryView>("get_library").then(setLibrary);
  }, []);

  const showOutcome = useCallback((o: Outcome) => {
    setOutcome(o.ok && o.details.length === 0 ? null : o);
    refresh();
  }, [refresh]);

  const runImport = useCallback(
    (start: () => Promise<Outcome>) => {
      setBusy(true);
      start()
        .then(showOutcome)
        .catch((e) => showOutcome({ ok: false, title: "Import failed:", details: [String(e)] }))
        .finally(() => setBusy(false));
    },
    [showOutcome],
  );

  useEffect(() => {
    refresh();
    const subscriptions = [
      listen<null>("library-changed", refresh),
      listen<unknown>("tauri://drag-enter", () => setDragging(true)),
      listen<unknown>("tauri://drag-leave", () => setDragging(false)),
      listen<{ paths: string[] }>("tauri://drag-drop", ({ paths }) => {
        setDragging(false);
        runImport(() => invoke<Outcome>("import_zips", { paths }));
      }),
    ];
    return () => subscriptions.forEach((s) => s.then((unlisten) => unlisten()));
  }, [refresh, runImport]);

  if (!library) return <main className="app">Loading…</main>;

  return (
    <main className={`app${dragging ? " dragging" : ""}`}>
      <header>
        <h1>ReOMP Mod Manager</h1>
        <button disabled={busy || !library.libraryDir} onClick={() => runImport(() => invoke<Outcome>("pick_and_import"))}>
          {busy ? "Adding…" : "Add mod ZIPs…"}
        </button>
      </header>

      <section>
        <h2>Game</h2>
        <p className="hint">
          Tick a game to install OMP{library.ompVersion ? ` (${library.ompVersion})` : ""} and your enabled mods into it.
          Changes are applied automatically. Your old <code>mods</code> folder is kept in <code>legacy-mods</code>.
        </p>
        <DeployBanner status={library.deploy} onRetry={() => invoke("retry_deploy").then(refresh)} />
        <ul className="mods">
          {library.targets.map((t) => (
            <TargetRow key={t.kind} target={t} onOutcome={showOutcome} />
          ))}
        </ul>
      </section>

      <h2>Mods</h2>
      {library.libraryError && <div className="banner error">{library.libraryError}</div>}
      {library.watcherError && (
        <div className="banner error">
          {library.watcherError}{" "}
          <button onClick={() => invoke("retry_watcher").then(refresh)}>Retry</button>
        </div>
      )}
      {outcome && (
        <div className={`banner ${outcome.ok ? "info" : "error"}`}>
          {outcome.title && <strong>{outcome.title}</strong>}
          <ul>
            {outcome.details.map((d) => (
              <li key={d}>{d}</li>
            ))}
          </ul>
          <button className="dismiss" onClick={() => setOutcome(null)}>
            Dismiss
          </button>
        </div>
      )}

      <p className="hint">
        Drag mod ZIPs onto this window, or use <em>Add mod ZIPs…</em>.
        {library.libraryDir && (
          <>
            {" "}
            ZIPs copied into <code>{library.libraryDir}</code> also appear here.
          </>
        )}
      </p>

      {library.mods.length === 0 ? (
        <p className="empty">No mods in the library yet.</p>
      ) : (
        <ul className="mods">
          {library.mods.map((m) => (
            <ModRow key={m.id} mod={m} onOutcome={showOutcome} />
          ))}
        </ul>
      )}

      {library.invalid.length > 0 && (
        <section>
          <h2>Files that couldn't be read</h2>
          <ul className="invalid">
            {library.invalid.map((i) => (
              <li key={i.file}>
                <code>{i.file}</code>: {i.reason}
              </li>
            ))}
          </ul>
        </section>
      )}
      {dragging && <div className="drop-overlay">Drop mod ZIPs to add them</div>}
    </main>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
