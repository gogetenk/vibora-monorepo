"use client"

import { useState } from "react"

import type React from "react"
import { motion, AnimatePresence } from "framer-motion"
import { Plus, Edit, Trash2, Clock, Calendar } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import Header from "@/components/header"

interface TimeSlot {
  id: string
  day: string
  timeRange: string
  startTime: string
  endTime: string
}

const initialSlots: TimeSlot[] = [
  { id: "1", day: "Lundi", timeRange: "18h - 20h", startTime: "18h00", endTime: "20h00" },
  { id: "2", day: "Mercredi", timeRange: "19h - 21h", startTime: "19h00", endTime: "21h00" },
  { id: "3", day: "Samedi", timeRange: "Matin", startTime: "9h00", endTime: "12h00" },
]

const days = ["Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche"]

export default function FavoriteSlotsPage() {
  const [slots, setSlots] = useState<TimeSlot[]>(initialSlots)
  const [showAddForm, setShowAddForm] = useState(false)
  const [editingSlot, setEditingSlot] = useState<TimeSlot | null>(null)
  const [formData, setFormData] = useState({
    day: "",
    startTime: "",
    endTime: "",
  })

  const handleEdit = (slot: TimeSlot) => {
    setEditingSlot(slot)
    setFormData({
      day: slot.day,
      startTime: slot.startTime,
      endTime: slot.endTime,
    })
    setShowAddForm(true)
  }

  const handleDelete = (id: string) => {
    setSlots(slots.filter((slot) => slot.id !== id))
  }

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault()
    if (!formData.day || !formData.startTime || !formData.endTime) return

    const newSlot = {
      id: editingSlot?.id || Date.now().toString(),
      day: formData.day,
      startTime: formData.startTime,
      endTime: formData.endTime,
      timeRange: `${formData.startTime} - ${formData.endTime}`,
    }

    if (editingSlot) {
      setSlots(slots.map((slot) => (slot.id === editingSlot.id ? newSlot : slot)))
    } else {
      setSlots([...slots, newSlot])
    }

    setShowAddForm(false)
    setEditingSlot(null)
    setFormData({ day: "", startTime: "", endTime: "" })
  }

  const handleClose = () => {
    setShowAddForm(false)
    setEditingSlot(null)
    setFormData({ day: "", startTime: "", endTime: "" })
  }

  return (
    <div className="min-h-screen bg-black">
      <Header title="Créneaux favoris" back />

      <div className="px-4 py-6 space-y-6">
        {/* Header Info */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-gray-900/50 backdrop-blur-xs rounded-2xl p-6 border border-gray-800"
        >
          <div className="flex items-center space-x-4 mb-4">
            <div className="w-12 h-12 bg-gray-800 rounded-xl flex items-center justify-center">
              <Clock className="w-6 h-6 text-pink-400" />
            </div>
            <div>
              <h2 className="text-xl font-semibold text-white">Créneaux favoris</h2>
              <p className="text-gray-400">Soyez alerté quand vos créneaux préférés se libèrent</p>
            </div>
          </div>
        </motion.div>

        {/* Add Button */}
        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1 }}>
          <Button
            onClick={() => setShowAddForm(true)}
            className="w-full bg-[#00E26D] hover:bg-[#00E26D]/80 text-black h-12 font-medium"
          >
            <Plus className="w-5 h-5 mr-2" />
            Ajouter un créneau favori
          </Button>
        </motion.div>

        {/* Slots List */}
        <div className="space-y-3">
          <AnimatePresence>
            {slots.map((slot, index) => (
              <motion.div
                key={slot.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, x: -100 }}
                transition={{ delay: index * 0.1 }}
                className="bg-gray-900/50 backdrop-blur-xs rounded-xl p-4 border border-gray-800"
              >
                <div className="flex items-center space-x-4">
                  <div className="w-12 h-12 bg-gray-800 rounded-xl flex items-center justify-center">
                    <Calendar className="w-5 h-5 text-blue-400" />
                  </div>
                  <div className="flex-1">
                    <h3 className="font-semibold text-white">{slot.day}</h3>
                    <p className="text-sm text-gray-400">{slot.timeRange}</p>
                  </div>
                  <div className="flex space-x-2">
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => handleEdit(slot)}
                      className="text-gray-400 hover:text-white hover:bg-gray-800"
                    >
                      <Edit className="w-4 h-4" />
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => handleDelete(slot.id)}
                      className="text-gray-400 hover:text-red-400 hover:bg-red-900/30"
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              </motion.div>
            ))}
          </AnimatePresence>

          {slots.length === 0 && (
            <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="text-center py-12">
              <Clock className="w-12 h-12 text-gray-600 mx-auto mb-4" />
              <p className="text-gray-400 text-lg">Aucun créneau favori défini</p>
              <p className="text-gray-500 text-sm mt-2">Ajoutez vos créneaux préférés pour être alerté</p>
            </motion.div>
          )}
        </div>
      </div>

      {/* Add/Edit Form Modal */}
      <Dialog open={showAddForm} onOpenChange={handleClose}>
        <DialogContent className="bg-gray-900 border-gray-800 text-white">
          <DialogHeader>
            <DialogTitle>{editingSlot ? "Modifier un créneau favori" : "Ajouter un créneau favori"}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSave} className="space-y-4">
            <div>
              <Label htmlFor="day">Jour de la semaine</Label>
              <Select value={formData.day} onValueChange={(value) => setFormData({ ...formData, day: value })}>
                <SelectTrigger className="bg-gray-800 border-gray-700 text-white">
                  <SelectValue placeholder="Sélectionner un jour" />
                </SelectTrigger>
                <SelectContent className="bg-gray-800 border-gray-700">
                  {days.map((day) => (
                    <SelectItem key={day} value={day}>
                      {day}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="startTime">Heure de début</Label>
                <Select
                  value={formData.startTime}
                  onValueChange={(value) => setFormData({ ...formData, startTime: value })}
                >
                  <SelectTrigger className="bg-gray-800 border-gray-700 text-white">
                    <SelectValue placeholder="Début" />
                  </SelectTrigger>
                  <SelectContent className="bg-gray-800 border-gray-700">
                    {Array.from({ length: 15 }, (_, i) => i + 7).map((hour) => (
                      <SelectItem key={hour} value={`${hour}h00`}>
                        {hour}h00
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div>
                <Label htmlFor="endTime">Heure de fin</Label>
                <Select
                  value={formData.endTime}
                  onValueChange={(value) => setFormData({ ...formData, endTime: value })}
                >
                  <SelectTrigger className="bg-gray-800 border-gray-700 text-white">
                    <SelectValue placeholder="Fin" />
                  </SelectTrigger>
                  <SelectContent className="bg-gray-800 border-gray-700">
                    {Array.from({ length: 15 }, (_, i) => i + 8).map((hour) => (
                      <SelectItem key={hour} value={`${hour}h00`}>
                        {hour}h00
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <DialogFooter>
              <Button type="button" variant="ghost" onClick={handleClose}>
                Annuler
              </Button>
              <Button type="submit" className="bg-[#00E26D] hover:bg-[#00E26D]/80 text-black font-medium">
                {editingSlot ? "Mettre à jour" : "Enregistrer"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}
