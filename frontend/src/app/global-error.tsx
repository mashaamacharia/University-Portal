"use client";

import "./globals.css";

/** Last resort: replaces the whole page if the root layout itself fails. */
export default function GlobalError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <html lang="en">
      <body className="grid min-h-screen place-items-center bg-surface px-4 text-ink">
        <div className="max-w-sm text-center">
          <h1 className="font-serif text-3xl font-semibold text-navy-900">Something went wrong</h1>
          <p className="mt-2 text-muted">The portal hit an unexpected problem. Please try again.</p>
          <button
            type="button"
            onClick={reset}
            className="mt-6 h-10 rounded-lg bg-navy-900 px-4 text-sm font-medium text-white hover:bg-navy-800"
          >
            Try again
          </button>
        </div>
      </body>
    </html>
  );
}
