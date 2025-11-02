import Link from "next/link"
import { Button } from "@/components/ui/button"
import { AlertTriangle } from "lucide-react"
import Header from "@/components/header"

export default function JoinGameNotFound() {
  return (
    <div className="min-h-screen bg-gray-50">
      <Header />
      <main className="container mx-auto px-4 py-6">
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <AlertTriangle className="mb-4 h-12 w-12 text-amber-500" />
          <h2 className="mb-2 text-xl font-bold">Partie introuvable</h2>
          <p className="mb-6 text-gray-600">Cette partie n'existe pas ou a été supprimée</p>
          <div className="flex gap-4">
            <Link href="/create-game">
              <Button variant="outline">Rechercher des parties</Button>
            </Link>
            <Link href="/">
              <Button>Retour à l'accueil</Button>
            </Link>
          </div>
        </div>
      </main>
    </div>
  )
}
