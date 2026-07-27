/**
 * Manual reload. The analytics screens refresh themselves every few minutes,
 * which is right for a dashboard left open but far too slow while someone is
 * generating leads on the portal and wants to see them land.
 *
 * `busy` spins the icon, so the click has visible feedback even when the answer
 * comes back instantly.
 */
export function RefreshButton({ onClick, busy = false }: { onClick: () => void; busy?: boolean }) {
  return (
    <button
      type="button"
      onClick={onClick}
      title="Actualizar"
      aria-label="Actualizar"
      className="rounded-lg border border-stone-300 bg-white p-2 text-stone-600 transition hover:bg-stone-50"
    >
      <svg viewBox="0 0 24 24" fill="none" className={`h-5 w-5 ${busy ? 'animate-spin' : ''}`} aria-hidden>
        <path
          d="M20 12a8 8 0 1 1-2.34-5.66M20 4v4h-4"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    </button>
  )
}
