import { Skeleton } from "@/components/ui/skeleton"

export default function YieldManagementLoading() {
  return (
    <div className="container mx-auto py-8 max-w-6xl">
      <div className="mb-8">
        <Skeleton className="h-10 w-64 mb-2" />
        <Skeleton className="h-4 w-full max-w-3xl" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-1">
          <Skeleton className="h-[500px] w-full rounded-lg" />
          <Skeleton className="h-[200px] w-full rounded-lg mt-6" />
        </div>

        <div className="lg:col-span-2">
          <Skeleton className="h-[700px] w-full rounded-lg" />
        </div>
      </div>
    </div>
  )
}
