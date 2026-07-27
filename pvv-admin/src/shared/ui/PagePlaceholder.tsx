interface PagePlaceholderProps {
  title: string
  description: string
}

/** Simple stand-in for screens whose editors land in a later phase. */
export function PagePlaceholder({ title, description }: PagePlaceholderProps) {
  return (
    <div className="rounded-xl border border-dashed border-stone-300 bg-white p-10 text-center">
      <h1 className="text-2xl font-semibold tracking-tight text-stone-900">{title}</h1>
      <p className="mx-auto mt-2 max-w-md text-sm text-stone-500">{description}</p>
    </div>
  )
}
