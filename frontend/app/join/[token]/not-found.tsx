import { motion } from "framer-motion"
import Link from "next/link"
import { AlertTriangle } from "lucide-react"
import { VPage, VMain, VStack } from "@/components/ui/vibora-layout"
import { Button } from "@/components/ui/button"

export default function MagicLinkNotFound() {
  return (
    <VPage animate={false}>
      <VMain>
        <VStack spacing="lg" align="center" className="min-h-[60vh] justify-center text-center">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="space-y-6"
          >
            <div className="w-16 h-16 mx-auto rounded-full bg-amber-100 flex items-center justify-center">
              <AlertTriangle className="w-8 h-8 text-amber-600" />
            </div>
            
            <div className="space-y-3">
              <h1 className="text-2xl font-bold">Invitation introuvable</h1>
              <p className="text-muted-foreground max-w-md">
                Cette invitation n'existe pas ou n'est plus valide. Contactez la personne qui vous a invité pour obtenir un nouveau lien.
              </p>
            </div>

            <VStack spacing="sm" className="max-w-sm mx-auto">
              <Link href="/" className="w-full">
                <Button className="w-full">
                  Découvrir Vibora
                </Button>
              </Link>
              <Button 
                variant="outline" 
                onClick={() => window.history.back()}
                className="w-full"
              >
                Retour
              </Button>
            </VStack>
          </motion.div>
        </VStack>
      </VMain>
    </VPage>
  )
}