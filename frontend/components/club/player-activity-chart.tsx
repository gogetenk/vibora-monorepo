"use client"

import { useEffect, useState } from "react"
import { Bar, BarChart, ResponsiveContainer, XAxis, YAxis, Tooltip } from "recharts"

// Données fictives pour le graphique d'activité
const generateActivityData = (playerId: string) => {
  const months = ["Jan", "Fév", "Mar", "Avr", "Mai", "Juin"]
  return months.map((month) => ({
    name: month,
    parties: Math.floor(Math.random() * 8) + 1,
  }))
}

export function PlayerActivityChart({ playerId }: { playerId: string }) {
  const [data, setData] = useState<any[]>([])

  useEffect(() => {
    // Dans une application réelle, vous feriez un appel API ici
    setData(generateActivityData(playerId))
  }, [playerId])

  return (
    <div className="h-[300px] w-full">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={data}>
          <XAxis dataKey="name" />
          <YAxis />
          <Tooltip
            formatter={(value) => [`${value} parties`, "Nombre de parties"]}
            labelFormatter={(label) => `${label} 2023`}
          />
          <Bar dataKey="parties" fill="#3b82f6" radius={[4, 4, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
