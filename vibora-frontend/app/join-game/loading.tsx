import Header from "@/components/header"

export default function JoinGameLoading() {
  return (
    <div className="min-h-screen bg-gray-50">
      <Header />
      <main className="container mx-auto px-4 py-6">
        <div className="mb-6 flex items-center gap-2">
          <div className="h-8 w-8 rounded-md bg-gray-200 animate-pulse"></div>
          <div className="h-8 w-48 rounded-md bg-gray-200 animate-pulse"></div>
        </div>

        <div className="mx-auto max-w-2xl">
          <div className="rounded-lg border border-gray-200 bg-white shadow-md overflow-hidden">
            <div className="border-b p-4">
              <div className="flex items-center justify-between">
                <div className="flex gap-2">
                  <div className="h-6 w-24 rounded-full bg-gray-200 animate-pulse"></div>
                  <div className="h-6 w-20 rounded-full bg-gray-200 animate-pulse"></div>
                </div>
                <div className="h-8 w-8 rounded-md bg-gray-200 animate-pulse"></div>
              </div>
            </div>

            <div className="p-6 space-y-6">
              <div className="space-y-4">
                <div className="h-7 w-48 rounded-md bg-gray-200 animate-pulse"></div>
                <div className="h-5 w-32 rounded-md bg-gray-200 animate-pulse"></div>

                <div className="rounded-lg bg-gray-100 p-4 space-y-3">
                  {[1, 2, 3, 4].map((i) => (
                    <div key={i} className="flex items-center gap-2">
                      <div className="h-4 w-4 rounded-full bg-gray-200 animate-pulse"></div>
                      <div className="h-4 w-full rounded-md bg-gray-200 animate-pulse"></div>
                    </div>
                  ))}
                </div>

                <div className="space-y-2">
                  <div className="h-5 w-32 rounded-md bg-gray-200 animate-pulse"></div>
                  <div className="h-16 w-full rounded-md bg-gray-200 animate-pulse"></div>
                </div>

                <div className="space-y-2">
                  <div className="h-5 w-40 rounded-md bg-gray-200 animate-pulse"></div>
                  <div className="flex flex-wrap gap-2">
                    {[1, 2, 3, 4].map((i) => (
                      <div key={i} className="h-6 w-20 rounded-full bg-gray-200 animate-pulse"></div>
                    ))}
                  </div>
                </div>
              </div>

              <div className="h-px w-full bg-gray-200"></div>

              <div className="space-y-4">
                <div className="h-6 w-36 rounded-md bg-gray-200 animate-pulse"></div>

                {[1, 2].map((i) => (
                  <div key={i} className="flex items-center justify-between rounded-lg border p-3">
                    <div className="flex items-center gap-3">
                      <div className="h-10 w-10 rounded-full bg-gray-200 animate-pulse"></div>
                      <div className="space-y-2">
                        <div className="h-4 w-32 rounded-md bg-gray-200 animate-pulse"></div>
                        <div className="h-3 w-24 rounded-md bg-gray-200 animate-pulse"></div>
                      </div>
                    </div>
                    <div className="h-6 w-16 rounded-full bg-gray-200 animate-pulse"></div>
                  </div>
                ))}

                {[1, 2].map((i) => (
                  <div
                    key={i}
                    className="flex items-center rounded-lg border border-dashed border-gray-300 bg-gray-50 p-3"
                  >
                    <div className="flex items-center gap-3">
                      <div className="h-10 w-10 rounded-full bg-gray-200 animate-pulse"></div>
                      <div className="h-4 w-32 rounded-md bg-gray-200 animate-pulse"></div>
                    </div>
                  </div>
                ))}
              </div>

              <div className="rounded-lg bg-gray-100 p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <div className="h-5 w-5 rounded-full bg-gray-200 animate-pulse"></div>
                    <div className="h-5 w-32 rounded-md bg-gray-200 animate-pulse"></div>
                  </div>
                  <div className="h-7 w-16 rounded-md bg-gray-200 animate-pulse"></div>
                </div>
                <div className="h-10 w-full rounded-md bg-gray-200 animate-pulse"></div>
              </div>
            </div>

            <div className="border-t bg-gray-50 p-6 space-y-3">
              <div className="h-12 w-full rounded-lg bg-gray-200 animate-pulse"></div>
              <div className="h-10 w-full rounded-lg bg-gray-200 animate-pulse"></div>
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}
