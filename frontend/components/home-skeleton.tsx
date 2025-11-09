import { Card, CardContent } from "@/components/ui/card"

// Skeleton Components balanced for both themes
const SkeletonCard = ({ className = "", children }: { className?: string; children?: React.ReactNode }) => (
  <div className={`animate-pulse bg-zinc-100 dark:bg-zinc-800 border border-zinc-200 dark:border-zinc-700 rounded-lg shadow-sm ${className}`}>
    {children}
  </div>
)

const SkeletonAvatar = ({ size = "w-10 h-10" }: { size?: string }) => (
  <div className={`${size} bg-zinc-200 dark:bg-zinc-700 border border-zinc-300 dark:border-zinc-600 rounded-full animate-pulse`} />
)

const SkeletonText = ({ width = "w-full", height = "h-4" }: { width?: string; height?: string }) => (
  <div className={`${width} ${height} bg-zinc-200 dark:bg-zinc-700 rounded animate-pulse`} />
)

const SkeletonButton = ({ width = "w-20", height = "h-8" }: { width?: string; height?: string }) => (
  <div className={`${width} ${height} bg-zinc-200 dark:bg-zinc-700 border border-zinc-300 dark:border-zinc-600 rounded-md animate-pulse`} />
)

export const HomePageSkeleton = () => (
  <div className="min-h-screen bg-background text-foreground pb-32">
    {/* Header Skeleton */}
    <header className="sticky top-0 z-40 bg-background/80 backdrop-blur-lg">
      <div className="container flex items-center justify-between h-20">
        <SkeletonAvatar />
        <div className="flex items-center gap-2">
          <SkeletonButton width="w-10" height="h-10" />
          <SkeletonButton width="w-10" height="h-10" />
          <SkeletonButton width="w-10" height="h-10" />
        </div>
      </div>
    </header>

    <main>
      <div className="container">
        <div className="space-y-8">
          {/* Welcome Section Skeleton */}
          <div className="space-y-4">
            <SkeletonText width="w-48" height="h-8" />
            <SkeletonText width="w-64" height="h-5" />
          </div>

          {/* Quick Filters Skeleton */}
          <div className="space-y-4">
            <SkeletonText width="w-32" height="h-6" />
            <div className="flex gap-2 overflow-x-auto pb-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <SkeletonButton key={i} width="w-24" height="h-8" />
              ))}
            </div>
          </div>

          {/* Upcoming Games Skeleton */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <SkeletonText width="w-40" height="h-6" />
              <SkeletonText width="w-16" height="h-5" />
            </div>
            <div className="flex gap-4 overflow-x-auto pb-4">
              {Array.from({ length: 3 }).map((_, i) => (
                <SkeletonCard key={i} className="shrink-0 w-[280px] h-[200px] p-4">
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <SkeletonText width="w-16" height="h-5" />
                      <SkeletonText width="w-20" height="h-5" />
                    </div>
                    <SkeletonText width="w-32" height="h-6" />
                    <SkeletonText width="w-24" height="h-4" />
                    <div className="flex items-center gap-2 mt-4">
                      {Array.from({ length: 3 }).map((_, j) => (
                        <SkeletonAvatar key={j} size="w-6 h-6" />
                      ))}
                    </div>
                  </div>
                </SkeletonCard>
              ))}
            </div>
          </div>

          {/* Available Games Skeleton */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <SkeletonText width="w-40" height="h-6" />
              <SkeletonText width="w-16" height="h-5" />
            </div>
            <div className="space-y-3">
              {Array.from({ length: 2 }).map((_, i) => (
                <SkeletonCard key={i} className="h-20 p-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-4">
                      <div className="text-center space-y-1">
                        <SkeletonText width="w-8" height="h-6" />
                        <SkeletonText width="w-6" height="h-3" />
                      </div>
                      <div className="space-y-2">
                        <SkeletonText width="w-32" height="h-5" />
                        <SkeletonText width="w-40" height="h-4" />
                      </div>
                    </div>
                    <div className="w-10 h-10 bg-muted/60 rounded-full animate-pulse" />
                  </div>
                </SkeletonCard>
              ))}
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
)
