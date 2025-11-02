"use client"

import { useState, useEffect } from "react"
import { useSearchParams, useRouter } from "next/navigation"
import { motion } from "framer-motion"
import { 
  CheckCircle, 
  Calendar, 
  Clock, 
  MapPin, 
  Users,
  Sparkles,
  Gift,
  Smartphone,
  User,
  Mail,
  ArrowRight,
  MessageCircle
} from "lucide-react"

// Vibora Design System imports
import { 
  VPage, 
  VHeader, 
  VMain, 
  VContainer,
  VStack,
  VContentCard,
  FADE_IN_ANIMATION_VARIANTS,
  STAGGER_CONTAINER_VARIANTS,
  SCALE_VARIANTS
} from "@/components/ui/vibora-layout"

import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { toast } from "@/components/ui/use-toast"

export default function JoinSuccessPage() {
  const searchParams = useSearchParams()
  const router = useRouter()
  
  const isGuest = searchParams.get('guest') === 'true'
  const playerName = searchParams.get('name') || 'Joueur'
  const gameId = searchParams.get('gameId')

  const [showConversionOffer, setShowConversionOffer] = useState(false)
  const [isCreatingAccount, setIsCreatingAccount] = useState(false)
  const [email, setEmail] = useState("")

  // Mock game data (in real app, would fetch from gameId)
  const gameData = {
    id: gameId,
    date: "2025-08-02", 
    time: "19:00",
    endTime: "20:30",
    club: {
      name: "Padel Club Boulogne",
      address: "15 Rue de la Paix, 92100 Boulogne"
    },
    creator: {
      name: "Marion Lefebvre"
    },
    players: [
      { name: "Marion Lefebvre" },
      { name: "Lucas Moreau" },
      { name: playerName }, // Le joueur qui vient de rejoindre
    ]
  }

  const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    const today = new Date()
    const tomorrow = new Date(today)
    tomorrow.setDate(tomorrow.getDate() + 1)

    if (date.toDateString() === today.toDateString()) {
      return "Aujourd'hui"
    } else if (date.toDateString() === tomorrow.toDateString()) {
      return "Demain"
    } else {
      return date.toLocaleDateString("fr-FR", { 
        weekday: "long", 
        day: "numeric", 
        month: "long" 
      })
    }
  }

  // Montrer l'offre de conversion après quelques secondes pour les invités
  useEffect(() => {
    if (isGuest) {
      const timer = setTimeout(() => {
        setShowConversionOffer(true)
      }, 3000) // 3 secondes pour laisser le temps de savourer le succès

      return () => clearTimeout(timer)
    }
  }, [isGuest])

  const handleCreateAccount = async () => {
    if (!email.trim()) {
      toast({
        title: "Email requis",
        description: "Veuillez saisir votre adresse email pour continuer",
        variant: "destructive"
      })
      return
    }

    setIsCreatingAccount(true)

    try {
      // Simuler la création de compte
      await new Promise(resolve => setTimeout(resolve, 1500))
      
      toast({
        title: "Compte créé avec succès ! 🎉",
        description: "Vous recevrez vos notifications de match par email",
      })

      // Redirection vers l'onboarding ou le profil
      router.push('/onboarding?from=magic-link')

    } catch (error) {
      toast({
        title: "Erreur",
        description: "Impossible de créer le compte pour le moment",
        variant: "destructive"
      })
    } finally {
      setIsCreatingAccount(false)
    }
  }

  const handleSkipForNow = () => {
    router.push('/')
  }

  const handleAddToCalendar = () => {
    // Créer un événement calendrier
    const startDate = new Date(`${gameData.date}T${gameData.time}:00`)
    const endDate = new Date(`${gameData.date}T${gameData.endTime}:00`)
    
    const googleCalendarUrl = `https://calendar.google.com/calendar/render?action=TEMPLATE&text=${encodeURIComponent('Partie Padel - ' + gameData.club.name)}&dates=${startDate.toISOString().replace(/[-:]/g, '').split('.')[0]}Z/${endDate.toISOString().replace(/[-:]/g, '').split('.')[0]}Z&details=${encodeURIComponent('Partie organisée via Vibora')}&location=${encodeURIComponent(gameData.club.address)}`
    
    window.open(googleCalendarUrl, '_blank')
  }

  const handleContactOrganizer = () => {
    // Simuler l'ouverture du chat ou des contacts
    toast({
      title: "Contact organisateur",
      description: `Un message a été envoyé à ${gameData.creator.name}`,
    })
  }

  return (
    <VPage>
      {/* Header success */}
      <VHeader sticky>
        <VContainer>
          <div className="flex items-center justify-center h-16">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-full bg-success/20 flex items-center justify-center">
                <CheckCircle className="w-4 h-4 text-success" />
              </div>
              <div>
                <h1 className="font-semibold text-sm">Participation confirmée</h1>
                <p className="text-xs text-muted-foreground">Partie du {formatDate(gameData.date)}</p>
              </div>
            </div>
          </div>
        </VContainer>
      </VHeader>

      <VMain>
        <VStack spacing="lg">
          {/* Confirmation Hero */}
          <motion.div
            variants={SCALE_VARIANTS}
            initial="hidden"
            animate="show"
          >
            <VContentCard variant="elevated" className="text-center overflow-hidden">
              <div className="p-8 space-y-6">
                <div className="w-20 h-20 mx-auto rounded-full bg-success/20 flex items-center justify-center">
                  <CheckCircle className="w-10 h-10 text-success" />
                </div>

                <div className="space-y-3">
                  <h1 className="text-2xl font-bold">C'est parti {playerName} ! 🎾</h1>
                  <p className="text-muted-foreground">
                    Votre place est réservée pour la partie de{" "}
                    <span className="font-medium text-foreground">{formatDate(gameData.date)}</span> à{" "}
                    <span className="font-medium text-foreground">{gameData.time}</span>
                  </p>
                </div>

                <div className="bg-success/5 rounded-lg p-4 space-y-2">
                  <div className="flex items-center justify-center gap-2">
                    <MapPin className="w-4 h-4 text-success" />
                    <span className="font-medium text-success">{gameData.club.name}</span>
                  </div>
                  <p className="text-sm text-muted-foreground">{gameData.club.address}</p>
                </div>
              </div>
            </VContentCard>
          </motion.div>

          {/* Actions rapides */}
          <motion.div
            variants={FADE_IN_ANIMATION_VARIANTS}
            initial="hidden"
            animate="show"
          >
            <VContentCard>
              <div className="p-6 space-y-4">
                <h3 className="font-semibold">Actions rapides</h3>
                
                <VStack spacing="sm">
                  <Button
                    onClick={handleAddToCalendar}
                    variant="outline"
                    className="w-full justify-start h-12"
                  >
                    <Calendar className="w-4 h-4 mr-3" />
                    Ajouter à mon calendrier
                  </Button>

                  <Button
                    onClick={handleContactOrganizer}
                    variant="outline"
                    className="w-full justify-start h-12"
                  >
                    <MessageCircle className="w-4 h-4 mr-3" />
                    Contacter {gameData.creator.name}
                  </Button>
                </VStack>
              </div>
            </VContentCard>
          </motion.div>

          {/* Joueurs confirmés */}
          <motion.div
            variants={FADE_IN_ANIMATION_VARIANTS}
            initial="hidden"
            animate="show"
          >
            <VContentCard>
              <div className="p-6 space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="font-semibold">Équipe formée</h3>
                  <Badge variant="outline" className="bg-success/10 text-success">
                    {gameData.players.length}/4 joueurs
                  </Badge>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  {gameData.players.map((player, index) => (
                    <div 
                      key={index}
                      className={`p-3 rounded-lg border text-center ${
                        player.name === playerName 
                          ? 'bg-success/5 border-success/20' 
                          : 'bg-muted/30 border-border'
                      }`}
                    >
                      <div className="w-8 h-8 mx-auto mb-2 rounded-full bg-primary/10 flex items-center justify-center">
                        <User className="w-4 h-4 text-primary" />
                      </div>
                      <p className="text-sm font-medium">{player.name}</p>
                      {player.name === playerName && (
                        <Badge variant="outline" className="mt-1 text-xs bg-success/10 text-success">
                          C'est vous !
                        </Badge>
                      )}
                    </div>
                  ))}
                  
                  {/* Place restante si moins de 4 joueurs */}
                  {gameData.players.length < 4 && (
                    <div className="p-3 rounded-lg border-2 border-dashed border-muted-foreground/30 text-center">
                      <div className="w-8 h-8 mx-auto mb-2 rounded-full border-2 border-dashed border-muted-foreground/30 flex items-center justify-center">
                        <Users className="w-4 h-4 text-muted-foreground/50" />
                      </div>
                      <p className="text-xs text-muted-foreground">Place libre</p>
                    </div>
                  )}
                </div>
              </div>
            </VContentCard>
          </motion.div>

          {/* Offre de conversion douce pour les invités */}
          {isGuest && showConversionOffer && (
            <motion.div
              variants={SCALE_VARIANTS}
              initial="hidden"
              animate="show"
            >
              <VContentCard variant="elevated" className="border-primary/20">
                <div className="p-6 space-y-6">
                  <div className="text-center space-y-3">
                    <div className="w-12 h-12 mx-auto rounded-full bg-gradient-to-r from-primary/20 to-success/20 flex items-center justify-center">
                      <Gift className="w-6 h-6 text-primary" />
                    </div>
                    <div className="space-y-2">
                      <h3 className="text-lg font-bold">Vous avez aimé Vibora ?</h3>
                      <p className="text-sm text-muted-foreground">
                        Créez votre compte pour recevoir des notifications et découvrir d'autres parties près de chez vous
                      </p>
                    </div>
                  </div>

                  <div className="space-y-4">
                    <div className="bg-primary/5 rounded-lg p-4 space-y-3">
                      <h4 className="font-medium flex items-center gap-2">
                        <Sparkles className="w-4 h-4 text-primary" />
                        Avec un compte Vibora :
                      </h4>
                      <ul className="text-sm space-y-1 text-muted-foreground">
                        <li>• Notifications avant vos parties</li>
                        <li>• Historique de vos matches</li>
                        <li>• Découverte de nouvelles parties</li>
                        <li>• Chat avec les autres joueurs</li>
                      </ul>
                    </div>

                    <div className="space-y-3">
                      <div className="space-y-2">
                        <Label htmlFor="email" className="text-sm font-medium flex items-center gap-2">
                          <Mail className="w-4 h-4" />
                          Votre email (pour les notifications)
                        </Label>
                        <Input
                          id="email"
                          type="email"
                          placeholder="votre.email@exemple.com"
                          value={email}
                          onChange={(e) => setEmail(e.target.value)}
                          className="h-12"
                        />
                      </div>

                      <VStack spacing="sm">
                        <Button 
                          onClick={handleCreateAccount}
                          disabled={isCreatingAccount}
                          className="w-full h-12 text-base font-semibold"
                          size="lg"
                        >
                          {isCreatingAccount ? (
                            <>
                              <div className="w-4 h-4 mr-2 border-2 border-white border-t-transparent rounded-full animate-spin" />
                              Création...
                            </>
                          ) : (
                            <>
                              Créer mon compte gratuit
                              <ArrowRight className="w-4 h-4 ml-2" />
                            </>
                          )}
                        </Button>
                        
                        <Button
                          variant="ghost"
                          onClick={handleSkipForNow}
                          className="w-full"
                        >
                          Plus tard, retour à l'accueil
                        </Button>
                      </VStack>
                    </div>

                    <div className="text-xs text-muted-foreground text-center space-y-1">
                      <p>✓ Gratuit et sans engagement</p>
                      <p>✓ Vos données sont protégées</p>
                      <p>✓ Désactivez les notifications quand vous voulez</p>
                    </div>
                  </div>
                </div>
              </VContentCard>
            </motion.div>
          )}

          {/* Conseils pratiques */}
          <motion.div
            variants={FADE_IN_ANIMATION_VARIANTS}
            initial="hidden"
            animate="show"
          >
            <VContentCard>
              <div className="p-6 space-y-4">
                <h3 className="font-semibold">Conseils pour votre partie</h3>
                
                <div className="space-y-3 text-sm">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0 mt-0.5">
                      <Clock className="w-3 h-3 text-primary" />
                    </div>
                    <div>
                      <p className="font-medium">Arrivez 10 minutes avant</p>
                      <p className="text-muted-foreground">Pour vous échauffer et rencontrer l'équipe</p>
                    </div>
                  </div>
                  
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0 mt-0.5">
                      <Smartphone className="w-3 h-3 text-primary" />
                    </div>
                    <div>
                      <p className="font-medium">Gardez cette page</p>
                      <p className="text-muted-foreground">Ajoutez ce lien à vos favoris pour les détails</p>
                    </div>
                  </div>
                </div>
              </div>
            </VContentCard>
          </motion.div>

          {/* CTA final si pas d'offre de conversion */}
          {!isGuest && (
            <motion.div
              variants={FADE_IN_ANIMATION_VARIANTS}
              initial="hidden"
              animate="show"
            >
              <VStack spacing="sm">
                <Button 
                  onClick={() => router.push("/my-games")}
                  className="w-full h-12"
                >
                  Voir mes parties
                </Button>
                <Button 
                  variant="outline"
                  onClick={() => router.push("/")}
                  className="w-full"
                >
                  Retour à l'accueil
                </Button>
              </VStack>
            </motion.div>
          )}
        </VStack>
      </VMain>
    </VPage>
  )
}