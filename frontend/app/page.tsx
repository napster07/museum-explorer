"use client";
import { useEffect, useState } from "react";
type Exhibition = {
  id: string;
  slug: string;
  title: string;
  category: string;
  summary: string;
  description: string;
  gallery: string;
  minutes: number;
  colour: string;
  access: string[];
};
type Saved = { id: string; accessToken: string };
const storageKey = "museum-explorer-itinerary";
export default function Home() {
  const [exhibitions, setExhibitions] = useState<Exhibition[]>([]);
  const [selected, setSelected] = useState<string[]>([]);
  const [filter, setFilter] = useState("All exhibitions");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [status, setStatus] = useState("");
  const [saving, setSaving] = useState(false);
  const [expanded, setExpanded] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    async function load() {
      setLoading(true);
      setError("");
      try {
        const response = await fetch("/api/exhibitions", {
          signal: controller.signal,
        });
        if (!response.ok)
          throw new Error(
            "Exhibitions are temporarily unavailable. Please try again shortly.",
          );
        const loaded: Exhibition[] = await response.json();
        setExhibitions(loaded);
        try {
          const stored = localStorage.getItem(storageKey);
          if (stored) {
            const saved: Saved = JSON.parse(stored);
            const result = await fetch(`/api/itineraries/${saved.id}`, {
              headers: { Authorization: `Bearer ${saved.accessToken}` },
              signal: controller.signal,
            });
            if (result.ok) {
              const restored: string[] = (await result.json()).exhibitionIds;
              const available = restored.filter((id) =>
                loaded.some((exhibition) => exhibition.id === id),
              );
              setSelected(available);
              setStatus(
                available.length === restored.length
                  ? "Your saved itinerary has been restored."
                  : "Your visit has been restored. Some exhibitions are no longer available; save again to update your itinerary.",
              );
            } else
              setStatus(
                "Your previous itinerary could not be restored. You can create a new one.",
              );
          }
        } catch {
          if (!controller.signal.aborted)
            setStatus("Your previous itinerary could not be restored.");
        }
      } catch (e) {
        if (!controller.signal.aborted)
          setError(
            e instanceof Error ? e.message : "Unable to load exhibitions.",
          );
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }
    void load();
    return () => controller.abort();
  }, [attempt]);
  function toggle(id: string) {
    setSelected((current) =>
      current.includes(id)
        ? current.filter((item) => item !== id)
        : [...current, id],
    );
    setStatus("You have unsaved changes.");
  }
  async function save() {
    setSaving(true);
    setStatus("");
    try {
      const response = await fetch("/api/itineraries", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ exhibitionIds: selected }),
      });
      if (!response.ok)
        throw new Error(
          response.status === 429
            ? "Please wait a minute before saving again."
            : "We couldn’t save your itinerary. An exhibition may have changed or the service may be unavailable. Refresh the page to check availability, then try again.",
        );
      const saved: Saved = await response.json();
      try {
        localStorage.setItem(storageKey, JSON.stringify(saved));
        setStatus(
          "Saved to the database. Your itinerary will return on this browser for up to 30 days.",
        );
      } catch {
        setStatus(
          "Saved to the database, but browser storage is unavailable. This browser cannot restore it after you leave.",
        );
      }
    } catch (e) {
      setStatus(
        e instanceof Error ? e.message : "Unable to save. Please try again.",
      );
    } finally {
      setSaving(false);
    }
  }
  const stops = selected
    .map((id) => exhibitions.find((e) => e.id === id))
    .filter((e): e is Exhibition => Boolean(e));
  const minutes = stops.reduce((total, e) => total + e.minutes, 0);
  const visible = exhibitions.filter(
    (e) => filter === "All exhibitions" || e.category === filter,
  );
  return (
    <>
      <a className="skip" href="#main">
        Skip to content
      </a>
      <header className="site-header">
        <a className="brand" href="/" aria-label="Museum Explorer home">
          <span className="brand-mark" aria-hidden="true">
            m<span>e</span>
          </span>
          <span>
            MUSEUM
            <br />
            EXPLORER
          </span>
        </a>
        <nav aria-label="Main navigation">
          <a href="#exhibitions">Explore</a>
          <a href="#itinerary">
            Your visit <span className="count">{selected.length}</span>
          </a>
        </nav>
        <span className="prototype">Independent prototype</span>
      </header>
      <main id="main">
        <section className="hero" aria-labelledby="hero-title">
          <div className="hero-copy">
            <p className="eyebrow">A LITTLE CURIOSITY GOES A LONG WAY</p>
            <h1 id="hero-title">
              Find something
              <br />
              that <em>moves you.</em>
            </h1>
            <p>
              From the depths of the ocean to a new way of seeing.
              <br className="desktop-break" /> Make room for discovery. Make
              this visit yours.
            </p>
            <a className="primary-link" href="#exhibitions">
              Explore the exhibitions <span aria-hidden="true">↗</span>
            </a>
            <div className="hero-note">
              <span aria-hidden="true">✳</span> Find your own way to look
              closer.
            </div>
          </div>
          <div
            className="hero-art"
            role="img"
            aria-label="Abstract concentric circles in sea green and coral, suggesting ripples and discovery"
          >
            <div className="orbit orbit-one" />
            <div className="orbit orbit-two" />
            <div className="orbit orbit-three" />
            <div className="orbit orbit-four" />
            <span className="art-label">
              LOOK CLOSER.
              <br />
              THERE’S MORE HERE.
            </span>
            <span className="art-index">EXPLORER / 001</span>
          </div>
        </section>
        <div className="intro-strip">
          <span>OPEN YOUR MIND. FOLLOW YOUR CURIOSITY.</span>
          <span>Art · Nature · Science</span>
        </div>
        <div className="content-grid">
          <section id="exhibitions" aria-labelledby="exhibition-title">
            <div className="section-heading">
              <div>
                <p className="eyebrow">PICK YOUR NEXT DISCOVERY</p>
                <h2 id="exhibition-title">What will you explore?</h2>
              </div>
              <span className="small-label">
                {exhibitions.length.toString().padStart(2, "0")} exhibitions
              </span>
            </div>
            <div className="filters" aria-label="Filter exhibitions">
              {[
                "All exhibitions",
                "Natural world",
                "Art & design",
                "Science",
              ].map((category) => (
                <button
                  key={category}
                  onClick={() => setFilter(category)}
                  aria-pressed={filter === category}
                >
                  {category}
                </button>
              ))}
            </div>
            {loading && (
              <p role="status" className="notice">
                Opening the collection…
              </p>
            )}
            {error && (
              <div role="alert" className="notice">
                {error}{" "}
                <button onClick={() => setAttempt((value) => value + 1)}>
                  Try again
                </button>
              </div>
            )}
            {!loading && !error && visible.length === 0 && (
              <p className="notice" role="status">
                No exhibitions are available in this category yet.
              </p>
            )}
            <div className="cards">
              {visible.map((exhibition) => (
                <article className="card" key={exhibition.id}>
                  <div
                    className={`card-art ${exhibition.colour}`}
                    aria-hidden="true"
                  >
                    <span className="art-number">
                      {exhibition.gallery.replace("Gallery ", "")}
                    </span>
                    <div className="sculpture" />
                    <span className="art-caption">{exhibition.category}</span>
                  </div>
                  <div className="card-content">
                    <div className="card-meta">
                      {exhibition.gallery}
                      <span>{exhibition.minutes} min</span>
                    </div>
                    <h3>{exhibition.title}</h3>
                    <p>{exhibition.summary}</p>
                    <button
                      className="details"
                      aria-expanded={expanded === exhibition.id}
                      aria-controls={`detail-${exhibition.id}`}
                      onClick={() =>
                        setExpanded(
                          expanded === exhibition.id ? null : exhibition.id,
                        )
                      }
                    >
                      Look closer{" "}
                      <span aria-hidden="true">
                        {expanded === exhibition.id ? "−" : "+"}
                      </span>
                      <span className="sr-only">: {exhibition.title}</span>
                    </button>
                    <div
                      id={`detail-${exhibition.id}`}
                      hidden={expanded !== exhibition.id}
                    >
                      <p>{exhibition.description}</p>
                      <ul className="access-list">
                        {exhibition.access.map((item) => (
                          <li key={item}>{item}</li>
                        ))}
                      </ul>
                    </div>
                    <button
                      disabled={saving || loading}
                      className={`add-button ${selected.includes(exhibition.id) ? "selected" : ""}`}
                      aria-pressed={selected.includes(exhibition.id)}
                      onClick={() => toggle(exhibition.id)}
                    >
                      {selected.includes(exhibition.id)
                        ? "✓ Added to your visit"
                        : "+ Add to your visit"}
                      <span className="sr-only">: {exhibition.title}</span>
                    </button>
                  </div>
                </article>
              ))}
            </div>
          </section>
          <aside
            id="itinerary"
            className="itinerary"
            aria-labelledby="visit-title"
          >
            <p className="eyebrow">A DAY THAT’S YOURS</p>
            <h2 id="visit-title">
              Your visit<span aria-hidden="true">↗</span>
            </h2>
            <p className="muted">A little planning. A lot to discover.</p>
            {stops.length === 0 ? (
              <div className="empty">
                <span aria-hidden="true">＋</span>
                <h3>Start with a spark.</h3>
                <p>
                  Add an exhibition that catches your eye. Your itinerary will
                  take shape here.
                </p>
              </div>
            ) : (
              <ol className="stops">
                {stops.map((e) => (
                  <li key={e.id}>
                    <div>
                      <strong>{e.title}</strong>
                      <small>
                        {e.gallery} · {e.minutes} min
                      </small>
                    </div>
                    <button
                      disabled={saving}
                      onClick={() => toggle(e.id)}
                      aria-label={`Remove ${e.title}`}
                    >
                      ×
                    </button>
                  </li>
                ))}
              </ol>
            )}
            <div className="visit-total">
              <span>Time to explore</span>
              <strong>{minutes} min</strong>
            </div>
            <button
              className="save-button"
              onClick={save}
              disabled={selected.length === 0 || saving || loading}
            >
              {saving ? "Saving your visit…" : "Save my itinerary"}
              <span aria-hidden="true">→</span>
            </button>
            <p className="save-note">
              No account needed. Saved for 30 days.
              <br />
              Use a personal device to keep your visit private.
            </p>
            <p className="status" role="status" aria-live="polite">
              {status}
            </p>
          </aside>
        </div>
      </main>
      <footer>
        <span className="footer-brand">Museum Explorer</span>
        <p>
          A learning project inspired by museum experiences.
          <br />
          Fictional exhibitions. Not affiliated with Te Papa.
        </p>
        <a href="#main">Back to top ↑</a>
      </footer>
    </>
  );
}
