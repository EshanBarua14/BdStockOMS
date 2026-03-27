export function AdminPlaceholderPage({ title }: { title: string }) {
  return (
    <div className="flex h-full items-center justify-center">
      <div className="text-center">
        <div className="mb-3 text-4xl">🚧</div>
        <h2 className="text-lg font-semibold text-[var(--t-text1)]">{title}</h2>
        <p className="mt-1 text-sm text-[var(--t-text3)]">Coming soon — Day 66</p>
      </div>
    </div>
  );
}
