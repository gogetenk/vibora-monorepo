"use client"

import React from "react"
import { motion } from "framer-motion"
import { ArrowLeft, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { VLocationInput } from "@/components/ui/vibora-form"

interface CreateGameFormProps {
  onBack: () => void;
  newGameClubName: string;
  setNewGameClubName: (value: string) => void;
  newGameDate: string;
  setNewGameDate: (value: string) => void;
  newGameTime: string;
  setNewGameTime: (value: string) => void;
  newGameLevel: string;
  setNewGameLevel: (value: string) => void;
  newGamePrice: string;
  setNewGamePrice: (value: string) => void;
  handleCreateGame: () => void;
  isCreatingGame: boolean;
  hasCourt: boolean;
  setHasCourt: (value: boolean) => void;
}

export function CreateGameForm({ 
  onBack,
  newGameClubName,
  setNewGameClubName,
  newGameDate,
  setNewGameDate,
  newGameTime,
  setNewGameTime,
  newGameLevel,
  setNewGameLevel,
  newGamePrice,
  setNewGamePrice,
  handleCreateGame,
  isCreatingGame,
  hasCourt,
  setHasCourt
}: CreateGameFormProps) {
  return (
    <motion.div
      key="create"
      initial={{ opacity: 0, x: 50 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: -50 }}
      transition={{ duration: 0.3 }}
    >
      <div className="max-w-2xl mx-auto">
        <div className="flex items-center mb-6">
          <Button variant="ghost" size="icon" onClick={onBack}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <h2 className="text-2xl font-bold ml-2">Créer une partie</h2>
        </div>
        <Card className="bg-card/80 backdrop-blur-sm border-border/50">
          <CardContent className="p-6">
            <div className="space-y-6">
              <div className="flex items-center justify-between rounded-lg border p-4 bg-muted/30">
                <div className="space-y-0.5">
                    <Label htmlFor="has-court" className="text-base font-medium">
                        J'ai déjà réservé un court
                    </Label>
                    <p className="text-sm text-muted-foreground">
                        Activez si vous avez un club et un horaire précis.
                    </p>
                </div>
                <Switch
                    id="has-court"
                    checked={hasCourt}
                    onCheckedChange={setHasCourt}
                />
              </div>

                            {hasCourt ? (
                <motion.div
                  key="club-input"
                  initial={{ opacity: 0, y: -10 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -10 }}
                  transition={{ duration: 0.3, ease: 'easeInOut' }}
                >
                  <Label htmlFor="clubName">Nom du club</Label>
                  <VLocationInput
                    value={newGameClubName}
                    onChange={setNewGameClubName}
                    placeholder="Ex: Padel Club de Lyon"
                  />
                </motion.div>
              ) : (
                <motion.div
                  key="location-input"
                  initial={{ opacity: 0, y: -10 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -10 }}
                  transition={{ duration: 0.3, ease: 'easeInOut' }}
                >
                  <Label htmlFor="location">Secteur</Label>
                  <VLocationInput
                    value={newGameClubName}
                    onChange={setNewGameClubName}
                    placeholder="Ex: Lyon 8ème"
                  />
                </motion.div>
              )}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="date">Date</Label>
                  <Input
                    id="date"
                    type="date"
                    value={newGameDate}
                    onChange={(e) => setNewGameDate(e.target.value)}
                    className="bg-muted/50 border-border/50"
                  />
                </div>
                <div>
                  <Label htmlFor="time">Heure</Label>
                  <Input
                    id="time"
                    type="time"
                    value={newGameTime}
                    onChange={(e) => setNewGameTime(e.target.value)}
                    className="bg-muted/50 border-border/50"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="level">Niveau</Label>
                  <Select value={newGameLevel} onValueChange={setNewGameLevel}>
                    <SelectTrigger className="bg-muted/50 border-border/50">
                      <SelectValue placeholder="Niveau" />
                    </SelectTrigger>
                    <SelectContent>
                      {[...Array(8)].map((_, i) => (
                        <SelectItem key={i + 1} value={`${i + 1}`}>
                          Niveau {i + 1}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                                {hasCourt && (
                  <motion.div
                    key="price-input"
                    initial={{ opacity: 0, y: -10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -10 }}
                    transition={{ duration: 0.3, ease: 'easeInOut' }}
                  >
                    <Label htmlFor="price">Prix par personne</Label>
                    <Input
                      id="price"
                      type="number"
                      value={newGamePrice}
                      onChange={(e) => setNewGamePrice(e.target.value)}
                      placeholder="Ex: 7.5"
                      className="bg-muted/50 border-border/50"
                    />
                  </motion.div>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
        <div className="mt-6">
          <Button 
            className="w-full" 
            size="lg" 
            onClick={handleCreateGame}
            disabled={isCreatingGame}
          >
            {isCreatingGame ? (
              <><Loader2 className="mr-2 h-4 w-4 animate-spin" /> Création en cours...</>
            ) : (
              'Créer la partie'
            )}
          </Button>
        </div>
      </div>
    </motion.div>
  )
}
