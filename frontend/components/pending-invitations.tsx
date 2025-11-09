"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { InvitationStatusBadge } from "@/components/invitation-status-badge"
import { AlertTriangle } from "lucide-react"

export interface Invitation {
  id: string
  playerId: string
  playerName: string
  playerAvatar?: string
  playerLevel: number
  status: "pending" | "accepted" | "expired" | "declined"
  createdAt: string
  expiresAt: string
}

interface PendingInvitationsProps {
  invitations: Invitation[]
  onCancelInvitation: (invitationId: string) => void
  onResendInvitation: (invitationId: string) => void
}

export function PendingInvitations({ invitations, onCancelInvitation, onResendInvitation }: PendingInvitationsProps) {
  const [expandedSection, setExpandedSection] = useState(true)

  const pendingInvitations = invitations.filter((inv) => inv.status === "pending")
  const expiredInvitations = invitations.filter((inv) => inv.status === "expired")

  if (invitations.length === 0) {
    return null
  }

  return (
    <Card className="mb-4">
      <CardHeader className="border-b bg-gray-50 py-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm font-medium">Invitations</CardTitle>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setExpandedSection(!expandedSection)}
            className="h-8 px-2 text-xs"
          >
            {expandedSection ? "Masquer" : "Afficher"}
          </Button>
        </div>
      </CardHeader>

      {expandedSection && (
        <CardContent className="p-3">
          {pendingInvitations.length > 0 && (
            <div className="space-y-3">
              <h4 className="text-xs font-medium text-gray-500">En attente ({pendingInvitations.length})</h4>

              {pendingInvitations.map((invitation) => (
                <div key={invitation.id} className="flex items-center justify-between rounded-lg border bg-white p-3">
                  <div className="flex items-center gap-3">
                    <Avatar className="h-8 w-8 border border-gray-200">
                      <AvatarImage src={invitation.playerAvatar || "/placeholder.svg"} alt={invitation.playerName} />
                      <AvatarFallback>{invitation.playerName.charAt(0)}</AvatarFallback>
                    </Avatar>
                    <div>
                      <p className="text-sm font-medium">{invitation.playerName}</p>
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-gray-500">Niveau {invitation.playerLevel}</span>
                        <InvitationStatusBadge status={invitation.status} expiresAt={invitation.expiresAt} />
                      </div>
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-8 px-2 text-xs text-gray-500"
                      onClick={() => onCancelInvitation(invitation.id)}
                    >
                      Annuler
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}

          {expiredInvitations.length > 0 && (
            <div className="mt-4 space-y-3">
              <h4 className="text-xs font-medium text-gray-500">Expirées ({expiredInvitations.length})</h4>

              {expiredInvitations.map((invitation) => (
                <div
                  key={invitation.id}
                  className="flex items-center justify-between rounded-lg border border-gray-200 bg-gray-50 p-3"
                >
                  <div className="flex items-center gap-3">
                    <Avatar className="h-8 w-8 border border-gray-200">
                      <AvatarImage src={invitation.playerAvatar || "/placeholder.svg"} alt={invitation.playerName} />
                      <AvatarFallback>{invitation.playerName.charAt(0)}</AvatarFallback>
                    </Avatar>
                    <div>
                      <p className="text-sm font-medium text-gray-600">{invitation.playerName}</p>
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-gray-500">Niveau {invitation.playerLevel}</span>
                        <InvitationStatusBadge status={invitation.status} />
                      </div>
                    </div>
                  </div>
                  <Button
                    size="sm"
                    variant="outline"
                    className="h-8 px-2 text-xs"
                    onClick={() => onResendInvitation(invitation.id)}
                  >
                    Renvoyer
                  </Button>
                </div>
              ))}
            </div>
          )}

          <div className="mt-4 flex items-start gap-2 rounded-lg bg-amber-50 p-3 text-amber-800">
            <AlertTriangle className="h-4 w-4 shrink-0" />
            <p className="text-xs">
              Les invitations expirent après 24h. Les joueurs qui n'ont pas confirmé leur participation seront
              automatiquement retirés de la partie.
            </p>
          </div>
        </CardContent>
      )}
    </Card>
  )
}
