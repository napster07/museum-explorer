"use client";
import { useEffect, useRef, useState } from "react";
import MuseumExplorer from "../museum-explorer";

const idleMs = 5 * 60 * 1000;
const warningMs = 60 * 1000;
const storageKey = "museum-explorer-itinerary";
function clearSavedVisit() {
  // Clear only this application's visitor credential, never unrelated browser data.
  try { localStorage.removeItem(storageKey); } catch {}
  try { sessionStorage.removeItem(storageKey); } catch {}
}
export default function KioskSession() {
  const [session, setSession] = useState(0);
  const [notice, setNotice] = useState("");
  const [remaining, setRemaining] = useState(60);
  const dialog = useRef<HTMLDialogElement>(null);
  const previousFocus = useRef<HTMLElement | null>(null);
  const deadline = useRef(0);
  const resetAction = useRef<() => void>(() => {});

  function finish() {
    dialog.current?.close();
    clearSavedVisit();
    deadline.current = Date.now() + idleMs;
    setSession(value => value + 1);
    setNotice("The previous visit has been cleared. Welcome—start a new visit.");
  }
  function keepGoing() {
    if (Date.now() >= deadline.current) { finish(); return; }
    dialog.current?.close();
    deadline.current = Date.now() + idleMs;
    previousFocus.current?.focus();
  }
  useEffect(() => { resetAction.current = finish; });
  useEffect(() => {
    clearSavedVisit();
    deadline.current = Date.now() + idleMs;
    function check() {
      const left = deadline.current - Date.now();
      if (left <= 0) { resetAction.current(); return; }
      if (left <= warningMs) {
        setRemaining(Math.ceil(left / 1000));
        if (dialog.current && !dialog.current.open) {
          previousFocus.current = document.activeElement as HTMLElement;
          dialog.current.showModal();
        }
      }
    }
    function activity() {
      // A resumed/suspended tab must expire before accepting new interaction.
      if (Date.now() >= deadline.current) { resetAction.current(); return; }
      if (!dialog.current?.open) deadline.current = Date.now() + idleMs;
    }
    const events = ["pointerdown", "keydown", "wheel", "touchstart"] as const;
    events.forEach(event => window.addEventListener(event, activity, { passive: true }));
    document.addEventListener("visibilitychange", check);
    window.addEventListener("focus", check);
    window.addEventListener("pageshow", check);
    const timer = window.setInterval(check, 1000);
    return () => {
      clearInterval(timer);
      events.forEach(event => window.removeEventListener(event, activity));
      document.removeEventListener("visibilitychange", check);
      window.removeEventListener("focus", check);
      window.removeEventListener("pageshow", check);
    };
  }, []);
  useEffect(() => {
    if (session > 0) {
      document.getElementById("main")?.focus({ preventScroll: true });
      window.scrollTo({ top: 0, behavior: "instant" });
    }
  }, [session]);

  return <>
    <a className="skip" href="#main">Skip to content</a>
    <div className="kiosk-banner">
      <p><strong>Shared kiosk</strong> · Your plan stays on this screen only. After 4 idle minutes, you have 1 minute to continue.</p>
      <button onClick={finish}>Finish visit</button>
    </div>
    <p className="sr-only" role="status">{notice}</p>
    <MuseumExplorer key={session} kiosk onFinish={finish} />
    <dialog ref={dialog} className="kiosk-dialog" aria-labelledby="idle-title" aria-describedby="idle-description"
      onKeyDown={event => {
        if (event.key !== "Tab") return;
        const buttons = dialog.current?.querySelectorAll<HTMLButtonElement>("button");
        if (!buttons?.length) return;
        const first = buttons[0], last = buttons[buttons.length - 1];
        if (event.shiftKey && document.activeElement === first) {
          event.preventDefault(); last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault(); first.focus();
        }
      }}
      onCancel={event => { event.preventDefault(); keepGoing(); }}>
      <h2 id="idle-title">Would you like more time?</h2>
      <p id="idle-description">This shared screen will clear your visit shortly. Choose Continue my visit to keep your plan and get another five minutes. You can extend as often as you need.</p>
      <p role="timer" aria-live="off">{remaining} seconds remaining</p>
      <div className="dialog-actions">
        <button autoFocus className="save-button" onClick={keepGoing}>Continue my visit</button>
        <button onClick={finish}>Finish visit</button>
      </div>
    </dialog>
  </>;
}
