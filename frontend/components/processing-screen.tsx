"use client"

import { motion } from "framer-motion"

interface ProcessingScreenProps {
  message?: string
}

export function ProcessingScreen({ message = "Nous créons votre partie..." }: ProcessingScreenProps) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-white"
    >
      <div className="flex flex-col items-center justify-center gap-4">
        <div className="relative h-16 w-16">
          <motion.div
            className="absolute inset-0 rounded-full border-4 border-t-primary border-r-transparent border-b-transparent border-l-transparent"
            animate={{ rotate: 360 }}
            transition={{ duration: 1, repeat: Number.POSITIVE_INFINITY, ease: "linear" }}
          />
        </div>
        <p className="text-lg font-medium text-gray-800">{message}</p>
      </div>
    </motion.div>
  )
}
