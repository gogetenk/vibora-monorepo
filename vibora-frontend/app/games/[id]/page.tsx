"use client"

import { useState, useEffect, useMemo } from "react"
import { useParams, useRouter } from "next/navigation"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import {
  Calendar,
  Clock,
  MapPin,
  Users,
  Share2,
  AlertTriangle,
  Info,
  Phone,
  MessageCircle,
  UserPlus,
  X,
  WifiOff,
} from "lucide-react"
import Header from "@/components/header"
import { motion, AnimatePresence } from "framer-motion"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { toast } from "@/components/ui/use-toast"
import { InvitePlayersModal } from "@/components/invite-players-modal"
import { ShareGameButton } from "@/components/share-game-button"
import { useGameDetails, useUserProfile } from "@/lib/hooks/use-game-data"
import { viboraApi } from "@/lib/api/vibora-client"
import { getSession } from "@/lib/auth/supabase-auth"

export default function GameDetail() {
  const params = useParams()
  const router = useRouter()
  const gameId = params.id as string

  // Use offline-first hooks
  const { data: gameData, isLoading: isLoadingGame, isOffline } = useGameDetails(gameId)
  const { data: currentUserProfile } = useUserProfile()
  
  const [isMounted, setIsMounted] = useState(false)
  const [isLeaving, setIsLeaving] = useState(false)
  const [isJoining, setIsJoining] = useState(false)
  const [activeTab, setActiveTab] = useState("players")
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false)
  const [newMessage, setNewMessage] = useState("")
  const [currentUserExternalId, setCurrentUserExternalId] = useState<string | null>(null)
  
  // Avoid hydration errors
  useEffect(() => {
    setIsMounted(true)
  }, [])

  // TODO: Implémenter le système d'invitations réel (actuellement mocké)
  // const { sentInvitations, cancelInvitation, resendInvitation } = useInvitations(gameId)

  const openInviteModal = () => {
    setIsInviteModalOpen(true)
  }

  const closeInviteModal = () => {
    setIsInviteModalOpen(false)
  }

  const handleCallClub = () => {
    toast({
      title: "Appel au club",
      description: "Ouverture de l'application téléphone...",
    })
  }

  const handleSendMessage = () => {
    if (!newMessage.trim()) return

    // TODO: Implémenter le chat avec WebSocket/API temps réel
    setNewMessage("")
    toast({
      title: "Fonctionnalité à venir",
      description: "Le chat sera disponible prochainement",
    })
  }

  // Récupérer l'utilisateur courant
  useEffect(() => {
    const fetchCurrentUser = async () => {
      const { session } = await getSession()
      if (session?.user?.id) {
        setCurrentUserExternalId(session.user.id)
      }
    }
    fetchCurrentUser()
  }, [])
  
  // Helper pour parser le skillLevel (peut être string ou number)
  const parseSkillLevel = (skill: any): number => {
    if (typeof skill === 'number') return skill
    const parsed = parseInt(String(skill))
    return isNaN(parsed) ? 5 : parsed
  }

  // Transform API data to UI format (memoized to avoid re-renders)
  const game = useMemo(() => {
    if (!gameData) return null
    
    return {
      id: gameData.id,
      status: gameData.status.toLowerCase(),
      date: gameData.dateTime.split('T')[0],
      time: new Date(gameData.dateTime).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' }),
      endTime: new Date(new Date(gameData.dateTime).getTime() + 60 * 60 * 1000).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' }),
      level: parseSkillLevel(gameData.skillLevel),
      price: 25, // TODO: récupérer depuis l'API
      paymentStatus: "authorized",
      club: {
        id: "1",
        name: gameData.location,
        address: gameData.location,
        distance: 2.5,
        indoor: true,
        phone: "+33123456789",
        image: "/summer-padel-match.png",
      },
      creator: {
        id: gameData.hostExternalId,
        name: gameData.hostDisplayName,
        avatar: "/thoughtful-man-profile.png",
        level: parseSkillLevel(gameData.skillLevel),
      },
      players: gameData.participants.map((p: any, idx: number) => ({
        id: p.identifier || `player-${idx}`,
        name: p.displayName,
        avatar: "/thoughtful-man-profile.png",
        level: parseSkillLevel(p.skillLevel),
        status: "confirmed",
      })),
      spotsLeft: gameData.maxPlayers - gameData.currentPlayers,
      messages: [], // TODO: implémenter le chat
    }
  }, [gameData])
  
  // Show loader while mounting or loading to avoid hydration errors
  if (!isMounted || isLoadingGame) {
    return (
      <div className="min-h-screen bg-background">
        <Header />
        <div className="flex items-center justify-center py-20">
          <div className="text-center space-y-4">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto" />
            <p className="text-muted-foreground">Chargement...</p>
          </div>
        </div>
      </div>
    )
  }
  
  if (!game) {
    return (
      <div className="min-h-screen bg-background">
        <Header />
        <div className="flex items-center justify-center py-20">
          <div className="text-center space-y-4">
            <AlertTriangle className="w-12 h-12 text-destructive mx-auto" />
            <p className="text-muted-foreground">Partie introuvable</p>
            <Button onClick={() => router.back()}>Retour</Button>
          </div>
        </div>
      </div>
    )
  }

  const formatDate = (dateString: string) => {
    const options: Intl.DateTimeFormatOptions = { weekday: "long", day: "numeric", month: "long" }
    return new Date(dateString).toLocaleDateString("fr-FR", options)
  }

  const handleLeaveGame = async () => {
    if (!window.confirm("Êtes-vous sûr de vouloir quitter cette partie ? Votre place sera libérée pour d'autres joueurs.")) {
      return
    }

    setIsLeaving(true)

    try {
      const { error } = await viboraApi.games.leaveGame(gameId)

      if (error) {
        toast({
          variant: "destructive",
          title: "Erreur",
          description: error.message || "Impossible de quitter la partie",
        })
        return
      }

      toast({
        title: "Vous avez quitté la partie",
        description: "Votre place a été libérée pour d'autres joueurs",
      })

      setTimeout(() => {
        router.push("/my-games")
      }, 1500)

    } catch (err) {
      console.error("Failed to leave game:", err)
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Une erreur s'est produite",
      })
    } finally {
      setIsLeaving(false)
    }
  }

  const handleJoinGame = async () => {
    if (!currentUserExternalId || !currentUserProfile) {
      toast({
        variant: "destructive",
        title: "Authentification requise",
        description: "Vous devez être connecté pour rejoindre une partie",
      })
      router.push(`/auth/login?redirect=/games/${gameId}`)
      return
    }

    setIsJoining(true)

    try {
      const { error } = await viboraApi.games.joinGame(gameId, {
        userName: currentUserProfile.displayName || currentUserProfile.firstName || "Joueur",
        userSkillLevel: currentUserProfile.skillLevel?.toString() || "5"
      })

      if (error) {
        toast({
          variant: "destructive",
          title: "Erreur",
          description: error.message || "Impossible de rejoindre la partie",
        })
        return
      }

      toast({
        title: "Vous avez rejoint la partie !",
        description: "Vous pouvez maintenant discuter avec les autres joueurs",
      })

      // Recharger les données de la partie
      setTimeout(() => {
        window.location.reload()
      }, 1000)

    } catch (err) {
      console.error("Failed to join game:", err)
      toast({
        variant: "destructive",
        title: "Erreur",
        description: "Une erreur s'est produite",
      })
    } finally {
      setIsJoining(false)
    }
  }

  // game and loading states are already handled above
  // Game details rendering starts here
  
  // Utiliser uniquement les vrais joueurs de l'API
  const allPlayers = game.players

  // Calculer le nombre de places restantes
  const effectiveSpotsLeft = game.spotsLeft

  // Pas d'invitations mockées pour l'instant (TODO: implémenter système d'invitation réel)
  const pendingInvitations: any[] = []

  // Vérifier si l'utilisateur est participant
  const isParticipant = game && currentUserExternalId && 
    game.players.some((p: any) => p.id === currentUserExternalId || p.id.includes(currentUserExternalId))
  
  // Vérifier si la partie est complète
  const isFull = game && game.spotsLeft === 0

  // Vérifier la compatibilité du niveau de compétence
  const isSkillLevelCompatible = () => {
    if (!game || !currentUserProfile) return true
    if (!game.level) return true // Pas de niveau requis
    
    const playerLevel = currentUserProfile.skillLevel || 5
    const gameLevel = game.level
    const difference = Math.abs(gameLevel - playerLevel)
    
    return difference <= 1 // Max ±1 niveau d'écart
  }

  const skillLevelDifference = () => {
    if (!game || !currentUserProfile) return 0
    const playerLevel = currentUserProfile.skillLevel || 5
    return Math.abs(game.level - playerLevel)
  }

  return (
    <div className="min-h-screen bg-background pb-20">
      <Header back />

      <main className="container py-6">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          className="mx-auto max-w-2xl space-y-6"
        >
          {/* Hero Card avec image du club */}
          <Card className="overflow-hidden bg-card/80 backdrop-blur-sm border-border/50 shadow-lg">
            <div className="relative h-56 overflow-hidden">
              <img
                src={game.club.image || "/placeholder.svg"}
                alt={game.club.name}
                className="h-full w-full object-cover"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black/95 via-black/60 to-black/20" />

              {/* Informations principales */}
              <div className="absolute bottom-4 left-4 right-4">
                {/* Badges alignés */}
                <div className="flex items-center gap-2 mb-3 flex-wrap">
                  <Badge className="bg-white/95 backdrop-blur-sm text-gray-700 font-medium border-0 shadow-md">
                    Ouvert – {effectiveSpotsLeft} place{effectiveSpotsLeft > 1 ? "s" : ""}
                  </Badge>
                  <Badge className="bg-white/95 backdrop-blur-sm text-gray-700 font-medium border-0 shadow-md">Niveau {game.level}</Badge>
                  {game.club.indoor && (
                    <Badge className="bg-white/95 backdrop-blur-sm text-gray-700 font-medium border-0 shadow-md">Indoor</Badge>
                  )}
                </div>
                
                {/* Titre */}
                <h1 className="text-2xl font-bold text-white mb-2 drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]">{game.club.name}</h1>
                
                {/* Informations essentielles seulement */}
                <div className="flex items-center gap-4 text-sm text-white/95 drop-shadow-[0_1px_3px_rgba(0,0,0,0.8)]">
                  <div className="flex items-center gap-1">
                    <Calendar className="h-4 w-4" />
                    <span>{formatDate(game.date)}</span>
                  </div>
                  <div className="flex items-center gap-1">
                    <Clock className="h-4 w-4" />
                    <span>{game.time} - {game.endTime}</span>
                  </div>
                </div>
              </div>
            </div>
          </Card>

          {/* Onglets */}
          <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
            <TabsList className="grid w-full grid-cols-2 bg-muted/50 backdrop-blur-sm">
              <TabsTrigger
                value="players"
                className="data-[state=active]:bg-primary data-[state=active]:text-primary-foreground text-muted-foreground"
              >
                <Users className="h-4 w-4 mr-2" />
                Joueurs
              </TabsTrigger>
              <TabsTrigger
                value="chat"
                disabled={!isParticipant}
                className="data-[state=active]:bg-primary data-[state=active]:text-primary-foreground text-muted-foreground disabled:opacity-40 disabled:cursor-not-allowed"
              >
                <MessageCircle className="h-4 w-4 mr-2" />
                Chat
              </TabsTrigger>
            </TabsList>

            <TabsContent value="players" className="mt-6 space-y-4">
                <motion.div
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: 20 }}
                  transition={{ duration: 0.2 }}
                  className="space-y-6"
                >
                  {/* Joueurs confirmés */}
                  <div className="space-y-3">
                    <h3 className="text-sm font-medium text-muted-foreground uppercase tracking-wider">
                      Joueurs confirmés ({allPlayers.length}/4)
                    </h3>

                    {allPlayers.map((player: any, idx: number) => (
                      <Card key={player.id || `player-${idx}`} className="bg-card/50 backdrop-blur-sm border-border/50">
                        <CardContent className="p-4">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                              <Avatar className="h-14 w-14 ring-2 ring-border/50">
                                <AvatarImage src={player.avatar || "/placeholder.svg"} alt={player.name} />
                                <AvatarFallback className="bg-muted text-muted-foreground text-base">
                                  {player.name.charAt(0)}
                                </AvatarFallback>
                              </Avatar>
                              <div>
                                <p className="font-medium text-foreground">{player.name}</p>
                                <Badge className="mt-1 bg-primary/20 text-primary border-primary/30 font-semibold text-xs">Niveau {player.level}</Badge>
                              </div>
                            </div>
                            {player.id === game.creator.id && (
                              <Badge className="bg-success/40 text-success-foreground border-success/50">Créateur</Badge>
                            )}
                          </div>
                        </CardContent>
                      </Card>
                    ))}

                    {/* Places disponibles */}
                    {effectiveSpotsLeft > 0 && (
                      <Card className={`backdrop-blur-sm ${
                        !isParticipant && currentUserProfile && !isSkillLevelCompatible()
                          ? "bg-amber-50/5 border-amber-500/50"
                          : "bg-card/30 border-dashed border-border/50"
                      }`}>
                        <CardContent className="p-4 space-y-3">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                              <div className="flex h-12 w-12 items-center justify-center rounded-full border-2 border-dashed border-border/50">
                                <UserPlus className="h-5 w-5 text-muted-foreground" />
                              </div>
                              <p className="text-muted-foreground">
                                {effectiveSpotsLeft} place{effectiveSpotsLeft > 1 ? "s" : ""} disponible
                                {effectiveSpotsLeft > 1 ? "s" : ""}
                              </p>
                            </div>
                            {isParticipant ? (
                              <Button
                                size="sm"
                                className="bg-primary hover:bg-primary/90 text-primary-foreground font-medium"
                                onClick={openInviteModal}
                              >
                                Inviter
                              </Button>
                            ) : (
                              <Button
                                size="sm"
                                className="bg-primary hover:bg-primary/90 text-primary-foreground font-medium"
                                onClick={handleJoinGame}
                                disabled={isJoining || !isSkillLevelCompatible()}
                              >
                                {isJoining ? "Chargement..." : "Rejoindre"}
                              </Button>
                            )}
                          </div>
                          
                          {/* Message niveau incompatible intégré */}
                          {!isParticipant && currentUserProfile && !isSkillLevelCompatible() && (
                            <div className="flex items-start gap-2 pt-3 border-t border-amber-500/30">
                              <AlertTriangle className="h-4 w-4 text-amber-500 mt-0.5 flex-shrink-0" />
                              <div className="flex-1">
                                <p className="text-sm font-medium text-amber-600 dark:text-amber-400">Niveau incompatible</p>
                                <p className="text-xs text-muted-foreground mt-1">
                                  Votre niveau ({currentUserProfile.skillLevel || 5}) est trop éloigné du niveau de cette partie (niveau {game.level}). 
                                  Seuls les joueurs à ±1 niveau peuvent rejoindre.
                                </p>
                              </div>
                            </div>
                          )}
                        </CardContent>
                      </Card>
                    )}
                  </div>

                  {/* Bouton Quitter la partie - Seulement pour participants */}
                  {isParticipant && (
                    <Alert className="bg-destructive/10 backdrop-blur-sm border-destructive/30">
                      <AlertTriangle className="h-4 w-4 text-destructive" />
                      <AlertTitle className="text-destructive">Quitter la partie</AlertTitle>
                      <AlertDescription className="text-muted-foreground mb-3">
                        Si vous avez un empêchement, vous pouvez quitter cette partie. Votre place sera libérée pour d'autres joueurs.
                      </AlertDescription>
                      <Button
                        variant="destructive"
                        className="w-full"
                        onClick={handleLeaveGame}
                        disabled={isLeaving}
                      >
                        {isLeaving ? "Chargement..." : "Quitter la partie"}
                      </Button>
                    </Alert>
                  )}
                </motion.div>
              </TabsContent>

              <TabsContent value="chat" className="mt-6">
                <motion.div
                  initial={{ opacity: 0, x: 20 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: -20 }}
                  transition={{ duration: 0.2 }}
                >
                  <Card className="bg-card/50 backdrop-blur-sm border-border/50">
                    <CardContent className="p-4">
                      <div className="mb-4 max-h-60 space-y-4 overflow-y-auto">
                        {game.messages.map((message: any, idx: number) => (
                          <div key={message.id || `msg-${idx}`} className="flex gap-3">
                            <Avatar className="h-8 w-8 shrink-0 ring-1 ring-border/50">
                              <AvatarImage src={message.userAvatar || "/placeholder.svg"} alt={message.userName} />
                              <AvatarFallback className="bg-muted text-muted-foreground text-xs">
                                {message.userName.charAt(0)}
                              </AvatarFallback>
                            </Avatar>
                            <div className="flex-1">
                              <div className="flex items-center gap-2 mb-1">
                                <p className="text-sm font-medium text-foreground">{message.userName}</p>
                                <span className="text-xs text-muted-foreground">
                                  {new Date(message.timestamp).toLocaleTimeString([], {
                                    hour: "2-digit",
                                    minute: "2-digit",
                                  })}
                                </span>
                              </div>
                              <p className="text-sm text-muted-foreground leading-relaxed">{message.text}</p>
                            </div>
                          </div>
                        ))}
                      </div>

                      {/* Chat input - Seulement pour participants */}
                      {isParticipant ? (
                        <div className="flex gap-2">
                          <input
                            type="text"
                            placeholder="Envoyer un message..."
                            value={newMessage}
                            onChange={(e) => setNewMessage(e.target.value)}
                            onKeyPress={(e) => e.key === "Enter" && handleSendMessage()}
                            className="flex-1 rounded-lg bg-muted/50 px-3 py-2 text-sm text-foreground placeholder-muted-foreground backdrop-blur-sm focus:bg-muted/70 focus:outline-none focus:ring-2 focus:ring-primary border border-border/50"
                          />
                          <Button
                            size="sm"
                            className="bg-primary hover:bg-primary/90 text-primary-foreground font-medium"
                            onClick={handleSendMessage}
                            disabled={!newMessage.trim()}
                          >
                            Envoyer
                          </Button>
                        </div>
                      ) : (
                        <Alert className="bg-muted/50 border-border/50">
                          <Info className="h-4 w-4" />
                          <AlertDescription>
                            Seuls les participants peuvent écrire dans le chat. Rejoignez la partie pour participer à la discussion.
                          </AlertDescription>
                        </Alert>
                      )}
                    </CardContent>
                  </Card>
                </motion.div>
              </TabsContent>
          </Tabs>

          {/* Actions secondaires - Contact & Partage */}
          <div className="grid grid-cols-2 gap-3 pt-2">
            <Button
              onClick={handleCallClub}
              variant="outline"
              className="bg-card/50 backdrop-blur-sm border-border/30 hover:bg-card/80 hover:border-border text-muted-foreground hover:text-foreground font-medium"
            >
              <Phone className="h-4 w-4 mr-2" />
              Appeler le club
            </Button>
            <ShareGameButton
              gameId={game.id}
              gameTitle={game.club.name}
              location={game.club.address}
              dateTime={`${game.date}T${game.time}`}
              variant="outline"
              className="bg-card/50 backdrop-blur-sm border-border/30 hover:bg-card/80 hover:border-border text-muted-foreground hover:text-foreground font-medium"
            />
          </div>
        </motion.div>

        <InvitePlayersModal
          isOpen={isInviteModalOpen}
          onClose={closeInviteModal}
          gameId={gameId}
          gameLevel={game.level}
        />
      </main>
    </div>
  )
}
