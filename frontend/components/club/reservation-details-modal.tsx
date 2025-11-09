"use client"

import { DialogFooter } from "@/components/ui/dialog"

import { useState } from "react"
import { format } from "date-fns"
import { fr } from "date-fns/locale"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { toast } from "@/components/ui/use-toast"
import { Loader2, Mail, Phone, CreditCard, RefreshCcw, X } from "lucide-react"

type Player = {
  name: string
  email?: string
  phone?: string
  is_member?: boolean
}

type Reservation = {
  id: string
  date: string
  start_time: string
  end_time: string
  court_name: string
  players: Player[]
  status: "confirmed" | "pending" | "cancelled"
  payment_status: "paid" | "pending" | "free"
  payment_method?: "card" | "cash" | "transfer" | null
  payment_date?: string | null
  payment_amount: number
  payment_reference?: string | null
  refunded?: boolean
}

type ReservationDetailsModalProps = {
  isOpen: boolean
  onClose: () => void
  reservation: Reservation | null
  onCancelReservation?: (id: string) => void
  onRefundReservation?: (id: string) => void
}

export function ReservationDetailsModal({
  isOpen,
  onClose,
  reservation,
  onCancelReservation,
  onRefundReservation,
}: ReservationDetailsModalProps) {
  const [isRefundDialogOpen, setIsRefundDialogOpen] = useState(false)
  const [isCancelDialogOpen, setIsCancelDialogOpen] = useState(false)
  const [isRefunding, setIsRefunding] = useState(false)
  const [isCancelling, setIsCancelling] = useState(false)

  if (!reservation) return null

  const formatDate = (dateString: string | undefined | null) => {
    if (!dateString) return "Non spécifié"
    const date = new Date(dateString)
    return format(date, "dd MMMM yyyy", { locale: fr })
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "confirmed":
        return <Badge className="bg-green-100 text-green-800 hover:bg-green-200">Confirmée</Badge>
      case "pending":
        return <Badge className="bg-yellow-100 text-yellow-800 hover:bg-yellow-200">En attente</Badge>
      case "cancelled":
        return <Badge className="bg-red-100 text-red-800 hover:bg-red-200">Annulée</Badge>
      default:
        return <Badge>{status}</Badge>
    }
  }

  const getPaymentStatusBadge = (status: string, refunded = false) => {
    if (refunded) {
      return <Badge className="bg-purple-100 text-purple-800 hover:bg-purple-200">Remboursé</Badge>
    }

    switch (status) {
      case "paid":
        return <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-200">Payé</Badge>
      case "pending":
        return <Badge className="bg-orange-100 text-orange-800 hover:bg-orange-200">En attente</Badge>
      case "free":
        return <Badge className="bg-gray-100 text-gray-800 hover:bg-gray-200">Gratuit</Badge>
      default:
        return <Badge>{status}</Badge>
    }
  }

  const getPaymentMethodText = (method: string | undefined | null) => {
    switch (method) {
      case "card":
        return "Carte bancaire"
      case "cash":
        return "Espèces"
      case "transfer":
        return "Virement"
      default:
        return "Non spécifié"
    }
  }

  const handleRefund = async () => {
    setIsRefunding(true)

    try {
      // Simuler un appel API pour le remboursement
      await new Promise((resolve) => setTimeout(resolve, 1500))

      if (onRefundReservation) {
        onRefundReservation(reservation.id)
      }

      setIsRefundDialogOpen(false)
      toast({
        title: "Remboursement effectué",
        description: `Le montant de ${reservation.payment_amount}€ a été remboursé`,
      })
    } catch (error) {
      toast({
        title: "Erreur",
        description: "Une erreur est survenue lors du remboursement",
        variant: "destructive",
      })
    } finally {
      setIsRefunding(false)
    }
  }

  const handleCancel = async () => {
    setIsCancelling(true)

    try {
      // Simuler un appel API pour l'annulation
      await new Promise((resolve) => setTimeout(resolve, 1000))

      if (onCancelReservation) {
        onCancelReservation(reservation.id)
      }

      setIsCancelDialogOpen(false)
      toast({
        title: "Réservation annulée",
        description: "La réservation a été annulée avec succès",
      })
    } catch (error) {
      toast({
        title: "Erreur",
        description: "Une erreur est survenue lors de l'annulation",
        variant: "destructive",
      })
    } finally {
      setIsCancelling(false)
    }
  }

  const isFutureReservation = () => {
    const today = new Date()
    const reservationDate = new Date(reservation.date)
    return reservationDate >= today
  }

  const canBeCancelled = () => {
    return isFutureReservation() && reservation.status !== "cancelled"
  }

  const canBeRefunded = () => {
    return (
      reservation.payment_status === "paid" &&
      !reservation.refunded &&
      (reservation.status === "confirmed" || reservation.status === "cancelled")
    )
  }

  return (
    <>
      <Dialog open={isOpen} onOpenChange={onClose}>
        <DialogContent className="sm:max-w-md max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Détails de la réservation</DialogTitle>
            <DialogDescription>
              {formatDate(reservation.date)} • {reservation.start_time} - {reservation.end_time} •{" "}
              {reservation.court_name}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <h3 className="mb-2 text-sm font-medium">Statut</h3>
              <div className="flex items-center gap-2">
                {getStatusBadge(reservation.status)}
                {getPaymentStatusBadge(reservation.payment_status, reservation.refunded)}
              </div>
            </div>

            {/* Informations de paiement */}
            <div>
              <h3 className="mb-2 text-sm font-medium">Paiement</h3>
              <div className="rounded-md border p-3 space-y-2">
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">Montant:</span>
                  <span className={reservation.refunded ? "line-through text-gray-500" : "font-medium"}>
                    {reservation.payment_amount}€
                  </span>
                </div>

                {reservation.payment_status !== "free" && (
                  <>
                    <div className="flex justify-between">
                      <span className="text-sm text-gray-500">Méthode:</span>
                      <span>{getPaymentMethodText(reservation.payment_method)}</span>
                    </div>

                    {reservation.payment_date && (
                      <div className="flex justify-between">
                        <span className="text-sm text-gray-500">Date:</span>
                        <span>{formatDate(reservation.payment_date)}</span>
                      </div>
                    )}

                    {reservation.payment_reference && (
                      <div className="flex justify-between">
                        <span className="text-sm text-gray-500">Référence:</span>
                        <span className="font-mono text-xs">{reservation.payment_reference}</span>
                      </div>
                    )}

                    {reservation.refunded && (
                      <div className="flex justify-between">
                        <span className="text-sm text-gray-500">Statut:</span>
                        <Badge className="bg-purple-100 text-purple-800">Remboursé</Badge>
                      </div>
                    )}
                  </>
                )}

                {canBeRefunded() && (
                  <div className="pt-2">
                    <Button
                      variant="outline"
                      size="sm"
                      className="w-full border-red-200 text-red-600 hover:bg-red-50"
                      onClick={() => setIsRefundDialogOpen(true)}
                    >
                      <RefreshCcw className="mr-2 h-3.5 w-3.5" />
                      Rembourser
                    </Button>
                  </div>
                )}
              </div>
            </div>

            {/* Joueurs avec informations de contact */}
            <div>
              <h3 className="mb-2 text-sm font-medium">Joueurs ({reservation.players.length}/4)</h3>
              <div className="space-y-2">
                {reservation.players.map((player, index) => (
                  <div key={index} className="rounded-md border p-3">
                    <div className="flex items-center justify-between mb-2">
                      <div className="flex items-center gap-2">
                        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gray-200 text-sm font-medium">
                          {player.name.charAt(0)}
                        </div>
                        <span className="font-medium">{player.name}</span>
                      </div>
                      <Badge variant="outline" className="bg-gray-100">
                        {index === 0 ? "Créateur" : "Joueur"}
                      </Badge>
                    </div>

                    {/* Informations de contact */}
                    <div className="space-y-1 text-sm">
                      {player.email && (
                        <div className="flex items-center gap-2 text-gray-600">
                          <Mail className="h-3.5 w-3.5" />
                          <a href={`mailto:${player.email}`} className="hover:underline">
                            {player.email}
                          </a>
                        </div>
                      )}
                      <div className="flex items-center gap-2 text-gray-600">
                        <Phone className="h-3.5 w-3.5" />
                        <a href={`tel:${player.phone || ""}`} className="hover:underline">
                          {player.phone || "Non spécifié"}
                        </a>
                      </div>
                      {player.is_member !== undefined && (
                        <div className="flex items-center gap-2 text-gray-600">
                          <Badge
                            variant="outline"
                            className={player.is_member ? "bg-green-50 text-green-700" : "bg-gray-50"}
                          >
                            {player.is_member ? "Adhérent" : "Non-adhérent"}
                          </Badge>
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <DialogFooter className="flex flex-col sm:flex-row sm:justify-end sm:space-x-2">
            {canBeCancelled() && (
              <Button
                variant="outline"
                className="mb-2 sm:mb-0 border-red-200 text-red-600 hover:bg-red-50 hover:text-red-700"
                onClick={() => setIsCancelDialogOpen(true)}
              >
                <X className="mr-2 h-4 w-4" />
                Annuler la réservation
              </Button>
            )}

            <Button onClick={onClose}>Fermer</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dialogue de confirmation de remboursement */}
      <AlertDialog open={isRefundDialogOpen} onOpenChange={setIsRefundDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Confirmer le remboursement</AlertDialogTitle>
            <AlertDialogDescription>
              Vous êtes sur le point de rembourser cette réservation. Cette action est irréversible.
            </AlertDialogDescription>
          </AlertDialogHeader>

          <div className="space-y-4 py-4">
            <div className="rounded-md border p-3 bg-gray-50">
              <div className="flex justify-between mb-2">
                <span className="text-sm font-medium">Réservation:</span>
                <span>
                  {formatDate(reservation.date)} • {reservation.start_time}
                </span>
              </div>
              <div className="flex justify-between mb-2">
                <span className="text-sm font-medium">Terrain:</span>
                <span>{reservation.court_name}</span>
              </div>
              <Separator className="my-2" />
              <div className="flex justify-between text-lg font-bold">
                <span>Montant à rembourser:</span>
                <span className="text-primary">{reservation.payment_amount}€</span>
              </div>
            </div>

            <div className="rounded-md border border-yellow-200 bg-yellow-50 p-3">
              <p className="text-sm text-yellow-800">
                Le remboursement sera effectué sur le moyen de paiement d'origine (
                {getPaymentMethodText(reservation.payment_method)}).
              </p>
            </div>
          </div>

          <AlertDialogFooter>
            <AlertDialogCancel>Annuler</AlertDialogCancel>
            <AlertDialogAction onClick={handleRefund} disabled={isRefunding}>
              {isRefunding ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Remboursement en cours...
                </>
              ) : (
                <>
                  <CreditCard className="mr-2 h-4 w-4" />
                  Confirmer le remboursement
                </>
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Dialogue de confirmation d'annulation */}
      <AlertDialog open={isCancelDialogOpen} onOpenChange={setIsCancelDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Confirmer l'annulation</AlertDialogTitle>
            <AlertDialogDescription>
              Vous êtes sur le point d'annuler cette réservation.
              {reservation.payment_status === "paid" &&
                !reservation.refunded &&
                " Un remboursement sera automatiquement effectué."}
            </AlertDialogDescription>
          </AlertDialogHeader>

          <div className="space-y-4 py-4">
            <div className="rounded-md border p-3 bg-gray-50">
              <div className="flex justify-between mb-2">
                <span className="text-sm font-medium">Réservation:</span>
                <span>
                  {formatDate(reservation.date)} • {reservation.start_time}
                </span>
              </div>
              <div className="flex justify-between mb-2">
                <span className="text-sm font-medium">Terrain:</span>
                <span>{reservation.court_name}</span>
              </div>
              <div className="flex justify-between mb-2">
                <span className="text-sm font-medium">Joueurs:</span>
                <span>{reservation.players.length}/4</span>
              </div>

              {reservation.payment_status === "paid" && !reservation.refunded && (
                <>
                  <Separator className="my-2" />
                  <div className="flex justify-between text-lg font-bold">
                    <span>Montant à rembourser:</span>
                    <span className="text-primary">{reservation.payment_amount}€</span>
                  </div>
                </>
              )}
            </div>

            {reservation.payment_status === "paid" && !reservation.refunded && (
              <div className="rounded-md border border-yellow-200 bg-yellow-50 p-3">
                <p className="text-sm text-yellow-800">
                  Le remboursement sera effectué sur le moyen de paiement d'origine (
                  {getPaymentMethodText(reservation.payment_method)}).
                </p>
              </div>
            )}
          </div>

          <AlertDialogFooter>
            <AlertDialogCancel>Annuler</AlertDialogCancel>
            <AlertDialogAction onClick={handleCancel} disabled={isCancelling}>
              {isCancelling ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Annulation en cours...
                </>
              ) : (
                <>
                  <X className="mr-2 h-4 w-4" />
                  Confirmer l'annulation
                </>
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
