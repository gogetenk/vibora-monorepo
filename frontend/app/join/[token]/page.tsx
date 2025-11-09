"use client"

import { useState, useEffect } from "react"
import { useParams, useRouter } from "next/navigation"
import { motion } from "framer-motion"
import { 
  Calendar, 
  Clock, 
  MapPin, 
  Users, 
  Share2, 
  AlertTriangle, 
  Star,
  Sparkles,
  CheckCircle,
  Phone,
  User
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
import { Card, CardContent, CardHeader, CardFooter } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { toast } from "@/components/ui/use-toast"
import { viboraApi } from "@/lib/api/vibora-client"
import type { GetShareByTokenResponse } from "@/lib/api/vibora-types"
import { useTriggerInstallPrompt } from "@/lib/hooks/useTriggerInstallPrompt"

interface MagicLinkState {
  isValid: boolean
  isExpired: boolean
  game: any | null
  isLoading: boolean
  error: string | null
}

interface GuestFormData {
  name: string
  phone: string
  level: string
}

export default function MagicLinkJoinPage() {
  const params = useParams()
  const router = useRouter()
  const token = params.token as string
  const triggerInstallPrompt = useTriggerInstallPrompt()

  // États principaux
  const [linkState, setLinkState] = useState<MagicLinkState>({
    isValid: false,
    isExpired: false,
    game: null,
    isLoading: true,
    error: null
  })

  // États du formulaire invité
  const [guestData, setGuestData] = useState<GuestFormData>({
    name: "",
    phone: "",
    level: ""
  })
  
  const [isJoining, setIsJoining] = useState(false)
  const [showGuestForm, setShowGuestForm] = useState(false)
  const [hasAccount, setHasAccount] = useState(false)

  // Validation du Magic Link au chargement
  useEffect(() => {
    const validateMagicLink = async () => {
      setLinkState(prev => ({ ...prev, isLoading: true }))

      try {
        // Appel API réel pour valider le token
        const { data, error } = await viboraApi.shares.getShareByToken(token)

        if (error || !data) {
          // Gérer les erreurs spécifiques
          if (data?.isExpired) {
            setLinkState({
              isValid: false,
              isExpired: true,
              game: null,
              isLoading: false,
              error: "Ce lien d'invitation a expiré"
            })
          } else {
            setLinkState({
              isValid: false,
              isExpired: false,
              game: null,
              isLoading: false,
              error: error?.message || "Lien d'invitation invalide"
            })
          }
          return
        }

        // Parser skillLevel (peut être string ou number)
        const parseSkillLevel = (skill: any): number => {
          if (typeof skill === 'number') return skill
          const parsed = parseInt(String(skill))
          return isNaN(parsed) ? 5 : parsed
        }

        // Mapper les données API vers le format UI
        const gameDateTime = new Date(data.game.dateTime)
        const endDateTime = new Date(gameDateTime.getTime() + 90 * 60 * 1000) // +1h30

        const gameData = {
          id: data.gameId,
          status: data.game.status.toLowerCase(),
          date: data.game.dateTime.split('T')[0],
          time: gameDateTime.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' }),
          endTime: endDateTime.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' }),
          level: parseSkillLevel(data.game.skillLevel),
          price: 15, // TODO: Ajouter au backend
          description: "Rejoignez cette partie de padel !",
          club: {
            id: "1",
            name: data.game.location,
            address: data.game.location,
            distance: 0,
            indoor: true,
            rating: 4.5,
            imageUrl: "/vibrant-padel-match.png"
          },
          creator: {
            id: "creator",
            name: data.game.hostDisplayName,
            avatar: "/woman-portrait.png",
            level: parseSkillLevel(data.game.skillLevel),
            gamesPlayed: 0
          },
          players: [], // Simplified for now
          maxPlayers: data.game.maxPlayers,
          spotsLeft: data.game.maxPlayers - data.game.currentPlayers,
          isFull: data.game.status === "Full",
          magicLinkCreatedBy: data.game.hostDisplayName
        }

        setLinkState({
          isValid: true,
          isExpired: data.isExpired,
          game: gameData,
          isLoading: false,
          error: null
        })

      } catch (error) {
        console.error("Failed to validate magic link:", error)
        setLinkState({
          isValid: false,
          isExpired: false,
          game: null,
          isLoading: false,
          error: "Erreur lors du chargement de l'invitation"
        })
      }
    }

    validateMagicLink()
  }, [token])

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

  const handleQuickJoin = () => {
    setShowGuestForm(true)
  }

  const handleGuestJoin = async () => {
    // Validation simple
    if (!guestData.name.trim() || !guestData.phone.trim() || !guestData.level) {
      toast({
        title: "Informations manquantes",
        description: "Veuillez remplir tous les champs pour continuer",
        variant: "destructive"
      })
      return
    }

    if (!linkState.game?.id) {
      toast({
        title: "Erreur",
        description: "Impossible de rejoindre la partie",
        variant: "destructive"
      })
      return
    }

    setIsJoining(true)

    try {
      // Appel API réel pour rejoindre en tant que guest
      const { error } = await viboraApi.games.joinGameAsGuest(linkState.game.id, {
        name: guestData.name.trim(),
        phoneNumber: guestData.phone.trim(),
        email: undefined
      })

      if (error) {
        toast({
          title: "Erreur",
          description: error.message || "Impossible de rejoindre la partie",
          variant: "destructive"
        })
        return
      }
      
      toast({
        title: "Parfait ! Vous êtes dans la partie 🎾",
        description: `${guestData.name}, votre place est réservée pour ${linkState.game?.time}`,
      })

      // Redirection vers une page de confirmation avec proposition de conversion
      router.push(`/join/success?guest=true&name=${encodeURIComponent(guestData.name)}&gameId=${linkState.game.id}`)

      // Trigger PWA install prompt after navigation starts (delayed)
      setTimeout(() => {
        triggerInstallPrompt()
      }, 500)

    } catch (error) {
      console.error("Failed to join game as guest:", error)
      toast({
        title: "Erreur",
        description: "Impossible de rejoindre la partie pour le moment",
        variant: "destructive"
      })
    } finally {
      setIsJoining(false)
    }
  }

  const handleAuthenticatedJoin = () => {
    // Rediriger vers l'authentification puis retour
    router.push(`/auth/login?returnTo=/join/${token}`)
  }

  const handleShare = async () => {
    const shareData = {
      title: `Partie de padel chez ${linkState.game?.club.name}`,
      text: `${linkState.game?.creator.name} t'invite à une partie de padel le ${formatDate(linkState.game?.date)} à ${linkState.game?.time}`,
      url: window.location.href
    }

    if (navigator.share) {
      try {
        await navigator.share(shareData)
      } catch (err) {
        // Fallback to clipboard
        navigator.clipboard.writeText(window.location.href)
        toast({
          title: "Lien copié !",
          description: "Le lien d'invitation a été copié dans le presse-papier"
        })
      }
    } else {
      navigator.clipboard.writeText(window.location.href)
      toast({
        title: "Lien copié !",
        description: "Le lien d'invitation a été copié dans le presse-papier"
      })
    }
  }

  // États de chargement
  if (linkState.isLoading) {
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

  // États d'erreur
  if (!linkState.isValid || linkState.error) {
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
                <h1 className="text-2xl font-bold">
                  {linkState.isExpired ? "Invitation expirée" : "Lien invalide"}
                </h1>
                <p className="text-muted-foreground max-w-md">
                  {linkState.error || "Cette invitation n'est plus valide. Contactez la personne qui vous a invité pour obtenir un nouveau lien."}
                </p>
              </div>

              <VStack spacing="sm" className="max-w-sm mx-auto">
                <Button 
                  onClick={() => router.push("/")}
                  className="w-full"
                >
                  Découvrir Vibora
                </Button>
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

  const { game } = linkState

  return (
    <VPage>
      {/* Header collé - optimal mobile */}
      <VHeader sticky>
        <VContainer>
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-full bg-success/20 flex items-center justify-center">
                <Sparkles className="w-4 h-4 text-success" />
              </div>
              <div>
                <h1 className="font-semibold text-sm">Invitation padel</h1>
                <p className="text-xs text-muted-foreground">de {game.magicLinkCreatedBy}</p>
              </div>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={handleShare}
              className="h-8 w-8 p-0"
            >
              <Share2 className="w-4 h-4" />
            </Button>
          </div>
        </VContainer>
      </VHeader>

      <VMain>
        <VStack spacing="lg">
          {/* Hero Card - Informations match avec CTA principal */}
          <motion.div
            variants={FADE_IN_ANIMATION_VARIANTS}
            initial="hidden"
            animate="show"
          >
            <VContentCard variant="elevated" className="overflow-hidden">
              {/* Image du club si disponible */}
              {game.club.imageUrl && (
                <div className="h-32 bg-gradient-to-r from-primary/20 to-primary/10 relative overflow-hidden">
                  <img 
                    src={game.club.imageUrl} 
                    alt={game.club.name}
                    className="w-full h-full object-cover opacity-80"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-black/20 to-transparent" />
                </div>
              )}

              <div className="p-6 space-y-6">
                {/* Statut de la partie */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    {game.isFull ? (
                      <Badge variant="outline" className="bg-red-50 text-red-700 border-red-200">
                        Complet
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="bg-success/10 text-success border-success/20">
                        <CheckCircle className="w-3 h-3 mr-1" />
                        {game.spotsLeft} place{game.spotsLeft > 1 ? "s" : ""} libre{game.spotsLeft > 1 ? "s" : ""}
                      </Badge>
                    )}
                    <Badge variant="outline" className="bg-primary/10 text-primary border-primary/20">
                      Niveau {game.level}
                    </Badge>
                  </div>
                </div>

                {/* Informations du club */}
                <div className="space-y-2">
                  <h2 className="text-xl font-bold">{game.club.name}</h2>
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <div className="flex items-center gap-1">
                      <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />
                      <span>{game.club.rating}</span>
                    </div>
                    <span>•</span>
                    <Badge variant="outline" className="text-xs">
                      {game.club.indoor ? "Indoor" : "Outdoor"}
                    </Badge>
                    <span>•</span>
                    <span>{game.club.distance} km</span>
                  </div>
                </div>

                {/* Détails de la session */}
                <div className="bg-muted/50 rounded-lg p-4 space-y-3">
                  <div className="flex items-center gap-3">
                    <Calendar className="w-4 h-4 text-muted-foreground" />
                    <span className="font-medium">{formatDate(game.date)}</span>
                  </div>
                  <div className="flex items-center gap-3">
                    <Clock className="w-4 h-4 text-muted-foreground" />
                    <span>{game.time} - {game.endTime}</span>
                  </div>
                  <div className="flex items-center gap-3">
                    <MapPin className="w-4 h-4 text-muted-foreground" />
                    <span className="text-sm">{game.club.address}</span>
                  </div>
                </div>

                {/* Description si disponible */}
                {game.description && (
                  <div className="space-y-2">
                    <h3 className="text-sm font-medium text-muted-foreground">Message de {game.creator.name}</h3>
                    <p className="text-sm bg-primary/5 rounded-lg p-3 border-l-2 border-primary/20">
                      "{game.description}"
                    </p>
                  </div>
                )}

                {/* Prix si applicable */}
                {game.price > 0 && (
                  <div className="flex items-center justify-between p-3 bg-success/5 rounded-lg border border-success/20">
                    <span className="text-sm font-medium">Prix par personne</span>
                    <span className="text-lg font-bold text-success">{game.price}€</span>
                  </div>
                )}
              </div>
            </VContentCard>
          </motion.div>

          {/* Joueurs actuels */}
          <motion.div
            variants={FADE_IN_ANIMATION_VARIANTS}
            initial="hidden"
            animate="show"
          >
            <VContentCard>
              <div className="p-6 space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="font-semibold">Joueurs inscrits</h3>
                  <span className="text-sm text-muted-foreground">
                    {game.players.length}/{game.maxPlayers}
                  </span>
                </div>

                <div className="space-y-3">
                  {game.players.map((player: any) => (
                    <div key={player.id} className="flex items-center gap-3">
                      <Avatar className="w-10 h-10 border-2 border-border">
                        <AvatarImage src={player.avatar} alt={player.name} />
                        <AvatarFallback>{player.name.charAt(0)}</AvatarFallback>
                      </Avatar>
                      <div className="flex-1">
                        <p className="font-medium text-sm">{player.name}</p>
                        <p className="text-xs text-muted-foreground">Niveau {player.level}</p>
                      </div>
                      {player.isCreator && (
                        <Badge variant="outline" className="text-xs bg-primary/10 text-primary">
                          Organisateur
                        </Badge>
                      )}
                    </div>
                  ))}

                  {/* Places disponibles */}
                  {Array.from({ length: game.spotsLeft }).map((_, index) => (
                    <div key={`empty-${index}`} className="flex items-center gap-3 opacity-60">
                      <div className="w-10 h-10 rounded-full border-2 border-dashed border-muted-foreground/30 flex items-center justify-center">
                        <Users className="w-4 h-4 text-muted-foreground/50" />
                      </div>
                      <div className="flex-1">
                        <p className="text-sm text-muted-foreground">Place libre</p>
                        <p className="text-xs text-muted-foreground/70">En attente d'un joueur</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </VContentCard>
          </motion.div>

          {/* CTA Principal - Formulaire de participation */}
          {!game.isFull && (
            <motion.div
              variants={SCALE_VARIANTS}
              initial="hidden" 
              animate="show"
            >
              {!showGuestForm ? (
                <VContentCard variant="elevated" className="border-success/20">
                  <div className="p-6 text-center space-y-4">
                    <div className="w-12 h-12 mx-auto rounded-full bg-success/20 flex items-center justify-center">
                      <CheckCircle className="w-6 h-6 text-success" />
                    </div>
                    <div className="space-y-2">
                      <h3 className="font-bold">Rejoindre cette partie</h3>
                      <p className="text-sm text-muted-foreground">
                        Plus que {game.spotsLeft} place{game.spotsLeft > 1 ? "s" : ""} disponible{game.spotsLeft > 1 ? "s" : ""}
                      </p>
                    </div>

                    <VStack spacing="sm">
                      <Button 
                        onClick={handleQuickJoin}
                        className="w-full h-12 text-base font-semibold"
                        size="lg"
                      >
                        Participer maintenant
                      </Button>
                      
                      <Button
                        variant="outline"
                        onClick={handleAuthenticatedJoin}
                        className="w-full"
                      >
                        J'ai déjà un compte Vibora
                      </Button>
                    </VStack>

                    <p className="text-xs text-muted-foreground">
                      Aucune inscription requise • Rejoignez en 30 secondes
                    </p>
                  </div>
                </VContentCard>
              ) : (
                <VContentCard variant="elevated">
                  <div className="p-6 space-y-6">
                    <div className="text-center space-y-2">
                      <h3 className="font-bold">Dernière étape !</h3>
                      <p className="text-sm text-muted-foreground">
                        Dites-nous qui vous êtes pour que les autres joueurs puissent vous identifier
                      </p>
                    </div>

                    <VStack spacing="md">
                      <div className="space-y-2">
                        <Label htmlFor="name" className="text-sm font-medium flex items-center gap-2">
                          <User className="w-4 h-4" />
                          Votre prénom
                        </Label>
                        <Input
                          id="name"
                          placeholder="Ex: Marie"
                          value={guestData.name}
                          onChange={(e) => setGuestData(prev => ({ ...prev, name: e.target.value }))}
                          className="h-12"
                        />
                      </div>

                      <div className="space-y-2">
                        <Label htmlFor="phone" className="text-sm font-medium flex items-center gap-2">
                          <Phone className="w-4 h-4" />
                          Numéro de téléphone
                        </Label>
                        <Input
                          id="phone"
                          type="tel"
                          placeholder="06 12 34 56 78"
                          value={guestData.phone}
                          onChange={(e) => setGuestData(prev => ({ ...prev, phone: e.target.value }))}
                          className="h-12"
                        />
                      </div>

                      <div className="space-y-2">
                        <Label className="text-sm font-medium">Votre niveau (approximatif)</Label>
                        <Select 
                          value={guestData.level} 
                          onValueChange={(value) => setGuestData(prev => ({ ...prev, level: value }))}
                        >
                          <SelectTrigger className="h-12">
                            <SelectValue placeholder="Choisissez votre niveau" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="1">Niveau 1 - Débutant</SelectItem>
                            <SelectItem value="2">Niveau 2 - Débutant+</SelectItem>
                            <SelectItem value="3">Niveau 3 - Intermédiaire</SelectItem>
                            <SelectItem value="4">Niveau 4 - Intermédiaire+</SelectItem>
                            <SelectItem value="5">Niveau 5 - Confirmé</SelectItem>
                            <SelectItem value="6">Niveau 6 - Confirmé+</SelectItem>
                            <SelectItem value="7">Niveau 7 - Avancé</SelectItem>
                            <SelectItem value="8">Niveau 8 - Avancé+</SelectItem>
                            <SelectItem value="9">Niveau 9 - Expert</SelectItem>
                            <SelectItem value="10">Niveau 10 - Pro</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>

                      <div className="pt-4 space-y-3">
                        <Button 
                          onClick={handleGuestJoin}
                          disabled={isJoining}
                          className="w-full h-12 text-base font-semibold"
                          size="lg"
                        >
                          {isJoining ? (
                            <>
                              <div className="w-4 h-4 mr-2 border-2 border-white border-t-transparent rounded-full animate-spin" />
                              Confirmation...
                            </>
                          ) : (
                            "Confirmer ma participation"
                          )}
                        </Button>
                        
                        <Button
                          variant="ghost"
                          onClick={() => setShowGuestForm(false)}
                          className="w-full"
                        >
                          Retour
                        </Button>
                      </div>
                    </VStack>

                    <div className="text-xs text-muted-foreground text-center space-y-1">
                      <p>✓ Pas d'inscription requise</p>
                      <p>✓ Vos données restent privées</p>
                      <p>✓ Possibilité de créer un compte plus tard</p>
                    </div>
                  </div>
                </VContentCard>
              )}
            </motion.div>
          )}

          {/* Message si complet */}
          {game.isFull && (
            <motion.div
              variants={FADE_IN_ANIMATION_VARIANTS}
              initial="hidden"
              animate="show"
            >
              <Alert className="bg-amber-50 border-amber-200">
                <AlertTriangle className="w-4 h-4 text-amber-600" />
                <AlertDescription className="text-amber-800">
                  Cette partie est complète ! Vous pouvez contacter {game.creator.name} pour être sur liste d'attente.
                </AlertDescription>
              </Alert>
            </motion.div>
          )}
        </VStack>
      </VMain>
    </VPage>
  )
}