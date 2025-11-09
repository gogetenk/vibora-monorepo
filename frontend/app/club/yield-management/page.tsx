"use client"

import { useState } from "react"
import { motion } from "framer-motion"
import { Slider } from "@/components/ui/slider"
import { Switch } from "@/components/ui/switch"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Info, Save, TrendingDown, Clock, Euro, Calendar, Users } from "lucide-react"
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip"

export default function YieldManagementPage() {
  // États pour les paramètres de tarification dynamique
  const [discountThreshold, setDiscountThreshold] = useState(40)
  const [maxDiscount, setMaxDiscount] = useState(30)
  const [timeBeforeDiscount, setTimeBeforeDiscount] = useState(48)
  const [isActive, setIsActive] = useState(true)
  const [selectedDay, setSelectedDay] = useState("monday")
  const [previewMode, setPreviewMode] = useState("week")

  // Données simulées pour la visualisation
  const weekdayOccupancy = {
    monday: [25, 30, 45, 60, 75, 85, 90, 85, 70, 50, 30],
    tuesday: [20, 25, 40, 55, 70, 80, 85, 80, 65, 45, 25],
    wednesday: [30, 35, 50, 65, 80, 90, 95, 90, 75, 55, 35],
    thursday: [35, 40, 55, 70, 85, 95, 100, 95, 80, 60, 40],
    friday: [40, 45, 60, 75, 90, 100, 100, 95, 85, 65, 45],
    saturday: [60, 70, 85, 95, 100, 100, 100, 100, 90, 80, 65],
    sunday: [50, 60, 75, 85, 95, 100, 100, 95, 85, 70, 55],
  }

  const timeSlots = ["9h", "10h", "11h", "12h", "13h", "14h", "15h", "16h", "17h", "18h", "19h"]

  // Calcul des prix dynamiques
  const calculateDynamicPrice = (basePrice, occupancyRate) => {
    if (occupancyRate >= discountThreshold) return basePrice

    const discountPercentage = Math.min(maxDiscount, maxDiscount * (1 - occupancyRate / discountThreshold))

    return Math.round(basePrice * (1 - discountPercentage / 100))
  }

  // Animation variants
  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.1,
      },
    },
  }

  const itemVariants = {
    hidden: { y: 20, opacity: 0 },
    visible: {
      y: 0,
      opacity: 1,
      transition: { type: "spring", stiffness: 100 },
    },
  }

  return (
    <div className="container mx-auto py-8 max-w-6xl">
      <motion.div
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
        className="mb-8"
      >
        <h1 className="text-3xl font-bold mb-2">Yield Management</h1>
        <p className="text-gray-600 max-w-3xl">
          Optimisez automatiquement vos tarifs en fonction du taux d'occupation. Le système ajuste les prix des créneaux
          les moins réservés pour maximiser votre chiffre d'affaires et l'utilisation de vos terrains.
        </p>
      </motion.div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Panneau de configuration */}
        <motion.div className="lg:col-span-1" variants={containerVariants} initial="hidden" animate="visible">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <span>Configuration</span>
                <div className="flex items-center space-x-2">
                  <span className="text-sm text-gray-500">{isActive ? "Activé" : "Désactivé"}</span>
                  <Switch
                    checked={isActive}
                    onCheckedChange={setIsActive}
                    className="data-[state=checked]:bg-green-500"
                  />
                </div>
              </CardTitle>
              <CardDescription>Définissez les paramètres de votre tarification dynamique</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <motion.div variants={itemVariants} className="space-y-3">
                <div className="flex justify-between items-center">
                  <div>
                    <label className="text-sm font-medium">Seuil d'occupation</label>
                    <div className="text-xs text-gray-500 flex items-center">
                      Appliquer des réductions en dessous de ce taux
                      <TooltipProvider>
                        <Tooltip>
                          <TooltipTrigger>
                            <Info className="h-3 w-3 ml-1 text-gray-400" />
                          </TooltipTrigger>
                          <TooltipContent>
                            <p className="w-80">
                              Si l'occupation est inférieure à ce seuil, le système commencera à appliquer des
                              réductions. Par exemple, avec un seuil de 40%, tous les créneaux avec moins de 40%
                              d'occupation verront leur prix réduit.
                            </p>
                          </TooltipContent>
                        </Tooltip>
                      </TooltipProvider>
                    </div>
                  </div>
                  <span className="font-medium">{discountThreshold}%</span>
                </div>
                <Slider
                  value={[discountThreshold]}
                  min={10}
                  max={80}
                  step={5}
                  onValueChange={(value) => setDiscountThreshold(value[0])}
                />
                <div className="flex justify-between text-xs text-gray-500">
                  <span>10%</span>
                  <span>80%</span>
                </div>
              </motion.div>

              <motion.div variants={itemVariants} className="space-y-3">
                <div className="flex justify-between items-center">
                  <div>
                    <label className="text-sm font-medium">Réduction maximale</label>
                    <div className="text-xs text-gray-500 flex items-center">
                      Pourcentage maximal de réduction sur le prix de base
                      <TooltipProvider>
                        <Tooltip>
                          <TooltipTrigger>
                            <Info className="h-3 w-3 ml-1 text-gray-400" />
                          </TooltipTrigger>
                          <TooltipContent>
                            <p className="w-80">
                              C'est la réduction maximale qui sera appliquée aux créneaux les moins occupés. Par
                              exemple, avec une réduction max de 30%, le prix ne sera jamais réduit de plus de 30%.
                            </p>
                          </TooltipContent>
                        </Tooltip>
                      </TooltipProvider>
                    </div>
                  </div>
                  <span className="font-medium">{maxDiscount}%</span>
                </div>
                <Slider
                  value={[maxDiscount]}
                  min={5}
                  max={50}
                  step={5}
                  onValueChange={(value) => setMaxDiscount(value[0])}
                />
                <div className="flex justify-between text-xs text-gray-500">
                  <span>5%</span>
                  <span>50%</span>
                </div>
              </motion.div>

              <motion.div variants={itemVariants} className="space-y-3">
                <div className="flex justify-between items-center">
                  <div>
                    <label className="text-sm font-medium">Délai avant réduction</label>
                    <div className="text-xs text-gray-500 flex items-center">
                      Heures avant le créneau pour appliquer la réduction
                      <TooltipProvider>
                        <Tooltip>
                          <TooltipTrigger>
                            <Info className="h-3 w-3 ml-1 text-gray-400" />
                          </TooltipTrigger>
                          <TooltipContent>
                            <p className="w-80">
                              Combien d'heures avant le créneau le système commencera à appliquer les réductions. Par
                              exemple, avec 48h, les réductions seront appliquées 2 jours avant le créneau.
                            </p>
                          </TooltipContent>
                        </Tooltip>
                      </TooltipProvider>
                    </div>
                  </div>
                  <span className="font-medium">{timeBeforeDiscount}h</span>
                </div>
                <Slider
                  value={[timeBeforeDiscount]}
                  min={12}
                  max={72}
                  step={12}
                  onValueChange={(value) => setTimeBeforeDiscount(value[0])}
                />
                <div className="flex justify-between text-xs text-gray-500">
                  <span>12h</span>
                  <span>72h</span>
                </div>
              </motion.div>
            </CardContent>
            <CardFooter>
              <Button className="w-full">
                <Save className="mr-2 h-4 w-4" />
                Enregistrer les paramètres
              </Button>
            </CardFooter>
          </Card>

          <motion.div variants={itemVariants} className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Exemple en direct</CardTitle>
                <CardDescription>Visualisez l'impact de vos paramètres sur un cas réel</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="p-4 border rounded-lg bg-gray-50">
                    <div className="flex items-center mb-2">
                      <Clock className="h-4 w-4 mr-2 text-gray-500" />
                      <span className="text-sm font-medium">Mardi, 11h00</span>
                    </div>
                    <div className="flex items-center mb-2">
                      <Users className="h-4 w-4 mr-2 text-gray-500" />
                      <span className="text-sm">
                        Taux d'occupation:{" "}
                        <span className="font-medium text-amber-600">{weekdayOccupancy[selectedDay][2]}%</span>
                      </span>
                    </div>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center">
                        <Euro className="h-4 w-4 mr-2 text-gray-500" />
                        <span className="text-sm">Prix de base:</span>
                      </div>
                      <span className="font-medium">20€</span>
                    </div>

                    <div className="mt-4 pt-4 border-t border-dashed">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center">
                          <TrendingDown className="h-4 w-4 mr-2 text-green-500" />
                          <span className="text-sm">Prix ajusté:</span>
                        </div>
                        <motion.div
                          initial={{ scale: 1 }}
                          animate={{ scale: [1, 1.1, 1] }}
                          transition={{ duration: 0.5, repeat: 0 }}
                        >
                          <span className="font-bold text-green-600">
                            {calculateDynamicPrice(20, weekdayOccupancy[selectedDay][2])}€
                          </span>
                        </motion.div>
                      </div>
                      <div className="text-xs text-gray-500 mt-1 flex justify-end">
                        <Badge variant="outline" className="text-green-600 bg-green-50">
                          -{Math.round(((20 - calculateDynamicPrice(20, weekdayOccupancy[selectedDay][2])) / 20) * 100)}
                          %
                        </Badge>
                      </div>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        </motion.div>

        {/* Visualisation */}
        <motion.div
          className="lg:col-span-2"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.3, duration: 0.5 }}
        >
          <Card className="h-full">
            <CardHeader>
              <CardTitle>Simulation de tarification</CardTitle>
              <CardDescription>Visualisez l'impact de votre stratégie de yield management</CardDescription>
              <Tabs defaultValue="week" onValueChange={setPreviewMode} className="mt-2">
                <TabsList>
                  <TabsTrigger value="week">Vue hebdomadaire</TabsTrigger>
                  <TabsTrigger value="day">Vue journalière</TabsTrigger>
                </TabsList>
              </Tabs>
            </CardHeader>
            <CardContent>
              {previewMode === "week" ? (
                <div className="space-y-6">
                  <div className="flex justify-between items-center">
                    <h3 className="text-sm font-medium">Taux d'occupation moyen par jour</h3>
                    <div className="flex items-center space-x-4">
                      <div className="flex items-center">
                        <div className="w-3 h-3 bg-blue-500 rounded-full mr-1"></div>
                        <span className="text-xs">Occupation</span>
                      </div>
                      <div className="flex items-center">
                        <div className="w-3 h-3 bg-green-500 rounded-full mr-1"></div>
                        <span className="text-xs">Prix ajusté</span>
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-7 gap-2">
                    {["Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim"].map((day, index) => {
                      const dayKey = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"][
                        index
                      ]
                      const avgOccupancy = Math.round(
                        weekdayOccupancy[dayKey].reduce((a, b) => a + b, 0) / weekdayOccupancy[dayKey].length,
                      )
                      const isSelected = dayKey === selectedDay

                      return (
                        <motion.div
                          key={day}
                          whileHover={{ y: -5 }}
                          whileTap={{ scale: 0.95 }}
                          onClick={() => setSelectedDay(dayKey)}
                          className={`cursor-pointer p-3 rounded-lg border ${isSelected ? "border-blue-500 bg-blue-50" : "border-gray-200"}`}
                        >
                          <div className="text-center mb-2 font-medium">{day}</div>
                          <div className="flex flex-col items-center">
                            <div className="w-full bg-gray-200 rounded-full h-2 mb-2">
                              <div className="bg-blue-500 h-2 rounded-full" style={{ width: `${avgOccupancy}%` }}></div>
                            </div>
                            <div className="text-xs">{avgOccupancy}%</div>
                            {avgOccupancy < discountThreshold && (
                              <Badge variant="outline" className="mt-2 text-green-600 bg-green-50 text-xs">
                                Jusqu'à -{maxDiscount}%
                              </Badge>
                            )}
                          </div>
                        </motion.div>
                      )
                    })}
                  </div>

                  <div className="mt-8">
                    <h3 className="text-sm font-medium mb-4">
                      Détail pour{" "}
                      {
                        {
                          monday: "Lundi",
                          tuesday: "Mardi",
                          wednesday: "Mercredi",
                          thursday: "Jeudi",
                          friday: "Vendredi",
                          saturday: "Samedi",
                          sunday: "Dimanche",
                        }[selectedDay]
                      }
                    </h3>

                    <div className="relative">
                      <div className="absolute left-0 top-0 bottom-0 w-px bg-gray-200 ml-10"></div>
                      <div className="grid grid-cols-11 gap-2">
                        {timeSlots.map((time, index) => {
                          const occupancy = weekdayOccupancy[selectedDay][index]
                          const basePrice = 20
                          const adjustedPrice = calculateDynamicPrice(basePrice, occupancy)
                          const hasDiscount = adjustedPrice < basePrice

                          return (
                            <motion.div
                              key={time}
                              initial={{ opacity: 0, y: 20 }}
                              animate={{ opacity: 1, y: 0 }}
                              transition={{ delay: index * 0.05, duration: 0.3 }}
                              className="flex flex-col items-center"
                            >
                              <div className="text-xs text-gray-500 mb-1">{time}</div>
                              <div className="h-32 w-full relative flex items-end justify-center">
                                <motion.div
                                  className="w-full bg-blue-100 rounded-t-sm relative"
                                  initial={{ height: 0 }}
                                  animate={{ height: `${occupancy}%` }}
                                  transition={{ duration: 0.5, delay: 0.3 + index * 0.05 }}
                                >
                                  <div
                                    className="absolute inset-0 bg-blue-500 opacity-50"
                                    style={{ height: `${occupancy}%` }}
                                  ></div>
                                </motion.div>
                              </div>
                              <div className="mt-2 flex flex-col items-center">
                                <div className="text-xs font-medium">{occupancy}%</div>
                                <div className="flex items-center mt-1">
                                  <span
                                    className={`text-xs ${hasDiscount ? "line-through text-gray-400" : "font-medium"}`}
                                  >
                                    {basePrice}€
                                  </span>
                                  {hasDiscount && (
                                    <motion.span
                                      className="ml-1 text-xs font-bold text-green-600"
                                      initial={{ scale: 1 }}
                                      animate={{ scale: [1, 1.1, 1] }}
                                      transition={{ duration: 0.5, delay: 0.5 + index * 0.05 }}
                                    >
                                      {adjustedPrice}€
                                    </motion.span>
                                  )}
                                </div>
                              </div>
                            </motion.div>
                          )
                        })}
                      </div>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="space-y-6">
                  <div className="flex justify-between">
                    <Button variant="outline" size="sm">
                      <Calendar className="h-4 w-4 mr-2" />
                      Choisir une date
                    </Button>
                    <div className="flex items-center space-x-2">
                      <Badge variant="outline" className="bg-blue-50">
                        <Calendar className="h-3 w-3 mr-1" />
                        Aujourd'hui
                      </Badge>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <Card>
                      <CardHeader className="pb-2">
                        <CardTitle className="text-base">Terrain 1</CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="space-y-2">
                          {[
                            { time: "9h - 10h", occupancy: 25, basePrice: 18 },
                            { time: "10h - 11h", occupancy: 30, basePrice: 20 },
                            { time: "11h - 12h", occupancy: 45, basePrice: 22 },
                            { time: "17h - 18h", occupancy: 70, basePrice: 25 },
                            { time: "18h - 19h", occupancy: 90, basePrice: 28 },
                          ].map((slot, index) => {
                            const adjustedPrice = calculateDynamicPrice(slot.basePrice, slot.occupancy)
                            const hasDiscount = adjustedPrice < slot.basePrice

                            return (
                              <motion.div
                                key={slot.time}
                                initial={{ opacity: 0, x: -20 }}
                                animate={{ opacity: 1, x: 0 }}
                                transition={{ delay: index * 0.1, duration: 0.3 }}
                                className="flex items-center justify-between p-2 rounded-lg border"
                              >
                                <div className="flex items-center">
                                  <Clock className="h-4 w-4 mr-2 text-gray-500" />
                                  <span className="text-sm">{slot.time}</span>
                                  {slot.occupancy < discountThreshold && (
                                    <Badge variant="outline" className="ml-2 bg-amber-50 text-amber-600 text-xs">
                                      {slot.occupancy}%
                                    </Badge>
                                  )}
                                </div>
                                <div className="flex items-center">
                                  {hasDiscount ? (
                                    <>
                                      <span className="text-sm line-through text-gray-400 mr-2">{slot.basePrice}€</span>
                                      <motion.span
                                        className="text-sm font-bold text-green-600"
                                        initial={{ scale: 1 }}
                                        animate={{ scale: [1, 1.1, 1] }}
                                        transition={{ duration: 0.5, delay: 0.5 + index * 0.1 }}
                                      >
                                        {adjustedPrice}€
                                      </motion.span>
                                    </>
                                  ) : (
                                    <span className="text-sm font-medium">{slot.basePrice}€</span>
                                  )}
                                </div>
                              </motion.div>
                            )
                          })}
                        </div>
                      </CardContent>
                    </Card>

                    <Card>
                      <CardHeader className="pb-2">
                        <CardTitle className="text-base">Terrain 2</CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="space-y-2">
                          {[
                            { time: "9h - 10h", occupancy: 20, basePrice: 18 },
                            { time: "10h - 11h", occupancy: 25, basePrice: 20 },
                            { time: "11h - 12h", occupancy: 40, basePrice: 22 },
                            { time: "17h - 18h", occupancy: 65, basePrice: 25 },
                            { time: "18h - 19h", occupancy: 85, basePrice: 28 },
                          ].map((slot, index) => {
                            const adjustedPrice = calculateDynamicPrice(slot.basePrice, slot.occupancy)
                            const hasDiscount = adjustedPrice < slot.basePrice

                            return (
                              <motion.div
                                key={slot.time}
                                initial={{ opacity: 0, x: -20 }}
                                animate={{ opacity: 1, x: 0 }}
                                transition={{ delay: index * 0.1, duration: 0.3 }}
                                className="flex items-center justify-between p-2 rounded-lg border"
                              >
                                <div className="flex items-center">
                                  <Clock className="h-4 w-4 mr-2 text-gray-500" />
                                  <span className="text-sm">{slot.time}</span>
                                  {slot.occupancy < discountThreshold && (
                                    <Badge variant="outline" className="ml-2 bg-amber-50 text-amber-600 text-xs">
                                      {slot.occupancy}%
                                    </Badge>
                                  )}
                                </div>
                                <div className="flex items-center">
                                  {hasDiscount ? (
                                    <>
                                      <span className="text-sm line-through text-gray-400 mr-2">{slot.basePrice}€</span>
                                      <motion.span
                                        className="text-sm font-bold text-green-600"
                                        initial={{ scale: 1 }}
                                        animate={{ scale: [1, 1.1, 1] }}
                                        transition={{ duration: 0.5, delay: 0.5 + index * 0.1 }}
                                      >
                                        {adjustedPrice}€
                                      </motion.span>
                                    </>
                                  ) : (
                                    <span className="text-sm font-medium">{slot.basePrice}€</span>
                                  )}
                                </div>
                              </motion.div>
                            )
                          })}
                        </div>
                      </CardContent>
                    </Card>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </motion.div>
      </div>
    </div>
  )
}
