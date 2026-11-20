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
  deployed: string | null;
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
  legacy: LegacyView[];
  deploy: DeployStatus;
  ompVersion: string | null;
  libraryDir: string | null;
  libraryError: string | null;
  watcherError: string | null;
  mods: ModView[];
  invalid: { file: string; reason: string }[];
};
type Outcome = { ok: boolean; title: string; details: string[]; offer?: string };
type LegacyView = { id: string; name: string; folder: string };

function ModRow({ mod, onOutcome }: { mod: ModView; onOutcome: (o: Outcome) => void }) {
  const newest = mod.versions[0]?.version;
  const [confirming, setConfirming] = useState(false);
  const shown = mod.pinned ?? newest;
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
      {mod.versions.length > 0 && (
        <button
          className="remove"
          title="Delete this version's ZIP from the library"
          onBlur={() => setConfirming(false)}
          onClick={() => {
            if (!confirming) {
              setConfirming(true);
              return;
            }
            setConfirming(false);
            invoke<Outcome>("remove_version", { id: mod.id, version: shown }).then(onOutcome);
          }}
        >
          {confirming ? `Delete ${shown}?` : `Remove ${shown}`}
        </button>
      )}
      {mod.description && <p className="mod-description">{mod.description}</p>}
      {mod.deployed && mod.deployed !== mod.selected && (
        <p className="mod-description">
          In the game: {mod.deployed}
          {mod.selected ? ` (switching to ${mod.selected})` : mod.enabled ? "" : " (being removed)"}
        </p>
      )}
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

type AssetItem = { path: string; source: number; sourceName: string; size: number };
type AssetListing = { folders: string[]; files: AssetItem[]; more: boolean };
type AssetSummary = { gameDir: string; archives: [string, number][]; archiveErrors: string[]; loose: number; total: number };

const formatSize = (n: number) =>
  n < 1024 ? `${n} B` : n < 1048576 ? `${(n / 1024).toFixed(1)} KB` : `${(n / 1048576).toFixed(1)} MB`;
const itemKey = (i: AssetItem) => `${i.source}:${i.path}`;

type DraftFile = { gamePath: string; file: string };
type Draft = { name: string; version: string; description: string; files: DraftFile[] };
const emptyDraft: Draft = { name: "", version: "1.0.0", description: "", files: [] };

function DraftPanel({
  draft,
  setDraft,
  onOutcome,
}: {
  draft: Draft;
  setDraft: (d: Draft) => void;
  onOutcome: (o: Outcome) => void;
}) {
  const [building, setBuilding] = useState(false);
  const build = (toLibrary: boolean) => {
    setBuilding(true);
    invoke<Outcome>("draft_build", { draft, toLibrary })
      .then(onOutcome)
      .finally(() => setBuilding(false));
  };
  return (
    <div className="draft">
      <h3>New replacement mod</h3>
      <div className="draft-fields">
        <label>
          Name
          <input value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} placeholder="Red clone troopers" />
        </label>
        <label>
          Version
          <input value={draft.version} onChange={(e) => setDraft({ ...draft, version: e.target.value })} />
        </label>
        <label className="wide">
          Description (optional)
          <input value={draft.description} onChange={(e) => setDraft({ ...draft, description: e.target.value })} />
        </label>
      </div>
      <ul className="assets">
        {draft.files.map((f) => (
          <li key={f.gamePath} className="asset">
            <span className="asset-path">{f.gamePath}</span>
            <span className="asset-source" title={f.file}>
              ← {f.file.split(/[\\/]/).pop()}
            </span>
            <button className="link" onClick={() => setDraft({ ...draft, files: draft.files.filter((x) => x !== f) })}>
              Remove
            </button>
          </li>
        ))}
      </ul>
      <p className="hint">
        Use <em>Replace…</em> on a game file to add it. The mod needs OMP's v1 mod and only replaces files inside the game's
        archives.
      </p>
      <div className="toolbar">
        <button disabled={building || draft.files.length === 0} onClick={() => build(true)}>
          Add to library
        </button>
        <button disabled={building || draft.files.length === 0} onClick={() => build(false)}>
          Export ZIP…
        </button>
        <button disabled={building} onClick={() => setDraft(emptyDraft)}>
          Discard
        </button>
      </div>
    </div>
  );
}

function GameFiles({ targets }: { targets: TargetView[] }) {
  const available = targets.filter((t) => t.path);
  const [kind, setKind] = useState(available[0]?.kind ?? "steam");
  const [summary, setSummary] = useState<AssetSummary | null>(null);
  const [opening, setOpening] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [folder, setFolder] = useState("");
  const [query, setQuery] = useState("");
  const [listing, setListing] = useState<AssetListing | null>(null);
  const [selected, setSelected] = useState<Map<string, AssetItem>>(new Map());
  const [outcome, setOutcome] = useState<Outcome | null>(null);
  const [extracting, setExtracting] = useState(false);
  const [draft, setDraft] = useState<Draft>(emptyDraft);

  const replace = (item: AssetItem) =>
    invoke<string | null>("draft_pick_file", { gamePath: item.path }).then((file) => {
      if (!file) return;
      setDraft((d) => ({
        ...d,
        files: [...d.files.filter((f) => f.gamePath !== item.path), { gamePath: item.path, file }],
      }));
    });

  const open = (k: string) => {
    setOpening(true);
    setError(null);
    setSummary(null);
    setSelected(new Map());
    invoke<AssetSummary>("assets_open", { kind: k })
      .then((s) => {
        setSummary(s);
        setFolder("");
        setQuery("");
      })
      .catch((e) => setError(String(e)))
      .finally(() => setOpening(false));
  };

  useEffect(() => {
    if (!summary) return;
    const q = query.trim();
    const request = q
      ? invoke<AssetListing>("assets_search", { query: q, folder })
      : invoke<AssetListing>("assets_list", { folder });
    let current = true;
    request.then((l) => current && setListing(l)).catch((e) => current && setError(String(e)));
    return () => {
      current = false;
    };
  }, [summary, folder, query]);

  const toggle = (item: AssetItem) =>
    setSelected((old) => {
      const next = new Map(old);
      if (next.has(itemKey(item))) next.delete(itemKey(item));
      else next.set(itemKey(item), item);
      return next;
    });

  const extract = () => {
    setExtracting(true);
    const items = [...selected.values()].map((i) => ({ path: i.path, source: i.source }));
    invoke<Outcome>("assets_extract", { items })
      .then((o) => setOutcome(o.ok && o.details.length === 0 ? null : o))
      .finally(() => setExtracting(false));
  };

  const crumbs = folder ? folder.split("/") : [];

  if (available.length === 0) {
    return <p className="empty">Choose your game folder in the Mods tab first.</p>;
  }

  return (
    <section>
      <div className="toolbar">
        {available.length > 1 && (
          <select value={kind} onChange={(e) => setKind(e.target.value as TargetView["kind"])} aria-label="Game">
            {available.map((t) => (
              <option key={t.kind} value={t.kind}>
                {t.label}
              </option>
            ))}
          </select>
        )}
        <button disabled={opening} onClick={() => open(kind)}>
          {opening ? "Reading the game files…" : summary ? "Reload" : "Open game files"}
        </button>
        {summary && (
          <span className="hint">
            {summary.total.toLocaleString()} files · {summary.archives.length} archives · {summary.loose.toLocaleString()} loose
          </span>
        )}
      </div>
      {error && <div className="banner error">{error}</div>}
      {summary && summary.archiveErrors.length > 0 && (
        <div className="banner error">
          <strong>Some archives couldn't be read:</strong>
          <ul>
            {summary.archiveErrors.map((e) => (
              <li key={e}>{e}</li>
            ))}
          </ul>
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
      {summary && draft.files.length > 0 && (
        <DraftPanel draft={draft} setDraft={setDraft} onOutcome={(o) => setOutcome(o.ok && o.details.length === 0 ? null : o)} />
      )}
      {summary && listing && (
        <>
          <div className="toolbar">
            <input
              className="search"
              type="search"
              placeholder={folder ? `Search in ${folder}` : "Search all game files, e.g. clonetrooper .ghg"}
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
            <button disabled={selected.size === 0 || extracting} onClick={extract}>
              {extracting ? "Extracting…" : `Extract ${selected.size || ""} selected…`}
            </button>
            {selected.size > 0 && <button onClick={() => setSelected(new Map())}>Clear</button>}
          </div>
          <nav className="crumbs">
            <button className="link" onClick={() => setFolder("")}>
              Game
            </button>
            {crumbs.map((c, i) => (
              <span key={i}>
                {" / "}
                <button className="link" onClick={() => setFolder(crumbs.slice(0, i + 1).join("/"))}>
                  {c}
                </button>
              </span>
            ))}
          </nav>
          <ul className="assets">
            {listing.folders.map((f) => (
              <li key={`d:${f}`} className="asset folder">
                <button className="link" onClick={() => setFolder(folder ? `${folder}/${f}` : f)}>
                  📁 {f}
                </button>
              </li>
            ))}
            {listing.files.map((item) => (
              <li key={itemKey(item)} className="asset">
                <label>
                  <input type="checkbox" checked={selected.has(itemKey(item))} onChange={() => toggle(item)} />
                  <span className="asset-path">{query ? item.path : item.path.split("/").pop()}</span>
                </label>
                <span className="asset-source">{item.sourceName}</span>
                <span className="asset-size">{formatSize(item.size)}</span>
                {item.sourceName === "loose file" ? (
                  <span />
                ) : (
                  <button className="replace" onClick={() => replace(item)}>
                    Replace…
                  </button>
                )}
              </li>
            ))}
            {listing.folders.length === 0 && listing.files.length === 0 && <li className="empty">Nothing here.</li>}
          </ul>
          {listing.more && <p className="hint">Showing the first {listing.files.length} files. Search to narrow it down.</p>}
        </>
      )}
    </section>
  );
}

function App() {
  const [library, setLibrary] = useState<LibraryView | null>(null);
  const [outcome, setOutcome] = useState<Outcome | null>(null);
  const [dragging, setDragging] = useState(false);
  const [busy, setBusy] = useState(false);
  const [tab, setTab] = useState<"mods" | "files">("mods");

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
        <nav className="tabs">
          <button className={tab === "mods" ? "active" : ""} onClick={() => setTab("mods")}>
            Mods
          </button>
          <button className={tab === "files" ? "active" : ""} onClick={() => setTab("files")}>
            Game files
          </button>
        </nav>
        {tab === "mods" ? (
          <button disabled={busy || !library.libraryDir} onClick={() => runImport(() => invoke<Outcome>("pick_and_import"))}>
            {busy ? "Adding…" : "Add mod ZIPs…"}
          </button>
        ) : (
          <span />
        )}
      </header>

      {tab === "files" && <GameFiles targets={library.targets} />}
      <div hidden={tab !== "mods"}>
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
          {outcome.offer && (
            <button
              onClick={() =>
                invoke<Outcome>("set_enabled", { id: outcome.offer, enabled: true, withDependencies: true }).then(showOutcome)
              }
            >
              Enable {library.mods.find((m) => m.id === outcome.offer)?.name ?? outcome.offer} and the mods it needs
            </button>
          )}
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

      {library.legacy.length > 0 && (
        <section>
          <h2>Old mods</h2>
          <p className="hint">
            These were in your game's <code>mods</code> folder before ReOMP. Add one to the library to use it again; a missing
            version is set to 0.1.0.
          </p>
          <ul className="mods">
            {library.legacy.map((m) => (
              <li key={m.folder} className="target">
                <span className="mod-name">{m.name}</span>
                <span className="mod-id">{m.id}</span>
                <button
                  disabled={busy}
                  onClick={() => runImport(() => invoke<Outcome>("add_legacy_mod", { folder: m.folder }))}
                >
                  Add to library
                </button>
              </li>
            ))}
          </ul>
        </section>
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
      </div>
      {dragging && <div className="drop-overlay">Drop mod ZIPs to add them</div>}
    </main>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
