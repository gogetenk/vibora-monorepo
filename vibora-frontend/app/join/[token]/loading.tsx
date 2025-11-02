import { motion } from "framer-motion"
import { VPage, VMain } from "@/components/ui/vibora-layout"

export default function MagicLinkLoading() {
  return (
    <VPage animate={false}>
      <VMain containerized={false}>
        <div className="min-h-screen flex items-center justify-center">
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            className="text-center space-y-4"
          >
            <div className="w-12 h-12 mx-auto rounded-full bg-primary/20 flex items-center justify-center">
              <div className="w-6 h-6 border-2 border-primary border-t-transparent rounded-full animate-spin" />
            </div>
            <div className="space-y-2">
              <h2 className="text-lg font-semibold">Chargement de l'invitation...</h2>
              <p className="text-sm text-muted-foreground">Vérification du lien en cours</p>
            </div>
          </motion.div>
        </div>
      </VMain>
    </VPage>
  )
}