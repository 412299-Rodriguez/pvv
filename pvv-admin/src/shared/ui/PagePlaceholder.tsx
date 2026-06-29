interface PagePlaceholderProps {
  title: string
  description: string
}

/** Simple stand-in for screens whose editors land in a later phase. */
export function PagePlaceholder({ title, description }: PagePlaceholderProps) {
  return (
    <div className="rounded-xl border border-dashed border-slate-300 bg-white p-10 text-center">
      <h1 className="text-2xl font-bold text-slate-800">{title}</h1>
      <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">{description}</p>
    </div>
  )
}
