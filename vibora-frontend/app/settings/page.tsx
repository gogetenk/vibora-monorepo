"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { User, Bell, CreditCard, Shield, Heart, ChevronRight, LogOut, Trash2, ArrowLeft } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import {
  VPage,
  VHeader,
  VMain,
  VContainer,
  VStack,
  VContentCard,
} from "@/components/ui/vibora-layout"
import { FADE_IN_ANIMATION_VARIANTS, STAGGER_CONTAINER_VARIANTS } from "@/lib/animation-variants"
import { motion } from "framer-motion"
import { viboraApi } from "@/lib/api/vibora-client"
import { useToast } from "@/components/ui/use-toast"
import type { UserProfileDto } from "@/lib/api/vibora-types"

const settingsItems = [
  {
    id: "profile",
    title: "Profil",
    description: "Gérer vos informations personnelles",
    icon: User,
    href: "/settings/profile",
  },
  {
    id: "notifications",
    title: "Notifications",
    description: "Configurer vos préférences de notification",
    icon: Bell,
    href: "/settings/notifications",
  },
  {
    id: "payment",
    title: "Paiement",
    description: "Gérer vos moyens de paiement",
    icon: CreditCard,
    href: "/settings/payment",
  },
  {
    id: "password",
    title: "Mot de passe",
    description: "Modifier votre mot de passe",
    icon: Shield,
    href: "/settings/password",
  },
  {
    id: "favorites",
    title: "Créneaux favoris",
    description: "Configurer vos créneaux préférés",
    icon: Heart,
    href: "/settings/favorite-slots",
  },
]

export default function SettingsPage() {
  const router = useRouter()
  const { toast } = useToast()
  const [currentUser, setCurrentUser] = useState<UserProfileDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const { data } = await viboraApi.users.getCurrentUserProfile()
        if (data) {
          setCurrentUser(data)
        }
      } catch (error) {
        console.error("Failed to load profile:", error)
      } finally {
        setIsLoading(false)
      }
    }

    fetchProfile()
  }, [])

  const handleLogout = () => {
    // TODO: Implement logout
    toast({
      title: "Déconnexion",
      description: "Vous avez été déconnecté avec succès",
    })
    router.push("/auth/login")
  }

  const handleDeleteAccount = () => {
    // TODO: Implement account deletion with confirmation
    toast({
      variant: "destructive",
      title: "Suppression de compte",
      description: "Cette fonctionnalité sera bientôt disponible",
    })
  }

  const getLevelLabel = (level?: number) => {
    if (!level) return "Non défini"
    if (level <= 3) return "Débutant"
    if (level <= 6) return "Intermédiaire"
    return "Avancé"
  }

  return (
    <VPage animate>
      <VHeader>
        <VContainer>
          <div className="flex items-center gap-3 h-16">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => router.back()}
              className="rounded-full"
            >
              <ArrowLeft className="w-5 h-5" />
            </Button>
            <h1 className="text-xl font-bold">Paramètres</h1>
          </div>
        </VContainer>
      </VHeader>

      <VMain>
        <motion.div
          variants={STAGGER_CONTAINER_VARIANTS}
          initial="hidden"
          animate="show"
        >
          <VStack spacing="lg">
            {/* User Profile Card */}
            <motion.div variants={FADE_IN_ANIMATION_VARIANTS}>
              <VContentCard variant="elevated" className="p-6">
                <div className="flex items-center gap-4">
                  <div className="relative">
                    <Avatar className="w-16 h-16 border-2 border-border">
                      <AvatarImage
                        src={currentUser?.profilePhotoUrl || "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=80&h=80&fit=crop&crop=face"}
                        alt="Profile"
                      />
                      <AvatarFallback className="text-lg font-bold">
                        {currentUser?.firstName?.[0]}{currentUser?.lastName?.[0]}
                      </AvatarFallback>
                    </Avatar>
                    <div className="absolute -bottom-1 -right-1 w-5 h-5 bg-success rounded-full border-2 border-background"></div>
                  </div>
                  <div className="flex-1">
                    <h2 className="text-xl font-semibold">
                      {currentUser?.firstName} {currentUser?.lastName}
                    </h2>
                    <p className="text-muted-foreground text-sm">
                      {currentUser?.email || "thomas.martin@email.com"}
                    </p>
                    <div className="flex items-center mt-2">
                      <Badge variant="outline" className="bg-success/10 text-success border-success/30 font-medium">
                        Niveau {currentUser?.skillLevel || 7} - {getLevelLabel(currentUser?.skillLevel)}
                      </Badge>
                    </div>
                  </div>
                </div>
              </VContentCard>
            </motion.div>

            {/* Settings Items */}
            <VStack spacing="sm">
              {settingsItems.map((item, index) => (
                <motion.div
                  key={item.id}
                  variants={FADE_IN_ANIMATION_VARIANTS}
                  custom={index}
                >
                  <Link href={item.href}>
                    <VContentCard className="p-4 hover:bg-card/90 transition-all duration-200 cursor-pointer group">
                      <div className="flex items-center gap-4">
                        <div className="w-12 h-12 rounded-xl bg-muted flex items-center justify-center group-hover:bg-muted/80 transition-colors">
                          <item.icon className="w-6 h-6 text-primary" />
                        </div>
                        <div className="flex-1">
                          <h3 className="font-semibold">{item.title}</h3>
                          <p className="text-sm text-muted-foreground">{item.description}</p>
                        </div>
                        <ChevronRight className="w-5 h-5 text-muted-foreground group-hover:text-foreground transition-colors" />
                      </div>
                    </VContentCard>
                  </Link>
                </motion.div>
              ))}
            </VStack>

            {/* Danger Zone */}
            <motion.div variants={FADE_IN_ANIMATION_VARIANTS}>
              <Alert className="bg-destructive/10 border-destructive/30">
                <AlertTitle className="text-destructive font-semibold flex items-center gap-2 mb-4">
                  <Shield className="w-5 h-5" />
                  Zone de danger
                </AlertTitle>
                <AlertDescription className="space-y-3">
                  <Button
                    variant="ghost"
                    className="w-full justify-start text-destructive hover:text-destructive hover:bg-destructive/20"
                    onClick={handleLogout}
                  >
                    <LogOut className="w-5 h-5 mr-3" />
                    Se déconnecter
                  </Button>
                  <Button
                    variant="ghost"
                    className="w-full justify-start text-destructive hover:text-destructive hover:bg-destructive/20"
                    onClick={handleDeleteAccount}
                  >
                    <Trash2 className="w-5 h-5 mr-3" />
                    Supprimer le compte
                  </Button>
                </AlertDescription>
              </Alert>
            </motion.div>
          </VStack>
        </motion.div>
      </VMain>
    </VPage>
  )
}
