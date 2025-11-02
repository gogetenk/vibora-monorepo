"use client"

import { useState } from "react"
import { motion } from "framer-motion"
import { Bell, Clock, Trophy, Megaphone, Heart, ChevronRight } from "lucide-react"
import { Switch } from "@/components/ui/switch"
import Header from "@/components/header"
import Link from "next/link"

const notificationSettings = [
  {
    id: "match-reminders",
    title: "Rappels de match",
    description: "Notifications avant vos parties",
    icon: Clock,
    color: "text-blue-400",
    defaultValue: true,
  },
  {
    id: "tournaments",
    title: "Tournois & Événements",
    description: "Nouveaux tournois et événements",
    icon: Trophy,
    color: "text-yellow-400",
    defaultValue: true,
  },
  {
    id: "promotions",
    title: "Annonces / Promos club",
    description: "Offres spéciales et promotions",
    icon: Megaphone,
    color: "text-purple-400",
    defaultValue: false,
  },
  {
    id: "favorite-slots",
    title: "Créneaux favoris disponibles",
    description: "Quand vos créneaux préférés se libèrent",
    icon: Heart,
    color: "text-pink-400",
    defaultValue: true,
  },
]

export default function NotificationsPage() {
  const [settings, setSettings] = useState(
    notificationSettings.reduce(
      (acc, setting) => ({
        ...acc,
        [setting.id]: setting.defaultValue,
      }),
      {},
    ),
  )

  const handleToggle = (id: string, value: boolean) => {
    setSettings((prev) => ({ ...prev, [id]: value }))
  }

  return (
    <div className="min-h-screen bg-black">
      <Header title="Notifications" back />

      <div className="px-4 py-6 space-y-6">
        {/* Header Info */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-gray-900/50 backdrop-blur-xs rounded-2xl p-6 border border-gray-800"
        >
          <div className="flex items-center space-x-4 mb-4">
            <div className="w-12 h-12 bg-gray-800 rounded-xl flex items-center justify-center">
              <Bell className="w-6 h-6 text-[#00E26D]" />
            </div>
            <div>
              <h2 className="text-xl font-semibold text-white">Préférences de notification</h2>
              <p className="text-gray-400">Gérez vos notifications pour rester informé</p>
            </div>
          </div>
        </motion.div>

        {/* Notification Settings */}
        <div className="space-y-3">
          {notificationSettings.map((setting, index) => (
            <motion.div
              key={setting.id}
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: index * 0.1 }}
              className="bg-gray-900/50 backdrop-blur-xs rounded-xl p-4 border border-gray-800"
            >
              <div className="flex items-center space-x-4">
                <div className="w-12 h-12 bg-gray-800 rounded-xl flex items-center justify-center">
                  <setting.icon className={`w-5 h-5 ${setting.color}`} />
                </div>
                <div className="flex-1">
                  <h3 className="font-semibold text-white">{setting.title}</h3>
                  <p className="text-sm text-gray-400">{setting.description}</p>
                </div>
                <Switch
                  checked={settings[setting.id]}
                  onCheckedChange={(value) => handleToggle(setting.id, value)}
                  className="data-[state=checked]:bg-[#00E26D]"
                />
              </div>
            </motion.div>
          ))}
        </div>

        {/* Favorite Slots Configuration */}
        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.5 }}>
          <Link href="/settings/favorite-slots">
            <div className="bg-gray-900/50 backdrop-blur-xs rounded-xl p-4 border border-gray-800 hover:border-gray-700 transition-all duration-200 group">
              <div className="flex items-center space-x-4">
                <div className="w-12 h-12 bg-gray-800 rounded-xl flex items-center justify-center group-hover:bg-gray-700 transition-colors">
                  <Heart className="w-5 h-5 text-[#00E26D]" />
                </div>
                <div className="flex-1">
                  <h3 className="font-semibold text-white">Configurer mes créneaux favoris</h3>
                  <p className="text-sm text-gray-400">Définir vos préférences d'horaires</p>
                </div>
                <ChevronRight className="w-5 h-5 text-gray-500 group-hover:text-gray-300 transition-colors" />
              </div>
            </div>
          </Link>
        </motion.div>

        {/* Push Notification Info */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.6 }}
          className="bg-blue-900/20 backdrop-blur-xs rounded-2xl p-6 border border-blue-800/30"
        >
          <h3 className="text-lg font-semibold text-white mb-2">Notifications push</h3>
          <p className="text-gray-400 text-sm mb-4">
            Assurez-vous d'avoir activé les notifications push dans les paramètres de votre appareil pour recevoir
            toutes les alertes importantes.
          </p>
          <div className="flex items-center space-x-2">
            <div className="w-2 h-2 bg-[#00E26D] rounded-full"></div>
            <span className="text-[#00E26D] text-sm font-medium">Notifications activées</span>
          </div>
        </motion.div>
      </div>
    </div>
  )
}
